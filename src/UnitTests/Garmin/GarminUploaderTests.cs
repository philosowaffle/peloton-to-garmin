using Common.Dto;
using Common.Service;
using FluentAssertions;
using Garmin;
using Garmin.Auth;
using Garmin.Dto;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace UnitTests.Garmin
{
	public class GarminUploaderTests
	{
		private Mock<ISettingsService> _settingsServiceMock;
		private Mock<IGarminApiClient> _apiClientMock;
		private Mock<IGarminAuthenticationService> _authServiceMock;
		private GarminUploader _uploader;

		[SetUp]
		public void SetUp()
		{
			_settingsServiceMock = new Mock<ISettingsService>();
			_apiClientMock = new Mock<IGarminApiClient>();
			_authServiceMock = new Mock<IGarminAuthenticationService>();
			
			_uploader = new GarminUploader(_settingsServiceMock.Object, _apiClientMock.Object, _authServiceMock.Object);
		}

		[Test]
		public async Task UploadToGarminAsync_WhenUploadDisabled_ShouldReturnWithoutProcessing()
		{
			// SETUP
			var settings = new Settings
			{
				Garmin = new GarminSettings { Upload = false },
				App = new App()
			};
			_settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);

			// ACT
			await _uploader.UploadToGarminAsync();

			// ASSERT
			_apiClientMock.Verify(a => a.UploadActivity(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GarminApiAuthentication>()), Times.Never);
			_authServiceMock.Verify(a => a.GetGarminAuthenticationAsync(), Times.Never);
		}

		[Test]
		public async Task UploadToGarminAsync_WhenUploadDirectoryDoesNotExist_ShouldReturnWithoutProcessing()
		{
			// SETUP
			var settings = new Settings
			{
				Garmin = new GarminSettings { Upload = true },
				App = new App()
			};
			
			_settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);

			// ACT
			await _uploader.UploadToGarminAsync();

			// ASSERT - Should exit early without calling auth or upload since default upload directory doesn't exist
			_apiClientMock.Verify(a => a.UploadActivity(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GarminApiAuthentication>()), Times.Never);
			_authServiceMock.Verify(a => a.GetGarminAuthenticationAsync(), Times.Never);
		}

		[Test]
		public void ValidateConfig_WhenUploadDisabled_ShouldNotValidateCredentials()
		{
			// SETUP
			var config = new Settings
			{
				Garmin = new GarminSettings { Upload = false }
			};

			// ACT & ASSERT
			// Should not throw since upload is disabled
			Assert.DoesNotThrow(() => GarminUploader.ValidateConfig(config));
		}

		[Test]
		public void ValidateConfig_WhenUploadEnabled_ShouldValidateCredentials()
		{
			// SETUP
			var config = new Settings
			{
				Garmin = new GarminSettings 
				{ 
					Upload = true, 
					Email = "test@example.com", 
					Password = "password" 
				}
			};

			// ACT & ASSERT
			// This should call config.Garmin.EnsureGarminCredentialsAreProvided()
			Assert.DoesNotThrow(() => GarminUploader.ValidateConfig(config));
		}

		[Test]
		public void Constructor_ShouldInitializeCorrectly()
		{
			// SETUP & ACT
			var uploader = new GarminUploader(_settingsServiceMock.Object, _apiClientMock.Object, _authServiceMock.Object);

			// ASSERT
			uploader.Should().NotBeNull();
		}

		[Test]
		public void UploadToGarminAsync_WhenSettingsServiceThrows_ShouldPropagateException()
		{
			// SETUP
			_settingsServiceMock.Setup(s => s.GetSettingsAsync())
				.ThrowsAsync(new Exception("Settings service error"));

			// ACT & ASSERT
			Assert.ThrowsAsync<Exception>(() => _uploader.UploadToGarminAsync());
		}

		[Test]
		public void ValidateConfig_WhenConfigIsNull_ShouldThrowException()
		{
			// SETUP
			Settings config = null;

			// ACT & ASSERT
			Assert.Throws<NullReferenceException>(() => GarminUploader.ValidateConfig(config));
		}

		[Test]
		public void ValidateConfig_WhenGarminSettingsIsNull_ShouldThrowException()
		{
			// SETUP
			var config = new Settings
			{
				Garmin = null
			};

			// ACT & ASSERT
			Assert.Throws<NullReferenceException>(() => GarminUploader.ValidateConfig(config));
		}

		[Test]
		public async Task UploadToGarminAsync_WhenNoFilesToUpload_ShouldReturnEarlyWithoutAuthentication()
		{
			// SETUP
			var settings = new Settings
			{
				Garmin = new GarminSettings { Upload = true },
				App = new App()
			};

			_settingsServiceMock.Setup(s => s.GetSettingsAsync()).ReturnsAsync(settings);

			// ACT
			await _uploader.UploadToGarminAsync();

			// ASSERT - Should not attempt authentication or upload when no files exist
			_authServiceMock.Verify(a => a.GetGarminAuthenticationAsync(), Times.Never);
			_apiClientMock.Verify(a => a.UploadActivity(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GarminApiAuthentication>()), Times.Never);
		}
	}
}