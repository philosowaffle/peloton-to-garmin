using Common;
using Common.Dto;
using Common.Observe;
using Common.Service;
using Serilog;
using System.Text.Json;
using System.Threading.Tasks;

namespace Conversion
{
	public class JsonConverter : Converter<P2GWorkout>
	{
		private static readonly ILogger _logger = LogContext.ForClass<JsonConverter>();

		public JsonConverter(ISettingsService settings, IFileHandling fileHandler, IElevationGainCalculatorService elevationGainCalculatorService) : base(settings, fileHandler, elevationGainCalculatorService) 
		{
			Format = FileFormat.Json;
		}

		protected override bool ShouldConvert(Format settings) => settings.Json;

		protected override Task<P2GWorkout> ConvertInternalAsync(P2GWorkout workoutData, Settings settings, bool forceElevationGainCalculation = false)
		{
			return Task.FromResult(workoutData);
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
