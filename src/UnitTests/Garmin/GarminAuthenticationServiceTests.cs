using Common.Dto;
using Common.Service;
using FluentAssertions;
using Flurl.Http;
using Garmin;
using Garmin.Auth;
using Garmin.Database;
using Garmin.Dto;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace UnitTests.Garmin
{
	public class GarminAuthenticationServiceTests
	{
		private Mock<ISettingsService> _settingsServiceMock;
		private Mock<IGarminApiClient> _apiClientMock;
		private Mock<IGarminDb> _garminDbMock;
		private GarminAuthenticationService _authService;
		private Settings _settings;

		[SetUp]
		public void SetUp()
		{
			_settingsServiceMock = new Mock<ISettingsService>();
			_apiClientMock = new Mock<IGarminApiClient>();
			_garminDbMock = new Mock<IGarminDb>();
			
			_authService = new GarminAuthenticationService(
				_settingsServiceMock.Object,
				_apiClientMock.Object,
				_garminDbMock.Object);

			_settings = new Settings
			{
				Garmin = new GarminSettings
				{
					Email = "test@example.com",
					Password = "password123",
					TwoStepVerificationEnabled = true,
					Api = new GarminApiSettings
					{
						SsoEmbedUrl = "https://sso.garmin.com/sso/embed",
						SsoSignInUrl = "https://sso.garmin.com/sso/signin"
					}
				}
			};

			_settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(_settings);
		}

		[Test]
		public async Task GarminAuthTokenExistsAndIsValidAsync_WhenValidToken_ShouldReturnTrue()
		{
			// SETUP
			var validOAuth2Token = new OAuth2Token
			{
				Access_Token = "valid-token",
				ExpiresAt = DateTime.Now.AddHours(2) // Need 2 hours because IsExpired() adds 1 hour padding
			};

			_garminDbMock.Setup(db => db.GetGarminOAuth2TokenAsync(1))
				.ReturnsAsync(validOAuth2Token);

			// ACT
			var result = await _authService.GarminAuthTokenExistsAndIsValidAsync();

			// ASSERT
			result.Should().BeTrue();
			_garminDbMock.Verify(db => db.GetGarminOAuth2TokenAsync(1), Times.Once);
		}

		[Test]
		public async Task GarminAuthTokenExistsAndIsValidAsync_WhenExpiredToken_ShouldReturnFalse()
		{
			// SETUP
			var expiredOAuth2Token = new OAuth2Token
			{
				Access_Token = "expired-token",
				ExpiresAt = DateTime.Now.AddHours(-2) // Expired 2 hours ago
			};

			_garminDbMock.Setup(db => db.GetGarminOAuth2TokenAsync(1))
				.ReturnsAsync(expiredOAuth2Token);
			_garminDbMock.Setup(db => db.GetGarminOAuth1TokenAsync(1))
				.ReturnsAsync((OAuth1Token)null);

			// ACT
			var result = await _authService.GarminAuthTokenExistsAndIsValidAsync();

			// ASSERT
			result.Should().BeFalse();
			_garminDbMock.Verify(db => db.GetGarminOAuth2TokenAsync(1), Times.Once);
			_garminDbMock.Verify(db => db.GetGarminOAuth1TokenAsync(1), Times.Once);
		}

		[Test]
		public async Task GarminAuthTokenExistsAndIsValidAsync_WhenOAuth1TokenExists_ShouldReturnTrue()
		{
			// SETUP
			var oAuth1Token = new OAuth1Token
			{
				Token = "oauth1-token",
				TokenSecret = "oauth1-secret"
			};

			_garminDbMock.Setup(db => db.GetGarminOAuth2TokenAsync(1))
				.ReturnsAsync((OAuth2Token)null);
			_garminDbMock.Setup(db => db.GetGarminOAuth1TokenAsync(1))
				.ReturnsAsync(oAuth1Token);

			// ACT
			var result = await _authService.GarminAuthTokenExistsAndIsValidAsync();

			// ASSERT
			result.Should().BeTrue();
			_garminDbMock.Verify(db => db.GetGarminOAuth2TokenAsync(1), Times.Once);
			_garminDbMock.Verify(db => db.GetGarminOAuth1TokenAsync(1), Times.Once);
		}

		[Test]
		public async Task GetGarminAuthenticationAsync_WhenValidOAuth2Token_ShouldReturnCompleted()
		{
			// SETUP
			var validOAuth2Token = new OAuth2Token
			{
				Access_Token = "valid-token",
				ExpiresAt = DateTime.Now.AddHours(2) // Need 2 hours because IsExpired() adds 1 hour padding
			};

			_garminDbMock.Setup(db => db.GetGarminOAuth2TokenAsync(1))
				.ReturnsAsync(validOAuth2Token);

			// ACT
			var result = await _authService.GetGarminAuthenticationAsync();

			// ASSERT
			result.Should().NotBeNull();
			result.AuthStage.Should().Be(AuthStage.Completed);
			result.OAuth2Token.Should().Be(validOAuth2Token);
			_garminDbMock.Verify(db => db.GetGarminOAuth2TokenAsync(1), Times.Once);
		}

		[Test]
		public async Task GetGarminAuthenticationAsync_WhenValidOAuth1Token_ShouldExchangeForOAuth2()
		{
			// SETUP
			var oAuth1Token = new OAuth1Token
			{
				Token = "oauth1-token",
				TokenSecret = "oauth1-secret"
			};

			var consumerCredentials = new ConsumerCredentials
			{
				Consumer_Key = "consumer-key",
				Consumer_Secret = "consumer-secret"
			};

			var newOAuth2Token = new OAuth2Token
			{
				Access_Token = "new-oauth2-token",
				ExpiresAt = DateTime.Now.AddHours(1),
				Expires_In = 3600
			};

			_garminDbMock.Setup(db => db.GetGarminOAuth2TokenAsync(1))
				.ReturnsAsync((OAuth2Token)null);
			_garminDbMock.Setup(db => db.GetGarminOAuth1TokenAsync(1))
				.ReturnsAsync(oAuth1Token);
			_apiClientMock.Setup(api => api.GetConsumerCredentialsAsync())
				.ReturnsAsync(consumerCredentials);
			_apiClientMock.Setup(api => api.GetOAuth2TokenAsync(oAuth1Token, consumerCredentials))
				.ReturnsAsync(newOAuth2Token);

			// ACT
			var result = await _authService.GetGarminAuthenticationAsync();

			// ASSERT
			result.Should().NotBeNull();
			result.AuthStage.Should().Be(AuthStage.Completed);
			result.OAuth2Token.Access_Token.Should().Be("new-oauth2-token");
			_garminDbMock.Verify(db => db.UpsertGarminOAuth2TokenAsync(1, It.IsAny<OAuth2Token>()), Times.Once);
		}

		[Test]
		public async Task GetGarminAuthenticationAsync_WhenNoTokens_ShouldSignIn()
		{
			// SETUP
			_garminDbMock.Setup(db => db.GetGarminOAuth2TokenAsync(1))
				.ReturnsAsync((OAuth2Token)null);
			_garminDbMock.Setup(db => db.GetGarminOAuth1TokenAsync(1))
				.ReturnsAsync((OAuth1Token)null);

			// Mock the complex sign-in flow
			var cookieJar = new CookieJar();
			var csrfResult = new GarminResult { RawResponseBody = "<input name=\"_csrf\" value=\"csrf-token\" />" };
			var credentialsResult = new SendCredentialsResult 
			{ 
				RawResponseBody = "embed?ticket=test-ticket\"",
				WasRedirected = false 
			};
			var consumerCredentials = new ConsumerCredentials { Consumer_Key = "key", Consumer_Secret = "secret" };
			var oAuth1Response = "oauth_token=token&oauth_token_secret=secret";
			var oAuth2Token = new OAuth2Token 
			{ 
				Access_Token = "final-token", 
				ExpiresAt = DateTime.Now.AddHours(1),
				Expires_In = 3600
			};

			_apiClientMock.Setup(api => api.InitCookieJarAsync(It.IsAny<object>()))
				.ReturnsAsync(cookieJar);
			_apiClientMock.Setup(api => api.GetCsrfTokenAsync(It.IsAny<object>(), cookieJar))
				.ReturnsAsync(csrfResult);
			_apiClientMock.Setup(api => api.SendCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>(), cookieJar))
				.ReturnsAsync(credentialsResult);
			_apiClientMock.Setup(api => api.GetConsumerCredentialsAsync())
				.ReturnsAsync(consumerCredentials);
			_apiClientMock.Setup(api => api.GetOAuth1TokenAsync(consumerCredentials, "test-ticket"))
				.ReturnsAsync(oAuth1Response);
			_apiClientMock.Setup(api => api.GetOAuth2TokenAsync(It.IsAny<OAuth1Token>(), consumerCredentials))
				.ReturnsAsync(oAuth2Token);

			// ACT
			var result = await _authService.GetGarminAuthenticationAsync();

			// ASSERT
			result.Should().NotBeNull();
			result.AuthStage.Should().Be(AuthStage.Completed);
			result.OAuth2Token.Access_Token.Should().Be("final-token");
		}

		[Test]
		public async Task SignInAsync_WhenValidCredentials_ShouldReturnAuthentication()
		{
			// SETUP
			var cookieJar = new CookieJar();
			var csrfResult = new GarminResult { RawResponseBody = "<input name=\"_csrf\" value=\"csrf-token\" />" };
			var credentialsResult = new SendCredentialsResult 
			{ 
				RawResponseBody = "embed?ticket=test-ticket\"",
				WasRedirected = false 
			};
			var consumerCredentials = new ConsumerCredentials { Consumer_Key = "key", Consumer_Secret = "secret" };
			var oAuth1Response = "oauth_token=token&oauth_token_secret=secret";
			var oAuth2Token = new OAuth2Token 
			{ 
				Access_Token = "access-token", 
				ExpiresAt = DateTime.Now.AddHours(1),
				Expires_In = 3600
			};

			_apiClientMock.Setup(api => api.InitCookieJarAsync(It.IsAny<object>()))
				.ReturnsAsync(cookieJar);
			_apiClientMock.Setup(api => api.GetCsrfTokenAsync(It.IsAny<object>(), cookieJar))
				.ReturnsAsync(csrfResult);
			_apiClientMock.Setup(api => api.SendCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>(), cookieJar))
				.ReturnsAsync(credentialsResult);
			_apiClientMock.Setup(api => api.GetConsumerCredentialsAsync())
				.ReturnsAsync(consumerCredentials);
			_apiClientMock.Setup(api => api.GetOAuth1TokenAsync(consumerCredentials, "test-ticket"))
				.ReturnsAsync(oAuth1Response);
			_apiClientMock.Setup(api => api.GetOAuth2TokenAsync(It.IsAny<OAuth1Token>(), consumerCredentials))
				.ReturnsAsync(oAuth2Token);

			// ACT
			var result = await _authService.SignInAsync();

			// ASSERT
			result.Should().NotBeNull();
			result.AuthStage.Should().Be(AuthStage.Completed);
			result.OAuth2Token.Access_Token.Should().Be("access-token");
			_garminDbMock.Verify(db => db.UpsertGarminOAuth1TokenAsync(1, It.IsAny<OAuth1Token>()), Times.AtLeastOnce);
			_garminDbMock.Verify(db => db.UpsertGarminOAuth2TokenAsync(1, It.IsAny<OAuth2Token>()), Times.AtLeastOnce);
		}

		[Test]
		public void SignInAsync_WhenInvalidCredentials_ShouldThrowException()
		{
			// SETUP
			var invalidSettings = new Settings
			{
				Garmin = new GarminSettings() // No email/password
			};
			_settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(invalidSettings);

			// ACT & ASSERT
			Assert.ThrowsAsync<ArgumentException>(() => _authService.SignInAsync());
		}

		[Test]
		public async Task SignInAsync_WhenMfaRequired_ShouldReturnNeedMfaToken()
		{
			// SETUP
			var cookieJar = new CookieJar();
			var csrfResult = new GarminResult { RawResponseBody = "<input name=\"_csrf\" value=\"csrf-token\" />" };
			var mfaRedirectResult = new SendCredentialsResult
			{
				RawResponseBody = "<input name=\"_csrf\" value=\"mfa-csrf-token\" />",
				WasRedirected = true,
				RedirectedTo = "https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode"
			};

			_apiClientMock.Setup(api => api.InitCookieJarAsync(It.IsAny<object>()))
				.ReturnsAsync(cookieJar);
			_apiClientMock.Setup(api => api.GetCsrfTokenAsync(It.IsAny<object>(), cookieJar))
				.ReturnsAsync(csrfResult);
			_apiClientMock.Setup(api => api.SendCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>(), cookieJar))
				.ReturnsAsync(mfaRedirectResult);

			// ACT
			var result = await _authService.SignInAsync();

			// ASSERT
			result.Should().NotBeNull();
			result.AuthStage.Should().Be(AuthStage.NeedMfaToken);
			_garminDbMock.Verify(db => db.UpsertPartialGarminAuthenticationAsync(1, It.IsAny<StagedPartialGarminAuthentication>()), Times.AtLeastOnce);
		}

		[Test]
		public void SignInAsync_WhenCloudflareBlocked_ShouldThrowCloudflareException()
		{
			// SETUP
			var cookieJar = new CookieJar();
			var csrfResult = new GarminResult { RawResponseBody = "<input name=\"_csrf\" value=\"csrf-token\" />" };
			var cloudflareException = new FlurlHttpException(null);

			_apiClientMock.Setup(api => api.InitCookieJarAsync(It.IsAny<object>()))
				.ReturnsAsync(cookieJar);
			_apiClientMock.Setup(api => api.GetCsrfTokenAsync(It.IsAny<object>(), cookieJar))
				.ReturnsAsync(csrfResult);
			_apiClientMock.Setup(api => api.SendCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>(), cookieJar))
				.ThrowsAsync(cloudflareException);

			// ACT & ASSERT
			Assert.ThrowsAsync<FlurlHttpException>(() => _authService.SignInAsync());
		}

		[Test]
		public async Task CompleteMFAAuthAsync_WhenValidCode_ShouldCompleteAuth()
		{
			// SETUP
			var mfaCode = "123456";
			var partialAuth = new StagedPartialGarminAuthentication
			{
				AuthStage = AuthStage.NeedMfaToken,
				MFACsrfToken = "mfa-csrf-token",
				CookieJarString = "cookie-jar-string",
				ExpiresAt = DateTime.Now.AddMinutes(10)
			};

			var consumerCredentials = new ConsumerCredentials { Consumer_Key = "key", Consumer_Secret = "secret" };
			var oAuth1Response = "oauth_token=token&oauth_token_secret=secret";
			var oAuth2Token = new OAuth2Token 
			{ 
				Access_Token = "final-token", 
				ExpiresAt = DateTime.Now.AddHours(1),
				Expires_In = 3600
			};

			_garminDbMock.Setup(db => db.GetStagedPartialGarminAuthenticationAsync(1))
				.ReturnsAsync(partialAuth);
			_apiClientMock.Setup(api => api.SendMfaCodeAsync(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<CookieJar>()))
				.ReturnsAsync("embed?ticket=mfa-ticket\"");
			_apiClientMock.Setup(api => api.GetConsumerCredentialsAsync())
				.ReturnsAsync(consumerCredentials);
			_apiClientMock.Setup(api => api.GetOAuth1TokenAsync(consumerCredentials, "mfa-ticket"))
				.ReturnsAsync(oAuth1Response);
			_apiClientMock.Setup(api => api.GetOAuth2TokenAsync(It.IsAny<OAuth1Token>(), consumerCredentials))
				.ReturnsAsync(oAuth2Token);

			// ACT
			var result = await _authService.CompleteMFAAuthAsync(mfaCode);

			// ASSERT
			result.Should().NotBeNull();
			result.AuthStage.Should().Be(AuthStage.Completed);
			result.OAuth2Token.Access_Token.Should().Be("final-token");
			_garminDbMock.Verify(db => db.UpsertPartialGarminAuthenticationAsync(1, null), Times.Once);
		}

		[Test]
		public void CompleteMFAAuthAsync_WhenInvalidCode_ShouldThrowException()
		{
			// SETUP
			var mfaCode = "000000";
			var partialAuth = new StagedPartialGarminAuthentication
			{
				AuthStage = AuthStage.NeedMfaToken,
				MFACsrfToken = "mfa-csrf-token",
				CookieJarString = "cookie-jar-string",
				ExpiresAt = DateTime.Now.AddMinutes(10)
			};

			var mfaException = new FlurlHttpException(null);

			_garminDbMock.Setup(db => db.GetStagedPartialGarminAuthenticationAsync(1))
				.ReturnsAsync(partialAuth);
			_apiClientMock.Setup(api => api.SendMfaCodeAsync(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<CookieJar>()))
				.ThrowsAsync(mfaException);

			// ACT & ASSERT
			Assert.ThrowsAsync<FlurlHttpException>(() => _authService.CompleteMFAAuthAsync(mfaCode));
		}

		[Test]
		public void CompleteMFAAuthAsync_WhenNoPartialAuth_ShouldThrowException()
		{
			// SETUP
			_garminDbMock.Setup(db => db.GetStagedPartialGarminAuthenticationAsync(1))
				.ReturnsAsync((StagedPartialGarminAuthentication)null);

			// ACT & ASSERT
			Assert.ThrowsAsync<ArgumentException>(() => _authService.CompleteMFAAuthAsync("123456"));
		}

		[Test]
		public void CompleteMFAAuthAsync_WhenWrongAuthStage_ShouldThrowException()
		{
			// SETUP
			var partialAuth = new StagedPartialGarminAuthentication
			{
				AuthStage = AuthStage.Completed, // Wrong stage
				ExpiresAt = DateTime.Now.AddMinutes(10)
			};

			_garminDbMock.Setup(db => db.GetStagedPartialGarminAuthenticationAsync(1))
				.ReturnsAsync(partialAuth);

			// ACT & ASSERT
			Assert.ThrowsAsync<ArgumentException>(() => _authService.CompleteMFAAuthAsync("123456"));
		}

		[Test]
		public async Task SignOutAsync_ShouldClearAllTokens()
		{
			// SETUP
			_garminDbMock.Setup(db => db.UpsertPartialGarminAuthenticationAsync(1, null))
				.Returns(Task.CompletedTask);
			_garminDbMock.Setup(db => db.UpsertGarminOAuth1TokenAsync(1, null))
				.Returns(Task.CompletedTask);
			_garminDbMock.Setup(db => db.UpsertGarminOAuth2TokenAsync(1, null))
				.Returns(Task.CompletedTask);

			// ACT
			var result = await _authService.SignOutAsync();

			// ASSERT
			result.Should().BeTrue();
			_garminDbMock.Verify(db => db.UpsertPartialGarminAuthenticationAsync(1, null), Times.Once);
			_garminDbMock.Verify(db => db.UpsertGarminOAuth1TokenAsync(1, null), Times.Once);
			_garminDbMock.Verify(db => db.UpsertGarminOAuth2TokenAsync(1, null), Times.Once);
		}

		[Test]
		public void Constructor_ShouldInitializeCorrectly()
		{
			// SETUP & ACT
			var authService = new GarminAuthenticationService(
				_settingsServiceMock.Object,
				_apiClientMock.Object,
				_garminDbMock.Object);

			// ASSERT
			authService.Should().NotBeNull();
		}

		[Test]
		public void Constructor_WhenDependenciesAreNull_ShouldNotThrow()
		{
			// SETUP & ACT & ASSERT
			// The constructor doesn't validate null parameters
			Assert.DoesNotThrow(() => new GarminAuthenticationService(null, null, null));
		}

		[Test]
		public void SignInAsync_WhenInitCookieJarFails_ShouldThrowAuthenticationError()
		{
			// SETUP
			var initException = new FlurlHttpException(null);
			_apiClientMock.Setup(api => api.InitCookieJarAsync(It.IsAny<object>()))
				.ThrowsAsync(initException);

			// ACT & ASSERT
			var exception = Assert.ThrowsAsync<GarminAuthenticationError>(() => _authService.SignInAsync());
			exception.Code.Should().Be(Code.FailedPriorToCredentialsUsed);
			exception.Message.Should().Contain("Failed to initialize sign in flow");
		}

		[Test]
		public void SignInAsync_WhenGetCsrfTokenFails_ShouldThrowAuthenticationError()
		{
			// SETUP
			var cookieJar = new CookieJar();
			var csrfException = new FlurlHttpException(null);

			_apiClientMock.Setup(api => api.InitCookieJarAsync(It.IsAny<object>()))
				.ReturnsAsync(cookieJar);
			_apiClientMock.Setup(api => api.GetCsrfTokenAsync(It.IsAny<object>(), cookieJar))
				.ThrowsAsync(csrfException);

			// ACT & ASSERT
			var exception = Assert.ThrowsAsync<GarminAuthenticationError>(() => _authService.SignInAsync());
			exception.Code.Should().Be(Code.FailedPriorToCredentialsUsed);
			exception.Message.Should().Contain("Failed to fetch csrf token from Garmin");
		}

		[Test]
		public async Task GetGarminAuthenticationAsync_WhenOAuth1ExchangeFails_ShouldFallbackToSignIn()
		{
			// SETUP
			var oAuth1Token = new OAuth1Token { Token = "token", TokenSecret = "secret" };
			var consumerCredentials = new ConsumerCredentials { Consumer_Key = "key", Consumer_Secret = "secret" };

			_garminDbMock.Setup(db => db.GetGarminOAuth2TokenAsync(1))
				.ReturnsAsync((OAuth2Token)null);
			_garminDbMock.Setup(db => db.GetGarminOAuth1TokenAsync(1))
				.ReturnsAsync(oAuth1Token);
			_apiClientMock.Setup(api => api.GetConsumerCredentialsAsync())
				.ReturnsAsync(consumerCredentials);
			_apiClientMock.Setup(api => api.GetOAuth2TokenAsync(oAuth1Token, consumerCredentials))
				.ThrowsAsync(new Exception("OAuth2 exchange failed"));

			// Mock full sign-in flow for fallback
			var cookieJar = new CookieJar();
			var csrfResult = new GarminResult { RawResponseBody = "<input name=\"_csrf\" value=\"csrf-token\" />" };
			var credentialsResult = new SendCredentialsResult 
			{ 
				RawResponseBody = "embed?ticket=fallback-ticket\"",
				WasRedirected = false 
			};
			var oAuth1Response = "oauth_token=fallback-token&oauth_token_secret=fallback-secret";
			var oAuth2Token = new OAuth2Token 
			{ 
				Access_Token = "fallback-oauth2-token", 
				ExpiresAt = DateTime.Now.AddHours(1),
				Expires_In = 3600
			};

			_apiClientMock.Setup(api => api.InitCookieJarAsync(It.IsAny<object>()))
				.ReturnsAsync(cookieJar);
			_apiClientMock.Setup(api => api.GetCsrfTokenAsync(It.IsAny<object>(), cookieJar))
				.ReturnsAsync(csrfResult);
			_apiClientMock.Setup(api => api.SendCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<object>(), cookieJar))
				.ReturnsAsync(credentialsResult);
			_apiClientMock.Setup(api => api.GetOAuth1TokenAsync(consumerCredentials, "fallback-ticket"))
				.ReturnsAsync(oAuth1Response);
			_apiClientMock.Setup(api => api.GetOAuth2TokenAsync(It.IsAny<OAuth1Token>(), consumerCredentials))
				.ReturnsAsync(oAuth2Token);

			// ACT
			var result = await _authService.GetGarminAuthenticationAsync();

			// ASSERT
			result.Should().NotBeNull();
			result.AuthStage.Should().Be(AuthStage.Completed);
			result.OAuth2Token.Access_Token.Should().Be("fallback-oauth2-token");
		}
	}
}