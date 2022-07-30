using Common;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Observe;
using Serilog;
using System.IO;
using System.Text.Json;

namespace Conversion
{
	public class JsonConverter : Converter<P2GWorkout>
	{
		private static readonly ILogger _logger = LogContext.ForClass<JsonConverter>();

		public JsonConverter(Settings settings, IFileHandling fileHandler) : base(settings, fileHandler) { }

		public override ConvertStatus Convert(P2GWorkout workoutData)
		{
			if (!_config.Format.Json) return new ConvertStatus() { Success = true, ErrorMessage = "Json format disabled in config." };

			return base.ConvertForFormat(FileFormat.Json, workoutData);
		}

		protected override P2GWorkout Convert(Workout workout, WorkoutSamples workoutSamples, UserData userData)
		{
			var result = new P2GWorkout() { UserData = userData, Workout = workout, WorkoutSamples = workoutSamples };
			return result;
		}

		protected override void Save(P2GWorkout data, string path)
		{
			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(Save)}")
										.WithTag(TagKey.Format, FileFormat.Json.ToString());

			var serializedData = JsonSerializer.Serialize(data, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, WriteIndented = true });
			_fileHandler.WriteToFile(path, serializedData.ToString());
		}

		protected override void SaveLocalCopy(string sourcePath, string workoutTitle)
		{
			if (!_config.Format.Json) return;

			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(Save)}")
										.WithTag(TagKey.Format, FileFormat.Json.ToString());

			_fileHandler.MkDirIfNotExists(_config.App.JsonDirectory);

			var backupDest = Path.Join(_config.App.JsonDirectory, $"{workoutTitle}.json");
			_fileHandler.Copy(sourcePath, backupDest, overwrite: true);
			_logger.Information("[@Format] Backed up file {@File}", FileFormat.Fit, backupDest);
		}
	}
}
