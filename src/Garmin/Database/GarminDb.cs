using Common;
using Common.Database;
using Common.Helpers;
using Common.Observe;
using Garmin.Dto;
using JsonFlatFileDataStore;
using Prometheus;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Garmin.Database;

public interface IGarminDb
{
	Task<OAuth1Token> GetGarminOAuth1TokenAsync(int userId);
	Task UpsertGarminOAuth1TokenAsync(int userId, OAuth1Token token);

	Task<OAuth2Token> GetGarminOAuth2TokenAsync(int userId);
	Task UpsertGarminOAuth2TokenAsync(int userId, OAuth2Token token);

	Task<NativeOAuth2Session> GetNativeOAuth2SessionAsync(int userId);
	Task UpsertNativeOAuth2SessionAsync(int userId, NativeOAuth2Session session);

	Task<StagedPartialGarminAuthentication> GetStagedPartialGarminAuthenticationAsync(int userId);
	Task UpsertPartialGarminAuthenticationAsync(int userId, StagedPartialGarminAuthentication partialGarminAuthentication);
}

public class GarminDb : DbBase<P2GGarminData>, IGarminDb
{
	private static readonly ILogger _logger = LogContext.ForClass<GarminDb>();
	private static readonly P2GGarminData _defaultData = new P2GGarminData();

	private readonly DataStore _db;

	public GarminDb(IFileHandling fileHandling) : base("GarminDb", fileHandling)
	{
		_db = new DataStore(DbPath);
	}

	public Task<StagedPartialGarminAuthentication> GetStagedPartialGarminAuthenticationAsync(int userId)
	{
		using var metrics = DbMetrics.DbActionDuration
									.WithLabels("getStagedPartialGarminAuthentication", DbName)
									.NewTimer();
		using var tracing = Tracing.Trace($"{nameof(GarminDb)}.{nameof(GetStagedPartialGarminAuthenticationAsync)}", TagValue.Db)
									.WithTable(DbName);

		try
		{
			_db.TryGetItem<P2GGarminData>(userId, out var data);

			if (data is null
				|| data.PartialGarminAuthentication is null
				|| data.PartialGarminAuthentication.ExpiresAt < DateTime.Now) return Task.FromResult((StagedPartialGarminAuthentication)null);

			return Task.FromResult(data.PartialGarminAuthentication);
		}
		catch (Exception e)
		{
			_logger.Error(e, "Failed to get garmin cookie jar from db");
			throw;
		}
	}

	public Task<OAuth1Token> GetGarminOAuth1TokenAsync(int userId)
	{
		using var metrics = DbMetrics.DbActionDuration
									.WithLabels("getOAuth1", DbName)
									.NewTimer();
		using var tracing = Tracing.Trace($"{nameof(GarminDb)}.{nameof(GetGarminOAuth1TokenAsync)}", TagValue.Db)
									.WithTable(DbName);

		try
		{
			_db.TryGetItem<P2GGarminData>(userId, out var data);

			if (string.IsNullOrWhiteSpace(data?.OAuth1Token)) return Task.FromResult((OAuth1Token)null);

			var decrytedTokenString = data.OAuth1Token.Decrypt();
			var token = _fileHandler.DeserializeJson<OAuth1Token>(decrytedTokenString);
			return Task.FromResult(token);
		}
		catch (Exception e)
		{
			_logger.Error(e, "Failed to get oauth1 from db");
			throw;
		}
	}

	public Task<OAuth2Token> GetGarminOAuth2TokenAsync(int userId)
	{
		using var metrics = DbMetrics.DbActionDuration
									.WithLabels("getOAuth2", DbName)
									.NewTimer();
		using var tracing = Tracing.Trace($"{nameof(GarminDb)}.{nameof(GetGarminOAuth2TokenAsync)}", TagValue.Db)
									.WithTable(DbName);

		try
		{
			_db.TryGetItem<P2GGarminData>(userId, out var data);

			if (string.IsNullOrWhiteSpace(data?.OAuth1Token)) return Task.FromResult((OAuth2Token)null);

			var decrytedTokenString = data.OAuth2Token.Decrypt();
			var token = _fileHandler.DeserializeJson<OAuth2Token>(decrytedTokenString);
			return Task.FromResult(token);
		}
		catch (Exception e)
		{
			_logger.Error(e, "Failed to get oauth2 from db");
			throw;
		}
	}

	public Task UpsertPartialGarminAuthenticationAsync(int userId, StagedPartialGarminAuthentication partialGarminAuthentication)
	{
		using var metrics = DbMetrics.DbActionDuration
									.WithLabels("upsertPartialGarminAuthentication", DbName)
									.NewTimer();
		using var tracing = Tracing.Trace($"{nameof(GarminDb)}.{nameof(UpsertPartialGarminAuthenticationAsync)}", TagValue.Db)
									.WithTable(DbName);
		try
		{
			_db.TryGetItem<P2GGarminData>(userId, out var data);

			if (data is null)
				data = new P2GGarminData();

			data.PartialGarminAuthentication = partialGarminAuthentication;

			return _db.ReplaceItemAsync(userId.ToString(), data, upsert: true);
		}
		catch (Exception e)
		{
			_logger.Error(e, "Failed to upsert garmin oAuth1 token");
			return Task.FromResult(false);
		}
	}

	public Task UpsertGarminOAuth1TokenAsync(int userId, OAuth1Token token)
	{
		using var metrics = DbMetrics.DbActionDuration
									.WithLabels("upsertOAuth1", DbName)
									.NewTimer();
		using var tracing = Tracing.Trace($"{nameof(GarminDb)}.{nameof(UpsertGarminOAuth1TokenAsync)}", TagValue.Db)
									.WithTable(DbName);
		try
		{
			_db.TryGetItem<P2GGarminData>(userId, out var data);

			if (data is null)
				data = new P2GGarminData();

			var serialized = _fileHandler.SerializeToJson(token);
			var encrypted = serialized.Encrypt();

			data.EncryptionVersion = EncryptionVersion.V1;
			data.OAuth1Token = encrypted;

			return _db.ReplaceItemAsync(userId.ToString(), data, upsert: true);
		}
		catch (Exception e)
		{
			_logger.Error(e, "Failed to upsert garmin oAuth1 token");
			return Task.FromResult(false);
		}
	}

	public Task UpsertGarminOAuth2TokenAsync(int userId, OAuth2Token token)
	{
		using var metrics = DbMetrics.DbActionDuration
									.WithLabels("upsertOAuth2", DbName)
									.NewTimer();
		using var tracing = Tracing.Trace($"{nameof(GarminDb)}.{nameof(UpsertGarminOAuth2TokenAsync)}", TagValue.Db)
									.WithTable(DbName);
		try
		{
			_db.TryGetItem<P2GGarminData>(userId, out var data);

			if (data is null)
				data = new P2GGarminData();

			var serialized = _fileHandler.SerializeToJson(token);
			var encrypted = serialized.Encrypt();

			data.EncryptionVersion = EncryptionVersion.V1;
			data.OAuth2Token = encrypted;

			return _db.ReplaceItemAsync(userId.ToString(), data, upsert: true);
		}
		catch (Exception e)
		{
			_logger.Error(e, "Failed to upsert garmin oAuth1 token");
			return Task.FromResult(false);
		}
	}

	public Task<NativeOAuth2Session> GetNativeOAuth2SessionAsync(int userId)
	{
		using var metrics = DbMetrics.DbActionDuration
									.WithLabels("getNativeOAuth2Session", DbName)
									.NewTimer();
		using var tracing = Tracing.Trace($"{nameof(GarminDb)}.{nameof(GetNativeOAuth2SessionAsync)}", TagValue.Db)
									.WithTable(DbName);

		try
		{
			_db.TryGetItem<P2GGarminData>(userId, out var data);

			if (string.IsNullOrWhiteSpace(data?.NativeOAuth2Session)) return Task.FromResult((NativeOAuth2Session)null);

			var decryptedString = data.NativeOAuth2Session.Decrypt();
			var session = _fileHandler.DeserializeJson<NativeOAuth2Session>(decryptedString);
			return Task.FromResult(session);
		}
		catch (Exception e)
		{
			_logger.Error(e, "Failed to get native oauth2 session from db");
			throw;
		}
	}

	public Task UpsertNativeOAuth2SessionAsync(int userId, NativeOAuth2Session session)
	{
		using var metrics = DbMetrics.DbActionDuration
									.WithLabels("upsertNativeOAuth2Session", DbName)
									.NewTimer();
		using var tracing = Tracing.Trace($"{nameof(GarminDb)}.{nameof(UpsertNativeOAuth2SessionAsync)}", TagValue.Db)
									.WithTable(DbName);
		try
		{
			_db.TryGetItem<P2GGarminData>(userId, out var data);

			if (data is null)
				data = new P2GGarminData();

			if (session is null)
			{
				data.NativeOAuth2Session = null;
			}
			else
			{
				var serialized = _fileHandler.SerializeToJson(session);
				var encrypted = serialized.Encrypt();
				data.EncryptionVersion = EncryptionVersion.V1;
				data.NativeOAuth2Session = encrypted;
			}

			return _db.ReplaceItemAsync(userId.ToString(), data, upsert: true);
		}
		catch (Exception e)
		{
			_logger.Error(e, "Failed to upsert native oauth2 session");
			return Task.FromResult(false);
		}
	}
}
