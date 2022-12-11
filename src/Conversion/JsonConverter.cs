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

		public JsonConverter(ISettingsService settings, IFileHandling fileHandler) : base(settings, fileHandler) 
		{
			Format = FileFormat.Json;
		}

		protected override bool ShouldConvert(Format settings) => settings.Json;

		protected override Task<P2GWorkout> ConvertInternalAsync(Workout workout, WorkoutSamples workoutSamples, UserData userData, Settings settings)
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
	}
}
