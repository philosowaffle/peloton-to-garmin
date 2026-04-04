using Api.Contract;
using Common.Dto;
using FluentAssertions;
using NUnit.Framework;

namespace UnitTests.Api.Contract;

public class SettingsContractsMappingTests
{
	[Test]
	public void Map_SettingsPelotonGetResponse_To_PostRequest_Preserves_Api()
	{
		var customApi = new PelotonApiSettings()
		{
			ApiUrl = "https://custom.api.com/",
			AuthDomain = "custom.auth.com",
			AuthClientId = "customClientId",
			AuthAudience = "https://custom.audience.com/",
			AuthScope = "custom_scope",
			AuthRedirectUri = "https://custom.redirect.com/callback",
			Auth0ClientPayload = "customPayload",
			AuthAuthorizePath = "/custom/authorize",
			AuthTokenPath = "/custom/token",
			BearerTokenDefaultTtlSeconds = 3600,
		};

		var response = new SettingsPelotonGetResponse()
		{
			Email = "test@example.com",
			NumWorkoutsToDownload = 5,
			Api = customApi,
		};

		var request = response.Map();

		request.Api.ApiUrl.Should().Be(customApi.ApiUrl);
		request.Api.AuthDomain.Should().Be(customApi.AuthDomain);
		request.Api.AuthClientId.Should().Be(customApi.AuthClientId);
		request.Api.AuthAudience.Should().Be(customApi.AuthAudience);
		request.Api.AuthScope.Should().Be(customApi.AuthScope);
		request.Api.AuthRedirectUri.Should().Be(customApi.AuthRedirectUri);
		request.Api.Auth0ClientPayload.Should().Be(customApi.Auth0ClientPayload);
		request.Api.AuthAuthorizePath.Should().Be(customApi.AuthAuthorizePath);
		request.Api.AuthTokenPath.Should().Be(customApi.AuthTokenPath);
		request.Api.BearerTokenDefaultTtlSeconds.Should().Be(customApi.BearerTokenDefaultTtlSeconds);
	}

	[Test]
	public void Map_SettingsPelotonPostRequest_To_PelotonSettings_Preserves_Api()
	{
		var customApi = new PelotonApiSettings()
		{
			ApiUrl = "https://custom.api.com/",
			AuthDomain = "custom.auth.com",
			BearerTokenDefaultTtlSeconds = 3600,
		};

		var request = new SettingsPelotonPostRequest()
		{
			Email = "test@example.com",
			NumWorkoutsToDownload = 5,
			Api = customApi,
		};

		var settings = request.Map();

		settings.Api.ApiUrl.Should().Be(customApi.ApiUrl);
		settings.Api.AuthDomain.Should().Be(customApi.AuthDomain);
		settings.Api.BearerTokenDefaultTtlSeconds.Should().Be(customApi.BearerTokenDefaultTtlSeconds);
	}

	[Test]
	public void SettingsGetResponse_Constructor_Maps_PelotonApi()
	{
		var customApi = new PelotonApiSettings()
		{
			ApiUrl = "https://custom.api.com/",
			AuthDomain = "custom.auth.com",
		};

		var settings = new Settings()
		{
			Peloton = new PelotonSettings()
			{
				Email = "test@example.com",
				Api = customApi,
			},
			Garmin = new GarminSettings(),
		};

		var response = new SettingsGetResponse(settings);

		response.Peloton.Api.ApiUrl.Should().Be(customApi.ApiUrl);
		response.Peloton.Api.AuthDomain.Should().Be(customApi.AuthDomain);
	}

	[Test]
	public void SettingsGetResponse_Constructor_With_Null_PelotonApi_Uses_Defaults()
	{
		var settings = new Settings()
		{
			Peloton = new PelotonSettings() { Api = null },
			Garmin = new GarminSettings(),
		};

		var response = new SettingsGetResponse(settings);

		response.Peloton.Api.Should().NotBeNull();
		response.Peloton.Api.ApiUrl.Should().Be(new PelotonApiSettings().ApiUrl);
	}
}
