using Common.Dto.Api;
using Common.Helpers;
using Common.Service;
using Garmin.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
	[ApiController]
	[Produces("application/json")]
	[Consumes("application/json")]
	public class GarminAuthenticationController : Controller
	{
		private readonly IGarminAuthenticationService _garminAuthService;
		private readonly ISettingsService _settingsService;

		public GarminAuthenticationController(IGarminAuthenticationService garminAuthService, ISettingsService settingsService)
		{
			_garminAuthService = garminAuthService;
			_settingsService = settingsService;
		}

		[HttpGet]
		[Route("api/garminauthentication")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<ActionResult<GarminAuthenticationGetResponse>> GetAsync()
		{
			var settings = await _settingsService.GetSettingsAsync();
			var auth = _settingsService.GetGarminAuthentication(settings.Garmin.Email);

			var result = new GarminAuthenticationGetResponse() { IsAuthenticated = auth?.IsValid(settings) ?? false };
			return Ok(result);
		}

		/// <summary>
		/// Initializes Garmin authentication. When TwoStep verification is enabled, will perform part one of the auth
		/// flow, triggering and Email or Text to the user with an MFA code.  This flow will be completed by calling
		/// POST api/garminauthentication/mfaToken
		/// </summary>
		/// <returns>201 - Garmin Authentication was successfully initialized.</returns>
		/// <returns>202 - Garmin Authentication was successfully started but needs MFA token to be completed.</returns>
		[HttpPost]
		[Route("api/garminauthentication/signin")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status202Accepted)]
		public async Task<ActionResult> SignInAsync()
		{
			var settings = await _settingsService.GetSettingsAsync();

			if (settings.Garmin.Password.CheckIsNullOrEmpty("Garmin Password", out var result)) return result;
			if (settings.Garmin.Email.CheckIsNullOrEmpty("Garmin Email", out result)) return result;

			try
			{ 
				if (!settings.Garmin.TwoStepVerificationEnabled)
				{
					await _garminAuthService.RefreshGarminAuthenticationAsync();
					return Created("api/garminauthentication", new GarminAuthenticationGetResponse() { IsAuthenticated = true });
				}
				else
				{
					var auth = await _garminAuthService.RefreshGarminAuthenticationAsync();

					if (auth.AuthStage == Common.Stateful.AuthStage.NeedMfaToken)
						return Accepted();

					return Created("api/garminauthentication", new GarminAuthenticationGetResponse() { IsAuthenticated = true });
				}
			}
			catch (GarminAuthenticationError gae) when (gae.Code == Code.UnexpectedMfa)
			{
				return BadRequest(new ErrorResponse("It looks like your account is protected by two step verification. Please enable the Two Step verification setting."));
			}
			catch (GarminAuthenticationError gae) when (gae.Code == Code.InvalidCredentials)
			{
				return Unauthorized(new ErrorResponse("Garmin authentication failed. Invalid Garmin credentials."));
			}
			catch (Exception e)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
			}
		}

		[HttpPost]
		[Route("api/garminauthentication/mfaToken")]
		[ProducesResponseType(StatusCodes.Status201Created)]
		public async Task<ActionResult> PostMfaTokenAsync([FromBody] GarminAuthenticationMfaTokenPostRequest request)
		{
			var settings = await _settingsService.GetSettingsAsync();

			if (!settings.Garmin.TwoStepVerificationEnabled)
				return BadRequest(new ErrorResponse("Garmin two step verification is not enabled in Settings."));

			try
			{
				await _garminAuthService.CompleteMFAAuthAsync(request.MfaToken);
				return Created("api/garminauthentication", new GarminAuthenticationGetResponse() { IsAuthenticated = true });
			}
			catch (Exception e)
			{
				return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse($"Unexpected error occurred: {e.Message}"));
			}
		}
	}
}
