using Common.Observe;
using Common.Service;
using Garmin.Auth;
using Microsoft.Playwright;
using Serilog;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Garmin.Auth;

public interface IPlaywrightGarminAuthService
{
	/// <summary>
	/// Attempts headless browser login to Garmin SSO and returns a serviceTicketId.
	/// Returns null if Playwright is unavailable or login fails (MFA, CAPTCHA, timeout, etc.).
	/// Throws <see cref="GarminAuthenticationError"/> with Code.InvalidCredentials if credentials are rejected.
	/// </summary>
	Task<string> GetServiceTicketViaHeadlessBrowserAsync(string email, string password);

	/// <summary>
	/// Returns true if Playwright and a Chromium browser binary are installed and usable.
	/// </summary>
	bool IsAvailable();
}

public class PlaywrightGarminAuthService : IPlaywrightGarminAuthService
{
	private static readonly ILogger _logger = LogContext.ForClass<PlaywrightGarminAuthService>();

	private readonly ISettingsService _settingsService;

	private bool? _availabilityCache;

	public PlaywrightGarminAuthService(ISettingsService settingsService)
	{
		_settingsService = settingsService;
	}

	public bool IsAvailable()
	{
		if (_availabilityCache.HasValue)
			return _availabilityCache.Value;

		try
		{
			var playwright = Playwright.CreateAsync().GetAwaiter().GetResult();
			var execPath = playwright.Chromium.ExecutablePath;
			_availabilityCache = System.IO.File.Exists(execPath);

			if (!_availabilityCache.Value)
				_logger.Information("Playwright Chromium binary not found at {Path}. Headless browser auth is unavailable.", execPath);
		}
		catch (Exception ex)
		{
			_logger.Information(ex, "Playwright is not installed or could not be initialized. Headless browser auth is unavailable.");
			_availabilityCache = false;
		}

		return _availabilityCache.Value;
	}

	public async Task<string> GetServiceTicketViaHeadlessBrowserAsync(string email, string password)
	{
		using var tracing = Tracing.Trace($"{nameof(PlaywrightGarminAuthService)}.{nameof(GetServiceTicketViaHeadlessBrowserAsync)}");

		var settings = await _settingsService.GetSettingsAsync();
		var di = settings.Garmin.Api.Di;

		IPlaywright playwright = null;
		IBrowser browser = null;

		try
		{
			playwright = await Playwright.CreateAsync();
			browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
			{
				Headless = di.PlaywrightHeadless,
			});

			var context = await browser.NewContextAsync(new BrowserNewContextOptions
			{
				UserAgent = di.BrowserUserAgent,
				ViewportSize = new ViewportSize { Width = 412, Height = 915 },
				IsMobile = true,
				HasTouch = true,
				Locale = "en-US",
			});

			var page = await context.NewPageAsync();

			var ticketTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

			page.Response += async (_, response) =>
			{
				try
				{
					if (!response.Url.Contains("/mobile/api/login"))
						return;

					_logger.Debug("Intercepted /mobile/api/login response, status={Status}", response.Status);

					if (response.Status == 200)
					{
						var body = await response.TextAsync();
						_logger.Verbose("Login response body: {Body}", body);

						using var doc = JsonDocument.Parse(body);
						var root = doc.RootElement;

						if (root.TryGetProperty("serviceTicketId", out var ticketProp))
						{
							ticketTcs.TrySetResult(ticketProp.GetString() ?? string.Empty);
							return;
						}

						// Check for invalid credentials in the response
						if (root.TryGetProperty("message", out var msgProp))
						{
							var msg = msgProp.GetString() ?? string.Empty;
							if (msg.Contains("Invalid", StringComparison.OrdinalIgnoreCase)
								|| msg.Contains("incorrect", StringComparison.OrdinalIgnoreCase)
								|| msg.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
							{
								ticketTcs.TrySetException(
									new GarminAuthenticationError($"Garmin rejected credentials during headless login: {msg}")
									{ Code = Code.InvalidCredentials });
								return;
							}
						}

						ticketTcs.TrySetResult(string.Empty);
					}
					else if (response.Status == 401 || response.Status == 403)
					{
						ticketTcs.TrySetException(
							new GarminAuthenticationError("Garmin rejected credentials during headless login (HTTP {Status}).")
							{ Code = Code.InvalidCredentials });
					}
					else
					{
						_logger.Warning("Headless login: unexpected status {Status} from /mobile/api/login", response.Status);
						ticketTcs.TrySetResult(string.Empty);
					}
				}
				catch (Exception ex)
				{
					ticketTcs.TrySetException(ex);
				}
			};

			var ssoUrl = $"https://sso.garmin.com/mobile/sso/en_US/sign-in?clientId={di.LoginClientId}&service={Uri.EscapeDataString(di.ServiceUrl)}";
			_logger.Debug("Navigating to Garmin SSO: {Url}", ssoUrl);
			await page.GotoAsync(ssoUrl);

			// Detect MFA page — if already on MFA we can't proceed in Phase 3a
			var currentUrl = page.Url;
			if (IsMfaPage(currentUrl))
			{
				_logger.Information("Headless browser landed on MFA page before credentials entered. Returning null to fall through to manual service ticket flow.");
				return string.Empty;
			}

			// Fill username
			var usernameSelector = "#username, input[name='username'], input[type='email']";
			await page.WaitForSelectorAsync(usernameSelector, new PageWaitForSelectorOptions
			{
				Timeout = di.PlaywrightTimeoutSeconds * 1000
			});
			await page.FillAsync(usernameSelector, email);

			// Fill password
			var passwordSelector = "#password, input[name='password'], input[type='password']";
			await page.WaitForSelectorAsync(passwordSelector, new PageWaitForSelectorOptions
			{
				Timeout = di.PlaywrightTimeoutSeconds * 1000
			});
			await page.FillAsync(passwordSelector, password);

			// Submit
			await page.PressAsync(passwordSelector, "Enter");

			// Wait for ticket capture or timeout
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(di.PlaywrightTimeoutSeconds));
			cts.Token.Register(() => ticketTcs.TrySetResult(string.Empty));

			// Check for MFA redirect after submit
			try
			{
				await page.WaitForURLAsync(url => IsMfaPage(url),
					new PageWaitForURLOptions { Timeout = 2000 });

				_logger.Information("Headless browser detected MFA challenge after submitting credentials. MFA automation not yet supported (Phase 3b). Returning null to fall through to manual service ticket flow.");
				return string.Empty;
			}
			catch (TimeoutException)
			{
				// No MFA redirect within 2s — continue waiting for the login response
			}

			var ticket = await ticketTcs.Task;

			if (ticket is null)
				_logger.Information("Headless browser login did not yield a service ticket (response missing serviceTicketId). Returning null.");
			else
				_logger.Information("Headless browser login succeeded, service ticket obtained.");

			return ticket;
		}
		catch (GarminAuthenticationError)
		{
			throw;
		}
		catch (TimeoutException ex)
		{
			_logger.Warning(ex, "Headless browser auth timed out after {Seconds}s.", di.PlaywrightTimeoutSeconds);
			return string.Empty;
		}
		catch (Exception ex)
		{
			_logger.Warning(ex, "Headless browser auth encountered an unexpected error.");
			return string.Empty;
		}
		finally
		{
			if (browser is not null)
				await browser.CloseAsync();

			playwright?.Dispose();
		}
	}

	private static bool IsMfaPage(string url) =>
		url.Contains("/mfa/", StringComparison.OrdinalIgnoreCase)
		|| url.Contains("/verifyMFA/", StringComparison.OrdinalIgnoreCase)
		|| url.Contains("loginEnterMfaCode", StringComparison.OrdinalIgnoreCase);
}
