﻿using Common;
using Common.Database;
using Conversion;
using FluentAssertions;
using Garmin;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Peloton;
using Sync;
using System;
using System.Threading.Tasks;

namespace UnitTests.Sync
{
	public class SyncServiceTests
	{

		[Test]
		public async Task SyncAsync_When_PelotonStep_Fails_Should_ReturnCorrectResponse()
		{
			// SETUP
			var mocker = new AutoMocker();

			var config = new Settings();
			var service = mocker.CreateInstance<SyncService>();
			var peloton = mocker.GetMock<IPelotonService>();
			var db = mocker.GetMock<ISyncStatusDb>();

			var syncStatus = new SyncServiceStatus();
			db.Setup(x => x.GetSyncStatusAsync()).Returns(Task.FromResult(syncStatus));
			peloton.Setup(x => x.DownloadLatestWorkoutDataAsync(0)).Throws(new Exception());

			// ACT
			var response = await service.SyncAsync(0);

			// ASSERT
			response.SyncSuccess.Should().BeFalse();
			response.PelotonDownloadSuccess.Should().BeFalse();
			response.Errors.Should().NotBeNullOrEmpty();

			peloton.Verify(x => x.DownloadLatestWorkoutDataAsync(0), Times.Once);
			db.Verify(x => x.UpsertSyncStatusAsync(It.IsAny<SyncServiceStatus>()), Times.Once);
		}

		[Test]
		public async Task SyncAsync_When_Conversion_StepFails_Should_ReturnCorrectResponse()
		{
			// SETUP
			var mocker = new AutoMocker();

			var config = new Settings();
			var service = mocker.CreateInstance<SyncService>();
			var peloton = mocker.GetMock<IPelotonService>();
			var db = mocker.GetMock<ISyncStatusDb>();
			var converter = mocker.GetMock<IConverter>();

			var syncStatus = new SyncServiceStatus();
			db.Setup(x => x.GetSyncStatusAsync()).Returns(Task.FromResult(syncStatus));
			peloton.Setup(x => x.DownloadLatestWorkoutDataAsync(0)).Returns(Task.CompletedTask);
			converter.Setup(x => x.Convert()).Throws(new Exception());

			// ACT
			var response = await service.SyncAsync(0);

			// ASSERT
			response.SyncSuccess.Should().BeFalse();
			response.PelotonDownloadSuccess.Should().BeTrue();
			response.ConversionSuccess.Should().BeFalse();
			response.Errors.Should().NotBeNullOrEmpty();

			peloton.Verify(x => x.DownloadLatestWorkoutDataAsync(0), Times.Once);
			converter.Verify(x => x.Convert(), Times.Once);
			db.Verify(x => x.UpsertSyncStatusAsync(It.IsAny<SyncServiceStatus>()), Times.Once);
		}

		[Test]
		public async Task SyncAsync_When_GarminUpload_StepFails_Should_ReturnCorrectResponse()
		{
			// SETUP
			var mocker = new AutoMocker();

			var config = new Settings();
			var service = mocker.CreateInstance<SyncService>();
			var peloton = mocker.GetMock<IPelotonService>();
			var db = mocker.GetMock<ISyncStatusDb>();
			var converter = mocker.GetMock<IConverter>();
			var garmin = mocker.GetMock<IGarminUploader>();

			var syncStatus = new SyncServiceStatus();
			db.Setup(x => x.GetSyncStatusAsync()).Returns(Task.FromResult(syncStatus));
			peloton.Setup(x => x.DownloadLatestWorkoutDataAsync(0)).Returns(Task.CompletedTask);
			garmin.Setup(x => x.UploadToGarminAsync()).Throws(new Exception());

			// ACT
			var response = await service.SyncAsync(0);

			// ASSERT
			response.SyncSuccess.Should().BeFalse();
			response.PelotonDownloadSuccess.Should().BeTrue();
			response.ConversionSuccess.Should().BeTrue();
			response.UploadToGarminSuccess.Should().BeFalse();
			response.Errors.Should().NotBeNullOrEmpty();

			peloton.Verify(x => x.DownloadLatestWorkoutDataAsync(0), Times.Once);
			converter.Verify(x => x.Convert(), Times.Once);
			garmin.Verify(x => x.UploadToGarminAsync(), Times.Once);
			db.Verify(x => x.UpsertSyncStatusAsync(It.IsAny<SyncServiceStatus>()), Times.Once);
		}

		[Test]
		public async Task SyncAsync_When_SyncSuccess_Should_ReturnCorrectResponse()
		{
			// SETUP
			var mocker = new AutoMocker();

			var config = new Settings();
			var service = mocker.CreateInstance<SyncService>();
			var peloton = mocker.GetMock<IPelotonService>();
			var db = mocker.GetMock<ISyncStatusDb>();
			var converter = mocker.GetMock<IConverter>();
			var garmin = mocker.GetMock<IGarminUploader>();
			var fileHandler = mocker.GetMock<IFileHandling>();

			var syncStatus = new SyncServiceStatus();
			db.Setup(x => x.GetSyncStatusAsync()).Returns(Task.FromResult(syncStatus));
			peloton.Setup(x => x.DownloadLatestWorkoutDataAsync(0)).Returns(Task.CompletedTask);

			// ACT
			var response = await service.SyncAsync(0);

			// ASSERT
			response.SyncSuccess.Should().BeTrue();
			response.PelotonDownloadSuccess.Should().BeTrue();
			response.ConversionSuccess.Should().BeTrue();
			response.UploadToGarminSuccess.Should().BeTrue();
			response.Errors.Should().BeNullOrEmpty();

			peloton.Verify(x => x.DownloadLatestWorkoutDataAsync(0), Times.Once);
			converter.Verify(x => x.Convert(), Times.Once);
			garmin.Verify(x => x.UploadToGarminAsync(), Times.Once);
			db.Verify(x => x.UpsertSyncStatusAsync(It.IsAny<SyncServiceStatus>()), Times.Once);
			fileHandler.Verify(x => x.Cleanup(It.IsAny<string>()), Times.Exactly(3));
		}
	}
}
