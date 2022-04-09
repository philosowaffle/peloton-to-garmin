using Common;
using Common.Dto;
using Common.Dto.Peloton;
using Common.Observe;

namespace Conversion
{
	public class JsonConverter : Converter<dynamic>
	{
		public JsonConverter(Settings settings, IFileHandling fileHandler) : base(settings, fileHandler) { }

		public override void Convert()
		{
			if (!_config.Format.Json) return;

			base.Convert(FileFormat.Json);
		}

		public override ConvertStatus Convert(P2GWorkout workoutData)
		{
			if (!_config.Format.Json) return new ConvertStatus() { Success = true, ErrorMessage = "Json format disabled in config." };

			return base.Convert(FileFormat.Json, workoutData);
		}

		protected override dynamic Convert(Workout workout, WorkoutSamples workoutSamples)
		{
			dynamic result = new { workout = workout, workoutSamples = workoutSamples };
			return result;
		}

		protected override void Save(dynamic data, string path)
		{
			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(Save)}")
										.WithTag(TagKey.Format, FileFormat.Json.ToString());

			_fileHandler.WriteToFile(path, data.ToString());
		}
	}
}
