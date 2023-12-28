using Common.Dto;
using Common.Observe;
using JsonFlatFileDataStore;
using Serilog;
using System;
using System.IO;

namespace Common.Database
{
    public abstract class DbBase<T>
    {
		private static readonly ILogger _logger = LogContext.ForClass<DbBase<T>>();

		private IFileHandling _fileHandler;

		protected Lazy<IDocumentCollection<T>> _table;

		protected readonly string DbName;
		protected readonly string DbPath;

		public DbBase(string dbName, IFileHandling fileHandler)
		{
			DbName = dbName;
			DbPath = Path.Join(App.DataDirectory, $"{dbName}Db.json");
			_fileHandler = fileHandler;

			CreateDbIfNotExist(DbPath, DbName);
		}

		protected void CreateDbIfNotExist(string path, string dbName)
		{
			if (!File.Exists(path))
			{
				_logger.Debug("Creating {@DbName} db: {@Path}", dbName, path);
				try
				{
					var dir = Path.GetDirectoryName(path);
					_fileHandler.MkDirIfNotExists(dir);
					File.WriteAllText(path, "{}");

				}
				catch (Exception e)
				{
					_logger.Error(e, "Failed to create {@DbName} db file: {@Path}", dbName, path);
					throw;
				}
			}
		}
	}
}
