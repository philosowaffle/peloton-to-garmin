using Common;
using Common.Dto;
using Common.Dto.Garmin;
using Common.Dto.Peloton;
using Common.Helpers;
using Common.Observe;
using Common.Service;
using Dynastream.Fit;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Conversion
{
	public class FitConverter : Converter<Tuple<string, ICollection<Mesg>>>
	{
		private static readonly ILogger _logger = LogContext.ForClass<FitConverter>();
		public FitConverter(ISettingsService settings, IFileHandling fileHandler) : base(settings, fileHandler) 
		{
			Format = FileFormat.Fit;
		}

		protected override bool ShouldConvert(Format settings) => settings.Fit;

		protected override void Save(Tuple<string, ICollection<Mesg>> data, string path)
		{
			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(Save)}")
										.WithTag(TagKey.Format, FileFormat.Fit.ToString());

			using (FileStream fitDest = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
			{
				Encode encoder = new Encode(ProtocolVersion.V20);
				try
				{
					encoder.Open(fitDest);
					encoder.Write(data.Item2);
				}
				finally
				{
					encoder.Close();
				}
			}
		}

		protected override async Task<Tuple<string, ICollection<Mesg>>> ConvertInternalAsync(Workout workout, WorkoutSamples workoutSamples, UserData userData, Settings settings)
		{
			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(ConvertAsync)}")
										.WithTag(TagKey.Format, FileFormat.Fit.ToString())
										.WithWorkoutId(workout.Id);

			// MESSAGE ORDER MATTERS
			var messages = new List<Mesg>();

			var startTime = GetStartTimeUtc(workout);
			var endTime = GetEndTimeUtc(workout, workoutSamples);
			var title = WorkoutHelper.GetTitle(workout);
			var sport = GetGarminSport(workout);
			var subSport = GetGarminSubSport(workout);
			var deviceInfo = await GetDeviceInfoAsync(workout.Fitness_Discipline, settings);

			if (sport == Sport.Invalid)
			{
				_logger.Warning("Unsupported Sport Type - Skipping {@Sport}", workout.Fitness_Discipline);
				return new Tuple<string, ICollection<Mesg>>(string.Empty, null);
			}

			var fileIdMesg = new FileIdMesg();
			fileIdMesg.SetSerialNumber(deviceInfo.UnitId);
			fileIdMesg.SetTimeCreated(startTime);
			fileIdMesg.SetManufacturer(deviceInfo.ManufacturerId);
			fileIdMesg.SetProduct(deviceInfo.ProductID);
			fileIdMesg.SetType(Dynastream.Fit.File.Activity);
			messages.Add(fileIdMesg);

			var eventMesg = new EventMesg();
			eventMesg.SetTimestamp(startTime);
			eventMesg.SetData(0);
			eventMesg.SetEvent(Event.Timer);
			eventMesg.SetEventType(EventType.Start);
			eventMesg.SetEventGroup(0);
			messages.Add(eventMesg);

			var deviceInfoMesg = GetDeviceInfoMesg(deviceInfo, startTime);
			messages.Add(deviceInfoMesg);

			var userProfileMesg = new UserProfileMesg();
			userProfileMesg.SetPowerSetting(DisplayPower.PercentFtp);
			messages.Add(userProfileMesg);

			var sportMesg = new SportMesg();
			sportMesg.SetSport(sport);
			sportMesg.SetSubSport(subSport);
			messages.Add(sportMesg);

			var zoneTargetMesg = new ZonesTargetMesg();

			if (sport == Sport.Cycling)
			{
				zoneTargetMesg.SetFunctionalThresholdPower(GetCyclingFtp(workout, userData));
				zoneTargetMesg.SetPwrCalcType(PwrZoneCalc.PercentFtp);
			}
			
			var maxHr = GetUserMaxHeartRate(workoutSamples);
			if (maxHr is object)
			{
				zoneTargetMesg.SetMaxHeartRate(maxHr.Value);
				zoneTargetMesg.SetHrCalcType(HrZoneCalc.PercentMaxHr);
			}
			messages.Add(zoneTargetMesg);

			var trainingMesg = new TrainingFileMesg();
			trainingMesg.SetTimestamp(startTime);
			trainingMesg.SetTimeCreated(startTime);
			trainingMesg.SetSerialNumber(deviceInfo.UnitId);
			trainingMesg.SetManufacturer(deviceInfo.ManufacturerId);
			trainingMesg.SetProduct(deviceInfo.ProductID);
			trainingMesg.SetType(Dynastream.Fit.File.Workout);
			messages.Add(trainingMesg);

			AddMetrics(messages, workoutSamples, sport, startTime);

			var workoutMesg = new WorkoutMesg();
			workoutMesg.SetWktName(title.Replace(WorkoutHelper.SpaceSeparator, ' '));
			workoutMesg.SetCapabilities(32);
			workoutMesg.SetSport(sport);
			workoutMesg.SetSubSport(subSport);

			var lapCount = 0;

			if (subSport == SubSport.StrengthTraining)
			{
				var sets = GetStrengthWorkoutSteps(workout, startTime, settings);

				// Add sets in order
				foreach (var set in sets)
					messages.Add(set);

			} else
			{
				var workoutSteps = new List<WorkoutStepMesg>();
				var laps = new List<LapMesg>();
				var preferredLapType = PreferredLapType.Default;

				if (sport == Sport.Cycling)
					preferredLapType = settings.Format.Cycling.PreferredLapType;
				if (sport == Sport.Running)
					preferredLapType = settings.Format.Running.PreferredLapType;
				if (sport == Sport.Rowing)
					preferredLapType = settings.Format.Rowing.PreferredLapType;

				if ((preferredLapType == PreferredLapType.Class_Targets || preferredLapType == PreferredLapType.Default)
					&& workoutSamples.Target_Performance_Metrics?.Target_Graph_Metrics?.FirstOrDefault(w => w.Type == "cadence")?.Graph_Data is object)
				{
					var stepsAndLaps = GetWorkoutStepsAndLaps(workoutSamples, startTime, sport, subSport);
					workoutSteps = stepsAndLaps.Values.Select(v => v.Item1).ToList();
					laps = stepsAndLaps.Values.Select(v => v.Item2).ToList();
				}
				else
				{
					laps = GetLaps(preferredLapType, workoutSamples, startTime, sport, subSport).ToList();
				}

				workoutMesg.SetNumValidSteps((ushort)workoutSteps.Count);

				// add steps in order
				foreach (var step in workoutSteps)
					messages.Add(step);

				// Add laps in order
				foreach (var lap in laps)
					messages.Add(lap);

				lapCount = laps.Count; 
			}
			
			messages.Add(workoutMesg);

			messages.Add(GetSessionMesg(workout, workoutSamples, userData, settings, sport, startTime, endTime, (ushort)lapCount));

			var activityMesg = new ActivityMesg();
			activityMesg.SetTimestamp(endTime);
			activityMesg.SetTotalTimerTime(workoutSamples.Duration);
			activityMesg.SetNumSessions(1);
			activityMesg.SetType(Activity.Manual);
			activityMesg.SetEvent(Event.Activity);
			activityMesg.SetEventType(EventType.Stop);

			var timezoneOffset = (int)TimeZoneInfo.Local.GetUtcOffset(base.GetEndTimeUtc(workout, workoutSamples)).TotalSeconds;
			var timeStamp = (uint)((int)endTime.GetTimeStamp() + timezoneOffset);
			activityMesg.SetLocalTimestamp(timeStamp);

			messages.Add(activityMesg);

			return new Tuple<string, ICollection<Mesg>>(title, messages);
		}

		private new Dynastream.Fit.DateTime GetStartTimeUtc(Workout workout)
		{
			var dtDateTime = base.GetStartTimeUtc(workout);
			return new Dynastream.Fit.DateTime(dtDateTime);
		}

		private new Dynastream.Fit.DateTime GetEndTimeUtc(Workout workout, WorkoutSamples workoutSamples)
		{
			var dtDateTime = base.GetEndTimeUtc(workout, workoutSamples);
			return new Dynastream.Fit.DateTime(dtDateTime);
		}

		private Dynastream.Fit.DateTime AddMetrics(ICollection<Mesg> messages, WorkoutSamples workoutSamples, Sport sport, Dynastream.Fit.DateTime startTime)
		{
			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(AddMetrics)}")
										.WithTag(TagKey.Format, FileFormat.Fit.ToString());

			var allMetrics = workoutSamples.Metrics;
			var hrMetrics = allMetrics.FirstOrDefault(m => m.Slug == "heart_rate");
			var outputMetrics = allMetrics.FirstOrDefault(m => m.Slug == "output");

			var cadenceMetrics = GetCadenceSummary(workoutSamples, sport);
			var speedMetrics = GetSpeedSummary(workoutSamples);

			var resistanceMetrics = allMetrics.FirstOrDefault(m => m.Slug == "resistance");
			var inclineMetrics = GetGradeSummary(workoutSamples);
			var locationMetrics = workoutSamples.Location_Data?.SelectMany(x => x.Coordinates).ToArray();
			var altitudeMetrics = allMetrics.FirstOrDefault(m => m.Slug == "altitude");

			var recordsTimeStamp = new Dynastream.Fit.DateTime(startTime);
			if (workoutSamples.Seconds_Since_Pedaling_Start is object)
			{
				for (var i = 0; i < workoutSamples.Seconds_Since_Pedaling_Start.Count; i++)
				{
					var record = new RecordMesg();
					record.SetTimestamp(recordsTimeStamp);

					if (speedMetrics is object && i < speedMetrics.Values.Length)
						record.SetSpeed(ConvertToMetersPerSecond(speedMetrics.GetValue(i), speedMetrics.Display_Unit));

					if (hrMetrics is object && i < hrMetrics.Values.Length)
						record.SetHeartRate((byte)hrMetrics.Values[i]);
					
					if (cadenceMetrics is object && i < cadenceMetrics.Values.Length)
						record.SetCadence((byte)cadenceMetrics.Values[i]);

					if (outputMetrics is object && i < outputMetrics.Values.Length)
						record.SetPower((ushort)outputMetrics.Values[i]);

					if (resistanceMetrics is object && i < resistanceMetrics.Values.Length)
					{
						var resistancePercent = resistanceMetrics.Values[i] / 100 ?? 0;
						record.SetResistance((byte)(254 * resistancePercent));
					}

					if (altitudeMetrics is object && i < altitudeMetrics.Values.Length)
					{
						var altitude = ConvertDistanceToMeters(altitudeMetrics.GetValue(i), altitudeMetrics.Display_Unit);
						record.SetAltitude(altitude);
					}

					if (inclineMetrics is object && i < inclineMetrics.Values.Length)
					{
						record.SetGrade((float)inclineMetrics.GetValue(i));
					}

					if (locationMetrics is object && i < locationMetrics.Length)
					{
						// unit is semicircles
						record.SetPositionLat(ConvertDegreesToSemicircles(locationMetrics[i].Latitude));
						record.SetPositionLong(ConvertDegreesToSemicircles(locationMetrics[i].Longitude));
					}

					messages.Add(record);
					recordsTimeStamp.Add(1);
				}
			}

			return recordsTimeStamp;
		}

		private int ConvertDegreesToSemicircles(float degrees)
		{
			return (int)(degrees * (Math.Pow(2, 31) / 180));
		}

		private SubSport GetGarminSubSport(Workout workout)
		{
			var fitnessDiscipline = workout.Fitness_Discipline;
			switch (fitnessDiscipline)
			{
				case FitnessDiscipline.Running when workout.Is_Outdoor:
				case FitnessDiscipline.Cycling when workout.Is_Outdoor:
				case FitnessDiscipline.Walking when workout.Is_Outdoor:
					return SubSport.Generic;

				case FitnessDiscipline.Cycling:
				case FitnessDiscipline.Bike_Bootcamp:
					return SubSport.IndoorCycling;

				case FitnessDiscipline.Running:
				case FitnessDiscipline.Walking:
					return SubSport.Treadmill;

				case FitnessDiscipline.Cardio:
				case FitnessDiscipline.Circuit:
					return SubSport.CardioTraining;

				case FitnessDiscipline.Strength:
					return SubSport.StrengthTraining;

				case FitnessDiscipline.Stretching:
					return SubSport.FlexibilityTraining;

				case FitnessDiscipline.Yoga:
					return SubSport.Yoga;

				case FitnessDiscipline.Meditation:
					return SubSport.Breathing;

				case FitnessDiscipline.Caesar:
					return SubSport.IndoorRowing;

				default:
					return SubSport.Generic;
			}
		}

		private SessionMesg GetSessionMesg(Workout workout, WorkoutSamples workoutSamples, UserData userData, Settings settings, Sport sport, Dynastream.Fit.DateTime startTime, Dynastream.Fit.DateTime endTime, ushort numLaps)
		{
			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(GetSessionMesg)}")
										.WithTag(TagKey.Format, FileFormat.Fit.ToString())
										.WithWorkoutId(workout.Id);

			var totalDistance = GetTotalDistance(workoutSamples);

			var sessionMesg = new SessionMesg();
			sessionMesg.SetTimestamp(endTime);
			sessionMesg.SetStartTime(startTime);
			var totalTime = workoutSamples.Duration;
			sessionMesg.SetTotalElapsedTime(totalTime);
			sessionMesg.SetTotalTimerTime(totalTime);
			sessionMesg.SetTotalDistance(totalDistance);
			sessionMesg.SetTotalWork((uint)workout.Total_Work);
			sessionMesg.SetTotalCalories((ushort?)GetCalorieSummary(workoutSamples)?.Value);

			var outputSummary = GetOutputSummary(workoutSamples);
			sessionMesg.SetAvgPower((ushort?)outputSummary?.Average_Value);
			sessionMesg.SetMaxPower((ushort?)outputSummary?.Max_Value);

			sessionMesg.SetFirstLapIndex(0);
			sessionMesg.SetNumLaps(numLaps);
			sessionMesg.SetThresholdPower(GetCyclingFtp(workout, userData));
			sessionMesg.SetEvent(Event.Lap);
			sessionMesg.SetEventType(EventType.Stop);
			sessionMesg.SetSport(GetGarminSport(workout));
			sessionMesg.SetSubSport(GetGarminSubSport(workout));

			var hrSummary = GetHeartRateSummary(workoutSamples);
			sessionMesg.SetAvgHeartRate((byte?)hrSummary?.Average_Value);
			sessionMesg.SetMaxHeartRate((byte?)hrSummary?.Max_Value);

			var cadenceSummary = GetCadenceSummary(workoutSamples, sport);
			sessionMesg.SetAvgCadence((byte?)cadenceSummary?.Average_Value);
			sessionMesg.SetMaxCadence((byte?)cadenceSummary?.Max_Value);

			sessionMesg.SetMaxSpeed(GetMaxSpeedMetersPerSecond(workoutSamples));
			sessionMesg.SetAvgSpeed(GetAvgSpeedMetersPerSecond(workoutSamples));
			sessionMesg.SetAvgGrade(GetAvgGrade(workoutSamples));
			sessionMesg.SetMaxPosGrade(GetMaxGrade(workoutSamples));
			sessionMesg.SetMaxNegGrade(0.0f);

			if (sport == Sport.Rowing)
			{
				var strokeCountSummary = workoutSamples.Summaries.FirstOrDefault(m => m.Slug == "stroke_count");
				if (strokeCountSummary is object && strokeCountSummary.Value > 0)
				{
					var totalStrokes = strokeCountSummary.Value;
					sessionMesg.SetAvgStrokeDistance((float)totalDistance / (float)totalStrokes);
					sessionMesg.SetTotalStrokes((uint)strokeCountSummary.Value);
				}
			}

			// HR zones
			if (settings.Format.IncludeTimeInHRZones && workoutSamples.Metrics.Any())
			{
				var hrz1 = GetHeartRateZone(1, workoutSamples);
				if (hrz1 is object)
					sessionMesg.SetTimeInHrZone(1, hrz1?.Duration);

				var hrz2 = GetHeartRateZone(2, workoutSamples);
				if (hrz2 is object)
					sessionMesg.SetTimeInHrZone(2, hrz2?.Duration);

				var hrz3 = GetHeartRateZone(3, workoutSamples);
				if (hrz3 is object)
					sessionMesg.SetTimeInHrZone(3, hrz3?.Duration);

				var hrz4 = GetHeartRateZone(4, workoutSamples);
				if (hrz4 is object)
					sessionMesg.SetTimeInHrZone(4, hrz4?.Duration);

				var hrz5 = GetHeartRateZone(5, workoutSamples);
				if (hrz5 is object)
					sessionMesg.SetTimeInHrZone(5, hrz5?.Duration);
			}

			// Power Zones
			if (settings.Format.IncludeTimeInPowerZones && workoutSamples.Metrics.Any() && workout.Fitness_Discipline == FitnessDiscipline.Cycling)
			{
				var zones = GetTimeInPowerZones(workout, workoutSamples);
				if (zones is object)
				{
					sessionMesg.SetTimeInPowerZone(1, zones.Zone1.Duration);
					sessionMesg.SetTimeInPowerZone(2, zones.Zone2.Duration);
					sessionMesg.SetTimeInPowerZone(3, zones.Zone3.Duration);
					sessionMesg.SetTimeInPowerZone(4, zones.Zone4.Duration);
					sessionMesg.SetTimeInPowerZone(5, zones.Zone5.Duration);
					sessionMesg.SetTimeInPowerZone(6, zones.Zone6.Duration);
					sessionMesg.SetTimeInPowerZone(7, zones.Zone7.Duration);
				}
			}

			return sessionMesg;
		}

		private Dictionary<int, Tuple<WorkoutStepMesg, LapMesg>> GetWorkoutStepsAndLaps(WorkoutSamples workoutSamples, Dynastream.Fit.DateTime startTime, Sport sport, SubSport subSport)
		{
			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(GetWorkoutStepsAndLaps)}")
										.WithTag(TagKey.Format, FileFormat.Fit.ToString());

			var stepsAndLaps = new Dictionary<int, Tuple<WorkoutStepMesg, LapMesg>>();

			if (workoutSamples is null)
				return stepsAndLaps;

			var cadenceTargets = GetCadenceTargets(workoutSamples);

			if (cadenceTargets is null)
				return stepsAndLaps;

			uint previousCadenceLower = 0;
			uint previousCadenceUpper = 0;
			ushort stepIndex = 0;
			var duration = 0;
			float lapDistanceInMeters = 0;
			WorkoutStepMesg workoutStep = null;
			LapMesg lapMesg = null;
			var speedMetrics = GetSpeedSummary(workoutSamples);

			foreach (var secondSinceStart in workoutSamples.Seconds_Since_Pedaling_Start)
			{
				var index = secondSinceStart <= 0 ? 0 : secondSinceStart - 1;
				duration++;

				if (speedMetrics is object && index < speedMetrics.Values.Length)
				{
					var currentSpeedInMPS = ConvertToMetersPerSecond(speedMetrics.GetValue(index), speedMetrics.Display_Unit);
					lapDistanceInMeters += 1 * currentSpeedInMPS;
				}

				var currentCadenceLower = index < cadenceTargets.Lower.Length ? (uint)cadenceTargets.Lower[index] : 0;
				var currentCadenceUpper = index < cadenceTargets.Upper.Length ? (uint)cadenceTargets.Upper[index] : 0;

				if (currentCadenceLower != previousCadenceLower
					|| currentCadenceUpper != previousCadenceUpper)
				{
					if (workoutStep != null && lapMesg != null)
					{
						workoutStep.SetDurationValue((uint)duration * 1000); // milliseconds

						var lapEndTime = new Dynastream.Fit.DateTime(startTime);
						lapEndTime.Add(secondSinceStart);
						lapMesg.SetTotalElapsedTime(duration);
						lapMesg.SetTotalTimerTime(duration);
						lapMesg.SetTimestamp(lapEndTime);
						lapMesg.SetEventType(EventType.Stop);
						lapMesg.SetTotalDistance(lapDistanceInMeters);

						stepsAndLaps.Add(stepIndex, new Tuple<WorkoutStepMesg, LapMesg>(workoutStep, lapMesg));
						stepIndex++;
						duration = 0;
						lapDistanceInMeters = 0;
					}

					workoutStep = new WorkoutStepMesg();
					workoutStep.SetDurationType(WktStepDuration.Time);
					workoutStep.SetMessageIndex(stepIndex);
					workoutStep.SetTargetType(WktStepTarget.Cadence);
					workoutStep.SetCustomTargetValueHigh(currentCadenceUpper);
					workoutStep.SetCustomTargetValueLow(currentCadenceLower);
					workoutStep.SetIntensity(currentCadenceUpper > 60 ? Intensity.Active : Intensity.Rest);

					lapMesg = new LapMesg();
					var lapStartTime = new Dynastream.Fit.DateTime(startTime);
					lapStartTime.Add(secondSinceStart);
					lapMesg.SetStartTime(lapStartTime);
					lapMesg.SetWktStepIndex(stepIndex);
					lapMesg.SetMessageIndex(stepIndex);
					lapMesg.SetEvent(Event.Lap);
					lapMesg.SetLapTrigger(LapTrigger.Time);
					lapMesg.SetSport(sport);
					lapMesg.SetSubSport(subSport);

					previousCadenceLower = currentCadenceLower;
					previousCadenceUpper = currentCadenceUpper;
				}
			}

			return stepsAndLaps;
		}

		private ICollection<SetMesg> GetStrengthWorkoutSteps(Workout workout, Dynastream.Fit.DateTime startTime, Settings settings)
		{
			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(GetStrengthWorkoutSteps)}")
										.WithTag(TagKey.Format, FileFormat.Fit.ToString());

			var steps = new List<SetMesg>();

			if (workout is null)
				return steps;

			var trackedMovements = workout.Movement_Tracker_Data;
			if (trackedMovements is null) return steps;

			var completedMovementSummary = trackedMovements.Completed_Movements_Summary_Data;
			if (completedMovementSummary is null) return steps;

			var repSummaryData = completedMovementSummary.Repetition_Summary_Data;
			if (repSummaryData is null) return steps;

			ushort stepIndex = 0;
			foreach (var repdata in repSummaryData)
			{
				if (!ExerciseMapping.StrengthExerciseMappings.TryGetValue(repdata.Movement_Id, out var exercise))
				{
					_logger.Error($"Found Peloton Strength exercise with no Garmin mapping: {repdata.Movement_Name} {repdata.Movement_Id}");
					continue;
				}

				if (exercise.ExerciseCategory == ExerciseCategory.Invalid) continue; // AMRAP -- no details provided for these segments

				var setMesg = new SetMesg();
				var setStartTime = new Dynastream.Fit.DateTime(startTime);
				setStartTime.Add(repdata.Offset);
				setMesg.SetStartTime(setStartTime);
				setMesg.SetDuration(repdata.Length);

				setMesg.SetCategory(0, exercise.ExerciseCategory);
				setMesg.SetCategorySubtype(0, exercise.ExerciseName);

				setMesg.SetMessageIndex(stepIndex);
				setMesg.SetSetType(SetType.Active);
				setMesg.SetWktStepIndex(stepIndex);
				
				var reps = repdata.Completed_Number;
				if (repdata.Is_Hold)
					reps = repdata.Completed_Number / settings.Format.Strength.DefaultSecondsPerRep;

				setMesg.SetRepetitions((ushort)reps);

				if (repdata.Weight is object && repdata.Weight.FirstOrDefault() is not null)
				{
					var weight = repdata.Weight.FirstOrDefault();
					setMesg.SetWeight(ConvertWeightToKilograms(weight.Weight_Data.Weight_Value, weight.Weight_Data.Weight_Unit));
				}

				stepIndex++;
				steps.Add(setMesg);
			}

			return steps;
		}

		public ICollection<LapMesg> GetLaps(PreferredLapType preferredLapType, WorkoutSamples workoutSamples, Dynastream.Fit.DateTime startTime, Sport sport, SubSport subSport)
		{
			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(GetLaps)}")
										.WithTag(TagKey.Format, FileFormat.Fit.ToString());

			if ((preferredLapType == PreferredLapType.Default || preferredLapType == PreferredLapType.Class_Segments)
				&& workoutSamples.Segment_List.Any())
				return GetLapsBasedOnSegments(workoutSamples, startTime, sport, subSport);

			return GetLapsBasedOnDistance(workoutSamples, startTime, sport, subSport);
		}

		public ICollection<LapMesg> GetLapsBasedOnSegments(WorkoutSamples workoutSamples, Dynastream.Fit.DateTime startTime, Sport sport, SubSport subSport)
		{
			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(GetLapsBasedOnSegments)}")
										.WithTag(TagKey.Format, FileFormat.Fit.ToString());

			var stepsAndLaps = new List<LapMesg>();

			if (workoutSamples is null)
				return stepsAndLaps;

			ushort stepIndex = 0;
			var speedMetrics = GetSpeedSummary(workoutSamples);
			var cadenceMetrics = GetCadenceSummary(workoutSamples, sport);
			if (workoutSamples.Segment_List.Any())
			{
				var totalElapsedTime = 0;
				foreach (var segment in workoutSamples.Segment_List)
				{
					var lapStartTime = new Dynastream.Fit.DateTime(startTime);
					lapStartTime.Add(segment.Start_Time_Offset);

					totalElapsedTime += segment.Length;

					var lapMesg = new LapMesg();
					lapMesg.SetStartTime(lapStartTime);
					lapMesg.SetMessageIndex(stepIndex);
					lapMesg.SetEvent(Event.Lap);
					lapMesg.SetLapTrigger(LapTrigger.SessionEnd);
					lapMesg.SetSport(sport);
					lapMesg.SetSubSport(subSport);

					lapMesg.SetTotalElapsedTime(segment.Length);
					lapMesg.SetTotalTimerTime(segment.Length);

					var startIndex = segment.Start_Time_Offset;
					var endIndex = segment.Start_Time_Offset + segment.Length;
					var lapDistanceInMeters = 0f;
					double cadenceSumOverSeconds = 0;
					double maxCadence = 0;
					for (int i = startIndex; i < endIndex; i++)
					{
						if (speedMetrics is object && i < speedMetrics.Values.Length)
						{
							var currentSpeedInMPS = ConvertToMetersPerSecond(speedMetrics.GetValue(i), speedMetrics.Display_Unit);
							lapDistanceInMeters += 1 * currentSpeedInMPS;
						}

						if (cadenceMetrics is object && i < cadenceMetrics.Values.Length)
						{
							var currentCadence = cadenceMetrics.GetValue(i);
							cadenceSumOverSeconds += currentCadence;
							maxCadence = currentCadence > maxCadence ? currentCadence : maxCadence;
						}
					}

					lapMesg.SetTotalDistance(lapDistanceInMeters);

					if (cadenceMetrics is object)
					{
						lapMesg.SetAvgCadence((byte)Math.Min(cadenceSumOverSeconds / segment.Length, 255));
						lapMesg.SetMaxCadence((byte)Math.Min(maxCadence, 255));
					}
					
					stepsAndLaps.Add(lapMesg);

					stepIndex++;
				}
			}

			return stepsAndLaps;
		}

		public ICollection<LapMesg> GetLapsBasedOnDistance(WorkoutSamples workoutSamples, Dynastream.Fit.DateTime startTime, Sport sport, SubSport subSport)
		{
			using var tracing = Tracing.Trace($"{nameof(FitConverter)}.{nameof(GetLapsBasedOnDistance)}")
										.WithTag(TagKey.Format, FileFormat.Fit.ToString());

			var stepsAndLaps = new List<LapMesg>();

			if (workoutSamples is null)
				return stepsAndLaps;
			
			var speedMetrics = GetSpeedSummary(workoutSamples);
			if (speedMetrics is null)
				return stepsAndLaps;

			var cadenceMetrics = GetCadenceSummary(workoutSamples, sport);

			var speedUnit = UnitHelpers.GetSpeedUnit(speedMetrics?.Display_Unit);
			var lapMeters = 0;
			switch (speedUnit)
			{
				case SpeedUnit.KilometersPerHour: lapMeters = 1000; break;
				case SpeedUnit.MinutesPer500Meters: lapMeters = 500; break;
				default: lapMeters = 1600; break;
			}

			LapMesg lap = null;
			ushort stepIndex = 0;
			var lapDistanceInMeters = 0f;
			float lapLengthSeconds = 0;
			double cadenceSumOverSeconds = 0;
			double maxCadence = 0;

			for (var secondsSinceStart = 0; secondsSinceStart < speedMetrics.Values.Length; secondsSinceStart++)
			{
				if (lap is null || lap.GetTotalElapsedTime() is not null)
				{
					// Start new Lap
					var lapStartTime = new Dynastream.Fit.DateTime(startTime);
					lapStartTime.Add(secondsSinceStart);

					lap = new LapMesg();
					lap.SetStartTime(lapStartTime);
					lap.SetMessageIndex(stepIndex);
					lap.SetEvent(Event.Lap);
					lap.SetLapTrigger(LapTrigger.Distance);
					lap.SetSport(sport);
					lap.SetSubSport(subSport);

					lapLengthSeconds = 0;
					lapDistanceInMeters = 0f;
					cadenceSumOverSeconds = 0;
					maxCadence = 0;
				}

				lapLengthSeconds++;

				var currentSpeedInMPS = ConvertToMetersPerSecond(speedMetrics.GetValue(secondsSinceStart), speedMetrics.Display_Unit);
				lapDistanceInMeters += 1* currentSpeedInMPS;

				if (cadenceMetrics is object && cadenceMetrics.Values.Length > secondsSinceStart)
				{
					var currentCadence = cadenceMetrics.GetValue(secondsSinceStart);
					cadenceSumOverSeconds += currentCadence;
					maxCadence = currentCadence > maxCadence ? currentCadence : maxCadence;
				}

				if (lapDistanceInMeters >= lapMeters || secondsSinceStart == speedMetrics.Values.Length - 1)
				{
					lap.SetTotalElapsedTime(lapLengthSeconds);
					lap.SetTotalTimerTime(lapLengthSeconds);
					lap.SetTotalDistance(lapDistanceInMeters);

					if (cadenceMetrics is object)
					{
						lap.SetAvgCadence((byte)Math.Min(cadenceSumOverSeconds / lapLengthSeconds, 255));
						lap.SetMaxCadence((byte)Math.Min(maxCadence, 255));
					}

					stepsAndLaps.Add(lap);
					stepIndex++;
				}
			}

			return stepsAndLaps;
		}

		protected DeviceInfoMesg GetDeviceInfoMesg(GarminDeviceInfo deviceInfo, Dynastream.Fit.DateTime startTime)
		{
			var deviceInfoMesg = new DeviceInfoMesg();
			deviceInfoMesg.SetTimestamp(startTime);
			deviceInfoMesg.SetSerialNumber(deviceInfo.UnitId);
			deviceInfoMesg.SetManufacturer(deviceInfo.ManufacturerId);
			deviceInfoMesg.SetProduct(deviceInfo.ProductID);
			deviceInfoMesg.SetDeviceIndex(0);
			deviceInfoMesg.SetSourceType(SourceType.Local);
			deviceInfoMesg.SetProductName(deviceInfo.Name);

			if(deviceInfo.Version.VersionMinor <=0)
				deviceInfoMesg.SetSoftwareVersion(deviceInfo.Version.VersionMajor);
			else
			{
				var adjustedMinor = deviceInfo.Version.VersionMinor < 10 ? deviceInfo.Version.VersionMinor * 10 : deviceInfo.Version.VersionMinor;
				var minor = adjustedMinor / 100;
				deviceInfoMesg.SetSoftwareVersion((float)(deviceInfo.Version.VersionMajor + minor));
			}

			return deviceInfoMesg;
		}
	}
}
