using ActivityEncode;
using Dynastream.Fit;
using Peloton;
using PelotonToFitConsole.Converter;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PelotonToFitConsole
{
	class Program
	{
		static void Main(string[] args)
		{
			Configuration.DebugSeverity = Severity.Debug;

			FlurlConfiguration.Configure();

			RunAsync().GetAwaiter().GetResult();
		}

		static async Task RunAsync()
		{
			Console.WriteLine("Hello World!");

			//FitEncoderExample.CreateTimeBasedActivity();

			var fitConverter = new FitConverter();

			var pelotonApiClient = new ApiClient("@gmail.com", "");
			await pelotonApiClient.InitAuthAsync();

			//var recentWorkouts = await pelotonApiClient.GetWorkoutsAsync(70);
			//var wId = recentWorkouts.data.ElementAt(2);
			var wId = "1174a7ae422b4fb48ab6976d91341882"; // cadence and resistance
			var workout = await pelotonApiClient.GetWorkoutByIdAsync(wId);
			var workoutSamples = await pelotonApiClient.GetWorkoutSamplesByIdAsync(wId);
			var workoutSummary = await pelotonApiClient.GetWorkoutSummaryByIdAsync(wId);
			var response = fitConverter.Convert(workout, workoutSamples, workoutSummary);

			//foreach (var recentWorkout in recentWorkouts.data)
			//{
			//	if (recentWorkout.Status != "COMPLETE")
			//		continue;

			//	var workout = await pelotonApiClient.GetWorkoutByIdAsync(recentWorkout.Id);
			//	var workoutSamples = await pelotonApiClient.GetWorkoutSamplesByIdAsync(recentWorkout.Id);
			//	var workoutSummary = await pelotonApiClient.GetWorkoutSummaryByIdAsync(recentWorkout.Id);
			//	var response = fitConverter.Convert(workout, workoutSamples, workoutSummary);
			//}

			//Decode();
		}

		private static void Decode()
		{
			Decode decoder = new Decode();
			MesgBroadcaster mesgBroadcaster = new MesgBroadcaster();

			decoder.MesgEvent += mesgBroadcaster.OnMesg;
			decoder.MesgDefinitionEvent += mesgBroadcaster.OnMesgDefinition;

			mesgBroadcaster.ActivityMesgEvent += Write;
			mesgBroadcaster.DeviceInfoMesgEvent += Write;
			mesgBroadcaster.EventMesgEvent += Write;
			mesgBroadcaster.FileIdMesgEvent += Write;
			mesgBroadcaster.LapMesgEvent += WriteLap;
			mesgBroadcaster.SegmentLapMesgEvent += Write;
			mesgBroadcaster.SessionMesgEvent += Write;
			mesgBroadcaster.UserProfileMesgEvent += Write;
			mesgBroadcaster.WorkoutMesgEvent += WriteWorkout;
			mesgBroadcaster.WorkoutStepMesgEvent += WriteWorkoutStep;
			mesgBroadcaster.ZonesTargetMesgEvent += Write;
			mesgBroadcaster.BikeProfileMesgEvent += Write;
			mesgBroadcaster.CadenceZoneMesgEvent += Write;
			mesgBroadcaster.DeveloperDataIdMesgEvent += Write;
			mesgBroadcaster.PowerZoneMesgEvent += Write;
			mesgBroadcaster.SportMesgEvent += Write;
			mesgBroadcaster.TrainingFileMesgEvent += Write;
			mesgBroadcaster.UserProfileMesgEvent += Write;
			mesgBroadcaster.WorkoutSessionMesgEvent += Write;
			//mesgBroadcaster.RecordMesgEvent += Write;

			//FileStream fitDest = new FileStream("G:\\5837503646_ACTIVITY.fit", FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
			FileStream fitDest = new FileStream("./20_min_Classic_Rock_Ride.fit", FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
			decoder.Read(fitDest);
		}

		private static void Write(object sender, MesgEventArgs e)
		{
			Console.Out.WriteLine($"{e.mesg.Name}::");
			foreach (var f in e.mesg.Fields)
			{
				Console.Out.WriteLine($"{f.Name}::{f.GetValue()}");
			}
		}

		private static void WriteLap(object sender, MesgEventArgs e)
		{
			var lapmesg = e.mesg as LapMesg;

			Console.Out.WriteLine("LAP::");
			Console.Out.WriteLine($"{lapmesg.GetWktStepIndex()}");
			foreach (var f in lapmesg.Fields)
			{
				Console.Out.WriteLine($"{f.Name}:{f.GetValue()}");
			}
		}

		private static void WriteWorkout(object sender, MesgEventArgs e)
		{
			var lapmesg = e.mesg as WorkoutMesg;

			Console.Out.WriteLine("WORKOUT::");
			foreach (var f in lapmesg.Fields)
			{
				Console.Out.WriteLine($"{f.Name}:{f.GetValue()}");
			}
		}

		private static void WriteWorkoutStep(object sender, MesgEventArgs e)
		{
			var lapmesg = e.mesg as WorkoutStepMesg;

			Console.Out.WriteLine("WORKOUTSTEP::");
			foreach (var f in lapmesg.Fields)
			{
				Console.Out.WriteLine($"{f.Name}:{f.GetValue()}");
			}
		}
	}
}
