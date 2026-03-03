using Common.Service;
using FluentAssertions;
using Flurl.Http;
using Flurl.Http.Testing;
using Garmin;
using Garmin.Auth;
using Garmin.Dto;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Dto;

namespace UnitTests.Garmin
{
	public class GarminApiClientTests
	{
		private Mock<ISettingsService> _settingsServiceMock;
		private ApiClient _apiClient;
		private Settings _settings;
		private HttpTest _httpTest;

		[SetUp]
		public void SetUp()
		{
			_settingsServiceMock = new Mock<ISettingsService>();
			_apiClient = new ApiClient(_settingsServiceMock.Object);
			_httpTest = new HttpTest();

			_settings = new Settings
			{
				Garmin = new GarminSettings
				{
					Api = new GarminApiSettings
					{
						SsoEmbedUrl = "https://sso.garmin.com/sso/embed",
						SsoSignInUrl = "https://sso.garmin.com/sso/signin",
						SsoMfaCodeUrl = "https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode",
						SsoUserAgent = "GCM-iOS-5.7.2.1",
						Origin = "https://sso.garmin.com",
						Referer = "https://sso.garmin.com/sso/signin",
						OAuth1TokenUrl = "https://connectapi.garmin.com/oauth-service/oauth/preauthorized",
						OAuth1LoginUrlParam = "https://sso.garmin.com/sso/embed&accepts-mfa-tokens=true",
						OAuth2RequestUrl = "https://connectapi.garmin.com/oauth-service/oauth/exchange/user/2.0",
						UploadActivityUrl = "https://connectapi.garmin.com/upload-service/upload",
						UploadActivityUserAgent = "GCM-iOS-5.7.2.1",
						UplaodActivityNkHeader = "NT"
					}
				}
			};

			_settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(_settings);
		}

		[TearDown]
		public void TearDown()
		{
			_httpTest.Dispose();
		}

		[Test]
		public async Task GetConsumerCredentialsAsync_ShouldReturnValidCredentials()
		{
			// SETUP
			var expectedCredentials = new ConsumerCredentials
			{
				Consumer_Key = "test-consumer-key",
				Consumer_Secret = "test-consumer-secret"
			};

			_httpTest.RespondWithJson(expectedCredentials);

			// ACT
			var result = await _apiClient.GetConsumerCredentialsAsync();

			// ASSERT
			result.Should().NotBeNull();
			result.Consumer_Key.Should().Be("test-consumer-key");
			result.Consumer_Secret.Should().Be("test-consumer-secret");
			
			_httpTest.ShouldHaveCalled("https://thegarth.s3.amazonaws.com/oauth_consumer.json")
				.WithVerb(HttpMethod.Get)
				.Times(1);
		}

		[Test]
		public void GetConsumerCredentialsAsync_WhenHttpRequestFails_ShouldThrowException()
		{
			// SETUP
			_httpTest.RespondWith("Server Error", 500);

			// ACT & ASSERT
			Assert.ThrowsAsync<FlurlHttpException>(() => _apiClient.GetConsumerCredentialsAsync());
		}

		[Test]
		public async Task InitCookieJarAsync_ShouldInitializeCookieJar()
		{
			// SETUP
			var queryParams = new { param1 = "value1", param2 = "value2" };
			_httpTest.RespondWith("OK");

			// ACT
			var result = await _apiClient.InitCookieJarAsync(queryParams);

			// ASSERT
			result.Should().NotBeNull();
			
			_httpTest.ShouldHaveCalled(_settings.Garmin.Api.SsoEmbedUrl)
				.WithVerb(HttpMethod.Get)
				.WithQueryParam("param1", "value1")
				.WithQueryParam("param2", "value2")
				.WithHeader("User-Agent", _settings.Garmin.Api.SsoUserAgent)
				.WithHeader("origin", _settings.Garmin.Api.Origin)
				.Times(1);
		}

		[Test]
		public void InitCookieJarAsync_WhenSettingsServiceThrows_ShouldPropagateException()
		{
			// SETUP
			_settingsServiceMock.Setup(s => s.GetSettingsAsync())
				.ThrowsAsync(new Exception("Settings error"));

			// ACT & ASSERT
			Assert.ThrowsAsync<Exception>(() => _apiClient.InitCookieJarAsync(new { }));
		}

		[Test]
		public async Task SendCredentialsAsync_WhenValidCredentials_ShouldReturnSuccess()
		{
			// SETUP
			var email = "test@example.com";
			var password = "password123";
			var queryParams = new { service = "test" };
			var loginData = new { username = email, password = password };
			var jar = new CookieJar();
			var expectedResponse = "Success response";

			_httpTest.RespondWith(expectedResponse);

			// ACT
			var result = await _apiClient.SendCredentialsAsync(email, password, queryParams, loginData, jar);

			// ASSERT
			result.Should().NotBeNull();
			result.RawResponseBody.Should().Be(expectedResponse);
			result.WasRedirected.Should().BeFalse();
			
			_httpTest.ShouldHaveCalled(_settings.Garmin.Api.SsoSignInUrl)
				.WithVerb(HttpMethod.Post)
				.WithQueryParam("service", "test")
				.WithHeader("User-Agent", _settings.Garmin.Api.SsoUserAgent)
				.WithHeader("origin", _settings.Garmin.Api.Origin)
				.WithHeader("referer", _settings.Garmin.Api.Referer)
				.WithHeader("NK", "NT")
				.Times(1);
		}

		[Test]
		public async Task SendCredentialsAsync_WhenRedirected_ShouldCaptureRedirection()
		{
			// SETUP
			var email = "test@example.com";
			var password = "password123";
			var queryParams = new { service = "test" };
			var loginData = new { username = email, password = password };
			var jar = new CookieJar();
			var redirectUrl = "https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode";

			_httpTest.RespondWith("Redirected", 302, new { Location = redirectUrl });

			// ACT
			var result = await _apiClient.SendCredentialsAsync(email, password, queryParams, loginData, jar);

			// ASSERT
			result.Should().NotBeNull();
			result.RawResponseBody.Should().Be("Redirected");
		}

		[Test]
		public void SendCredentialsAsync_WhenHttpRequestFails_ShouldThrowException()
		{
			// SETUP
			var jar = new CookieJar();
			_httpTest.RespondWith("Unauthorized", 401);

			// ACT & ASSERT
			Assert.ThrowsAsync<FlurlHttpException>(() => 
				_apiClient.SendCredentialsAsync("test@example.com", "password", new { }, new { }, jar));
		}

		[Test]
		public async Task GetCsrfTokenAsync_ShouldReturnValidToken()
		{
			// SETUP
			var queryParams = new { service = "test" };
			var jar = new CookieJar();
			var expectedResponse = "<input name='_csrf' value='test-csrf-token' />";

			_httpTest.RespondWith(expectedResponse);

			// ACT
			var result = await _apiClient.GetCsrfTokenAsync(queryParams, jar);

			// ASSERT
			result.Should().NotBeNull();
			result.RawResponseBody.Should().Be(expectedResponse);
			
			_httpTest.ShouldHaveCalled(_settings.Garmin.Api.SsoSignInUrl)
				.WithVerb(HttpMethod.Get)
				.WithQueryParam("service", "test")
				.WithHeader("User-Agent", _settings.Garmin.Api.SsoUserAgent)
				.WithHeader("origin", _settings.Garmin.Api.Origin)
				.Times(1);
		}

		[Test]
		public void GetCsrfTokenAsync_WhenHttpRequestFails_ShouldThrowException()
		{
			// SETUP
			var jar = new CookieJar();
			_httpTest.RespondWith("Server Error", 500);

			// ACT & ASSERT
			Assert.ThrowsAsync<FlurlHttpException>(() => 
				_apiClient.GetCsrfTokenAsync(new { }, jar));
		}

		[Test]
		public async Task SendMfaCodeAsync_WhenValidCode_ShouldReturnSuccess()
		{
			// SETUP
			var queryParams = new { service = "test" };
			var mfaData = new { mfaCode = "123456" };
			var jar = new CookieJar();
			var expectedResponse = "MFA Success";

			_httpTest.RespondWith(expectedResponse);

			// ACT
			var result = await _apiClient.SendMfaCodeAsync(queryParams, mfaData, jar);

			// ASSERT
			result.Should().Be(expectedResponse);
			
			_httpTest.ShouldHaveCalled(_settings.Garmin.Api.SsoMfaCodeUrl)
				.WithVerb(HttpMethod.Post)
				.WithQueryParam("service", "test")
				.WithHeader("User-Agent", _settings.Garmin.Api.SsoUserAgent)
				.WithHeader("origin", _settings.Garmin.Api.Origin)
				.Times(1);
		}

		[Test]
		public void SendMfaCodeAsync_WhenInvalidCode_ShouldThrowException()
		{
			// SETUP
			var jar = new CookieJar();
			_httpTest.RespondWith("Invalid MFA Code", 400);

			// ACT & ASSERT
			Assert.ThrowsAsync<FlurlHttpException>(() => 
				_apiClient.SendMfaCodeAsync(new { }, new { mfaCode = "000000" }, jar));
		}

		[Test]
		public async Task GetOAuth1TokenAsync_ShouldReturnValidOAuth1Token()
		{
			// SETUP
			var credentials = new ConsumerCredentials
			{
				Consumer_Key = "test-key",
				Consumer_Secret = "test-secret"
			};
			var ticket = "test-ticket";
			var expectedResponse = "oauth_token=token123&oauth_token_secret=secret123";

			_httpTest.RespondWith(expectedResponse);

			// ACT
			var result = await _apiClient.GetOAuth1TokenAsync(credentials, ticket);

			// ASSERT
			result.Should().Be(expectedResponse);
			
			_httpTest.ShouldHaveMadeACall()
				.WithVerb(HttpMethod.Get)
				.WithHeader("User-Agent", _settings.Garmin.Api.SsoUserAgent)
				.WithHeader("Authorization")
				.Times(1);
		}

		[Test]
		public void GetOAuth1TokenAsync_WhenNetworkError_ShouldThrowException()
		{
			// SETUP
			var credentials = new ConsumerCredentials
			{
				Consumer_Key = "test-key",
				Consumer_Secret = "test-secret"
			};
			_httpTest.RespondWith("Network Error", 503);

			// ACT & ASSERT
			Assert.ThrowsAsync<FlurlHttpException>(() => 
				_apiClient.GetOAuth1TokenAsync(credentials, "test-ticket"));
		}

		[Test]
		public async Task GetOAuth2TokenAsync_ShouldExchangeOAuth1ForOAuth2()
		{
			// SETUP
			var oAuth1Token = new OAuth1Token
			{
				Token = "oauth1-token",
				TokenSecret = "oauth1-secret"
			};
			var credentials = new ConsumerCredentials
			{
				Consumer_Key = "test-key",
				Consumer_Secret = "test-secret"
			};
			var expectedOAuth2Token = new OAuth2Token
			{
				Access_Token = "access-token-123",
				Token_Type = "Bearer",
				Expires_In = 3600,
				Refresh_Token = "refresh-token-123",
				ExpiresAt = DateTime.Now.AddHours(1)
			};

			_httpTest.RespondWithJson(expectedOAuth2Token);

			// ACT
			var result = await _apiClient.GetOAuth2TokenAsync(oAuth1Token, credentials);

			// ASSERT
			result.Should().NotBeNull();
			result.Access_Token.Should().Be("access-token-123");
			result.Token_Type.Should().Be("Bearer");
			result.Expires_In.Should().Be(3600);
			result.Refresh_Token.Should().Be("refresh-token-123");
			
			_httpTest.ShouldHaveCalled(_settings.Garmin.Api.OAuth2RequestUrl)
				.WithVerb(HttpMethod.Post)
				.WithHeader("User-Agent", _settings.Garmin.Api.SsoUserAgent)
				.WithHeader("Authorization")
				.WithHeader("Content-Type", "application/x-www-form-urlencoded")
				.Times(1);
		}

		[Test]
		public void GetOAuth2TokenAsync_WhenOAuth1TokenInvalid_ShouldThrowException()
		{
			// SETUP
			var oAuth1Token = new OAuth1Token
			{
				Token = "invalid-token",
				TokenSecret = "invalid-secret"
			};
			var credentials = new ConsumerCredentials
			{
				Consumer_Key = "test-key",
				Consumer_Secret = "test-secret"
			};

			_httpTest.RespondWith("Unauthorized", 401);

			// ACT & ASSERT
			Assert.ThrowsAsync<FlurlHttpException>(() => 
				_apiClient.GetOAuth2TokenAsync(oAuth1Token, credentials));
		}

		[Test]
		public async Task UploadActivity_WhenValidFile_ShouldReturnUploadResponse()
		{
			// SETUP
			var tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "test activity data");
			
			var format = ".fit";
			var auth = new GarminApiAuthentication
			{
				OAuth2Token = new OAuth2Token
				{
					Access_Token = "valid-access-token"
				}
			};

			var expectedResponse = new UploadResponse
			{
				DetailedImportResult = new DetailedImportResult
				{
					FileName = "test.fit",
					Successes = new List<Success>
					{
						new Success { ExternalId = "activity-123" }
					},
					Failures = new List<Failure>()
				}
			};

			_httpTest.RespondWithJson(expectedResponse);

			try
			{
				// ACT
				var result = await _apiClient.UploadActivity(tempFile, format, auth);

				// ASSERT
				result.Should().NotBeNull();
				result.DetailedImportResult.Should().NotBeNull();
				result.DetailedImportResult.FileName.Should().Be("test.fit");
				
				_httpTest.ShouldHaveMadeACall()
					.WithVerb(HttpMethod.Post)
					.WithHeader("Authorization", "Bearer valid-access-token")
					.WithHeader("NK", _settings.Garmin.Api.UplaodActivityNkHeader)
					.WithHeader("origin", _settings.Garmin.Api.Origin)
					.WithHeader("User-Agent", _settings.Garmin.Api.UploadActivityUserAgent)
					.Times(1);
			}
			finally
			{
				// Cleanup
				if (File.Exists(tempFile))
					File.Delete(tempFile);
			}
		}

		[Test]
		public void UploadActivity_WhenInvalidFile_ShouldThrowException()
		{
			// SETUP
			var nonExistentFile = Path.Combine(Path.GetTempPath(), "non-existent-file.fit");
			var auth = new GarminApiAuthentication
			{
				OAuth2Token = new OAuth2Token
				{
					Access_Token = "valid-access-token"
				}
			};

			// ACT & ASSERT
			// The actual implementation throws NullReferenceException when file doesn't exist
			Assert.ThrowsAsync<NullReferenceException>(() => 
				_apiClient.UploadActivity(nonExistentFile, ".fit", auth));
		}

		[Test]
		public void UploadActivity_WhenNetworkError_ShouldThrowException()
		{
			// SETUP
			var tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "test activity data");
			
			var auth = new GarminApiAuthentication
			{
				OAuth2Token = new OAuth2Token
				{
					Access_Token = "valid-access-token"
				}
			};

			_httpTest.RespondWith("Service Unavailable", 503);

			try
			{
				// ACT & ASSERT
				Assert.ThrowsAsync<FlurlHttpException>(() => 
					_apiClient.UploadActivity(tempFile, ".fit", auth));
			}
			finally
			{
				// Cleanup
				if (File.Exists(tempFile))
					File.Delete(tempFile);
			}
		}

		[Test]
		public async Task UploadActivity_WhenFileAlreadyExists_ShouldHandleGracefully()
		{
			// SETUP
			var tempFile = Path.GetTempFileName();
			File.WriteAllText(tempFile, "test activity data");
			
			var auth = new GarminApiAuthentication
			{
				OAuth2Token = new OAuth2Token
				{
					Access_Token = "valid-access-token"
				}
			};

			var responseWithDuplicate = new UploadResponse
			{
				DetailedImportResult = new DetailedImportResult
				{
					FileName = "test.fit",
					Successes = new List<Success>(),
					Failures = new List<Failure>
					{
						new Failure
						{
							ExternalId = "test.fit",
							Messages = new List<Messages>
							{
								new Messages { Code = 202, Content = "Activity already exists" }
							}
						}
					}
				}
			};

			_httpTest.RespondWithJson(responseWithDuplicate);

			try
			{
				// ACT
				var result = await _apiClient.UploadActivity(tempFile, ".fit", auth);

				// ASSERT
				result.Should().NotBeNull();
				result.DetailedImportResult.Failures.Should().HaveCount(1);
				result.DetailedImportResult.Failures.First().Messages.Should().Contain(m => m.Code == 202);
			}
			finally
			{
				// Cleanup
				if (File.Exists(tempFile))
					File.Delete(tempFile);
			}
		}

		[Test]
		public void Constructor_ShouldInitializeCorrectly()
		{
			// SETUP & ACT
			var apiClient = new ApiClient(_settingsServiceMock.Object);

			// ASSERT
			apiClient.Should().NotBeNull();
		}

		[Test]
		public void Constructor_WhenSettingsServiceIsNull_ShouldNotThrow()
		{
			// SETUP & ACT & ASSERT
			// The ApiClient constructor doesn't validate null parameters
			Assert.DoesNotThrow(() => new ApiClient(null));
		}
	}
}