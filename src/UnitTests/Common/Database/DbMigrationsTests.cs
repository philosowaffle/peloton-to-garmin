#pragma warning disable CS0612 // Type or member is obsolete

using Common;
using Common.Database;
using Common.Dto;
using Common.Dto.P2G;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Sync.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnitTests.Common.Database;

public class DbMigrationsTests
{
	[Test]
	public async Task MigrateToAdminUserAsync_When_NoLegacySettings_NoOp()
	{
		var mocker = new AutoMocker();
		var service = mocker.CreateInstance<DbMigrations>();
		var settingsDb = mocker.GetMock<ISettingsDb>();

		settingsDb.Setup(x => x.DbMigrations_TryGetLegacy_Settings())
			.Returns((Settings)null)
			.Verifiable();

		await service.MigrateToAdminUserAsync();

		settingsDb.Verify();
		settingsDb.Verify(s => s.UpsertSettingsAsync(It.IsAny<int>(), It.IsAny<Settings>()), Times.Never);
		settingsDb.Verify(x => x.DbMigrations_TryRemoveLegacySettingsAsync(), Times.Never);
		mocker.GetMock<IUsersDb>().Verify(x => x.GetUsersAsync(), Times.Never);
		mocker.GetMock<ISyncStatusDb>().Verify(x => x.DeleteLegacySyncStatusAsync(), Times.Never);
	}

	[Test]
	public async Task MigrateToAdminUserAsync_When_LegacySettings_Migrates()
	{
		var mocker = new AutoMocker();
		var service = mocker.CreateInstance<DbMigrations>();
		var settingsDb = mocker.GetMock<ISettingsDb>();
		var usersDb = mocker.GetMock<IUsersDb>();

		var settings = new Settings()
		{
			App = new App() { EnablePolling = true },
		};

		settingsDb.Setup(x => x.DbMigrations_TryGetLegacy_Settings())
			.Returns(settings)
			.Verifiable();

		settingsDb.Setup(x => x.UpsertSettingsAsync(1, settings))
			.ReturnsAsync(true)
			.Verifiable();

		usersDb.Setup(x => x.GetUsersAsync())
			.ReturnsAsync(new List<P2GUser>() { new P2GUser() { Id = 1 } });

		await service.MigrateToAdminUserAsync();

		settingsDb.Verify();
		settingsDb.Verify(x => x.DbMigrations_TryRemoveLegacySettingsAsync(), Times.Once);
		usersDb.Verify(x => x.GetUsersAsync(), Times.Once);
	}

	[Test]
	public async Task MigrateToAdminUserAsync_When_FailsToMigrate_DoesNotThrow()
	{
		var mocker = new AutoMocker();
		var service = mocker.CreateInstance<DbMigrations>();
		var settingsDb = mocker.GetMock<ISettingsDb>();
		var usersDb = mocker.GetMock<IUsersDb>();

		var settings = new Settings()
		{
			App = new App() { EnablePolling = true },
		};

		settingsDb.Setup(x => x.DbMigrations_TryGetLegacy_Settings())
			.Returns(settings)
			.Verifiable();

		settingsDb.Setup(x => x.UpsertSettingsAsync(1, settings))
			.ThrowsAsync(new Exception())
			.Verifiable();

		usersDb.Setup(x => x.GetUsersAsync())
			.ReturnsAsync(new List<P2GUser>() { new P2GUser() { Id = 1 } });

		await service.MigrateToAdminUserAsync();

		settingsDb.Verify();
		settingsDb.Verify(x => x.DbMigrations_TryRemoveLegacySettingsAsync(), Times.Never);
		usersDb.Verify(x => x.GetUsersAsync(), Times.Once);
	}
}
