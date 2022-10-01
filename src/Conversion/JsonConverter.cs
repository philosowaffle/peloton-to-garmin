using Common;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Observe;
using Common.Service;
using Serilog;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Conversion
{
	public class JsonConverter : Converter<P2GWorkout>
	{
		private static readonly ILogger _logger = LogContext.ForClass<JsonConverter>();

		public JsonConverter(ISettingsService settings, IFileHandling fileHandler) : base(settings, fileHandler) { }

		public override async Task<ConvertStatus> ConvertAsync(P2GWorkout workoutData)
		{
			var settings = await _settingsService.GetSettingsAsync();
			if (!settings.Format.Json) return new ConvertStatus() { Result = ConversionResult.Skipped };

			return await ConvertForFormatAsync(FileFormat.Json, workoutData, settings);
		}

		protected override Task<P2GWorkout> ConvertAsync(Workout workout, WorkoutSamples workoutSamples, UserData userData, Settings settings)
		{
			var result = new P2GWorkout() { UserData = userData, Workout = workout, WorkoutSamples = workoutSamples };
			return Task.FromResult(result);
		}

		protected override void Save(P2GWorkout data, string path)
		{
			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(Save)}")
										.WithTag(TagKey.Format, FileFormat.Json.ToString());

			var serializedData = JsonSerializer.Serialize(data, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, WriteIndented = true });
			_fileHandler.WriteToFile(path, serializedData.ToString());
		}

		protected override void SaveLocalCopy(string sourcePath, string workoutTitle, Settings settings)
		{
			if (!settings.Format.Json) return;

			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(Save)}")
										.WithTag(TagKey.Format, FileFormat.Json.ToString());

			_fileHandler.MkDirIfNotExists(settings.App.JsonDirectory);

			var backupDest = Path.Join(settings.App.JsonDirectory, $"{workoutTitle}.json");
			_fileHandler.Copy(sourcePath, backupDest, overwrite: true);
			_logger.Information("[@Format] Backed up file {@File}", FileFormat.Fit, backupDest);
		}
	}
}
