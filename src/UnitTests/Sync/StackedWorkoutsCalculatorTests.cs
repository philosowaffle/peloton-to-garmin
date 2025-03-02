using NUnit.Framework;
using Sync;
using System.Collections.Generic;
using Common.Dto;
using FluentAssertions;
using Bogus;
using System.Linq;
using Common.Dto.Peloton;
using FluentAssertions.Common;
using System;

namespace UnitTests.Sync;

public class StackedWorkoutsCalculatorTests
{
	private static readonly string RuleSet_BareMinimum = "bareMinimum";
	private static readonly string RuleSet_AllFitnessDisciplines = "allFitnessDisciplines";
	private static readonly string RuleSet_Workouts_Within_5min = "workoutsWithin5min";

	private readonly Faker<Workout> _workoutFaker = new Faker<Workout>().UseSeed(100).UseDateTimeReference(DateTime.Parse("1/1/1980"));
	private readonly Faker<P2GWorkout> _p2gWorkoutFaker = new Faker<P2GWorkout>().UseSeed(300).UseDateTimeReference(DateTime.Parse("1/1/1980"));
	
	[OneTimeSetUp]
	public void SetUp()
	{
		var startNext = DateTime.Now
										.ToUniversalTime()
										.ToDateTimeOffset()
										.ToUnixTimeSeconds();
		Action<IRuleSet<Workout>> workoutsWithin5min = (f) =>
		{
			f.RuleFor(w => w.Start_Time, f => startNext);
			f.RuleFor(w => w.End_Time, (f, w) => w.Start_Time + f.Random.Long(300, 3600));
			f.FinishWith((f, w) => startNext = w.End_Time.Value + 300);
		};
		_workoutFaker.RuleSet(RuleSet_Workouts_Within_5min, workoutsWithin5min);

		Action<IRuleSet<Workout>> workoutAllDisciplines = (f) =>
		{
			var distinctDisciplines = Enum.GetValues<FitnessDiscipline>();
			f.RuleFor(w => w.Fitness_Discipline, // Ensure one of each type
						(f) => distinctDisciplines[f.UniqueIndex % distinctDisciplines.Length]);
		};
		_workoutFaker.RuleSet(RuleSet_AllFitnessDisciplines, workoutAllDisciplines);

		Action<IRuleSet<P2GWorkout>> p2gWorkoutBareMinimum = (f) =>
		{
			f.RuleFor(w => w.Workout, f => _workoutFaker.Generate(ruleSets: RuleSet_AllFitnessDisciplines));
		};
		_p2gWorkoutFaker.RuleSet(RuleSet_BareMinimum, p2gWorkoutBareMinimum);
	}

	[Test]
	public void GetStackedWorkouts_When_Workouts_Is_NullOrEmpty_DoesNothing()
	{
		// NULL
		// SETUP
		IEnumerable<P2GWorkout> workouts = null;

		// ACT
		var result = StackedWorkoutsCalculator.GetStackedWorkouts(workouts, 1);

		// ASSERT
		result.Should().BeEmpty();

		// EMPTY
		// SETUP
		workouts = new List<P2GWorkout>();

		// ACT
		result = StackedWorkoutsCalculator.GetStackedWorkouts(workouts, 1);

		// ASSERT
		result.Should().BeEmpty();
	}

	[Test]
	public void GetStackedWorkouts_When_OneWorkout_Returns_One()
	{
		// SETUP
		IEnumerable<P2GWorkout> workouts = _p2gWorkoutFaker.Generate(1, ruleSets: RuleSet_BareMinimum);

		// ACT
		var result = StackedWorkoutsCalculator.GetStackedWorkouts(workouts, 1);

		// ASSERT
		result.Should().NotBeNullOrEmpty();
		
		result.Keys.Count.Should().Be(1);
		result.Keys.First().Should().Be(0);

		result[0].Should().NotBeNullOrEmpty();
		result[0].Count.Should().Be(1);
		result[0].First().Should().Be(workouts.First());
	}

	[Test]
	public void GetStackedWorkouts_When_BareMinimumData_DoesNotThrow()
	{
		// SETUP
		IEnumerable<P2GWorkout> workouts = _p2gWorkoutFaker.Generate(3, ruleSets: RuleSet_BareMinimum);

		// ACT
		var result = StackedWorkoutsCalculator.GetStackedWorkouts(workouts, 0);

		// ASSERT
		result.Should().NotBeNullOrEmpty();
	}

	[Test]
	public void GetStackedWorkouts_Should_Only_Group_By_Common_Type()
	{
		// SETUP
		Action<IRuleSet<P2GWorkout>> p2gWorkoutAllDisciplines = (f) =>
		{
			f.RuleFor(w => w.Workout, f => _workoutFaker.Generate(ruleSets: $"{RuleSet_AllFitnessDisciplines},{RuleSet_Workouts_Within_5min}"));
		};
		_p2gWorkoutFaker.RuleSet(nameof(GetStackedWorkouts_Should_Only_Group_By_Common_Type), p2gWorkoutAllDisciplines);
		IEnumerable<P2GWorkout> workouts = _p2gWorkoutFaker.Generate(Enum.GetValues<FitnessDiscipline>().Length * 2, ruleSets: nameof(GetStackedWorkouts_Should_Only_Group_By_Common_Type));

		// ACT
		var result = StackedWorkoutsCalculator.GetStackedWorkouts(workouts, long.MaxValue);

		// ASSERT
		result.Should().NotBeNullOrEmpty();

		result.Keys.Count.Should().Be((workouts.Count() / 2) - 1); // Caesar Bootcamp maps to None
		result.Values.SelectMany(i => i).Should().HaveSameCount(workouts);
	}

	[TestCase(0)]
	[TestCase(100)]
	[TestCase(300)]
	[TestCase(301)]
	public void GetStackedWorkouts_Should_Honor_TimeGap(long secondsGap)
	{
		Action<IRuleSet<Workout>> disciplines = (f) =>
		{
			var count = 0;
			var current = FitnessDiscipline.Cycling;
			f.RuleFor(w => w.Fitness_Discipline, f => current)
			.FinishWith((f,w) => 
			{
				if (count == 5)
				{
					current = current == FitnessDiscipline.Strength ? FitnessDiscipline.Cycling : FitnessDiscipline.Strength;
					count = 0;
					return;
				}

				count++;
			});
		};
		_workoutFaker.RuleSet("RuleSet_Disciplines", disciplines);
		Action<IRuleSet<P2GWorkout>> p2gWorkoutAllDisciplines = (f) =>
		{
			f.RuleFor(w => w.Workout, f => _workoutFaker.Generate(ruleSets: $"RuleSet_Disciplines,{RuleSet_Workouts_Within_5min}"));
		};
		_p2gWorkoutFaker.RuleSet($"MyTest:{secondsGap}", p2gWorkoutAllDisciplines);

		var workouts = _p2gWorkoutFaker
						.UseSeed((int)secondsGap)
						.Generate(10, ruleSets: $"MyTest:{secondsGap}");

		// ACT
		var result = StackedWorkoutsCalculator.GetStackedWorkouts(workouts, secondsGap);

		// ASSERT
		result.Should().NotBeNullOrEmpty();

		if (secondsGap >= 300)
		{
			result.Keys.Count.Should().Be(2, 
				because: "{0} Workouts, grouped by type should be five per type.", 
				becauseArgs: workouts.Count);
		} else
		{
			result.Keys.Count.Should().Be(workouts.Count,
				because: "{0} Workouts, none falling within the 5min time gap range should yield distinct groups.",
				becauseArgs: workouts.Count);
		}
	}

	[Test]
	public void CombineLocationData_When_WorkoutsToStack_Is_NullOrEmpty_NoOp()
	{
		// NULL
		// SETUP
		ICollection<P2GWorkout> workouts = null;

		// ACT
		var result = StackedWorkoutsCalculator.CombineLocationData(workouts);

		// ASSERT
		result.Should().BeEmpty();

		// EMPTY
		// SETUP
		workouts = new List<P2GWorkout>();

		// ACT
		result = StackedWorkoutsCalculator.CombineLocationData(workouts);

		// ASSERT
		result.Should().BeEmpty();
	}

	[Test]
	public void CombineLocationData_When_WorkoutSamples_Is_Null_SkipsEmpty()
	{
		// NULL
		// SETUP
		_p2gWorkoutFaker.RuleSet($"{nameof(CombineLocationData_When_WorkoutSamples_Is_Null_SkipsEmpty)}", (f) => 
		{
			f.RuleFor(f => f.WorkoutSamples, f => null);
		});

		var workoutsToStack = _p2gWorkoutFaker.Generate(10, $"{nameof(CombineLocationData_When_WorkoutSamples_Is_Null_SkipsEmpty)}");

		// ACT
		var result = StackedWorkoutsCalculator.CombineLocationData(workoutsToStack);

		// ASSERT
		result.Should().BeEmpty();
	}

	[Test]
	public void CombineLocationData_When_LocatioNData_Is_NullOrEmpty_SkipsEmpty()
	{
		// NULL
		// SETUP
		_p2gWorkoutFaker.RuleSet($"{nameof(CombineLocationData_When_LocatioNData_Is_NullOrEmpty_SkipsEmpty)}_Null", (f) =>
		{
			f.RuleFor(f => f.WorkoutSamples, f => new WorkoutSamples() { Location_Data = null });
		});

		var workoutsToStack = _p2gWorkoutFaker.Generate(10, $"{nameof(CombineLocationData_When_LocatioNData_Is_NullOrEmpty_SkipsEmpty)}_Null");

		// ACT
		var result = StackedWorkoutsCalculator.CombineLocationData(workoutsToStack);

		// ASSERT
		result.Should().BeEmpty();

		// EMPTY
		// SETUP
		_p2gWorkoutFaker.RuleSet($"{nameof(CombineLocationData_When_LocatioNData_Is_NullOrEmpty_SkipsEmpty)}_Empty", (f) =>
		{
			f.RuleFor(f => f.WorkoutSamples, f => new WorkoutSamples() { Location_Data = new List<LocationData>() });
		});

		workoutsToStack = _p2gWorkoutFaker.Generate(10, $"{nameof(CombineLocationData_When_LocatioNData_Is_NullOrEmpty_SkipsEmpty)}_Empty");

		// ACT
		result = StackedWorkoutsCalculator.CombineLocationData(workoutsToStack);

		// ASSERT
		result.Should().BeEmpty();
	}

	[Test]
	public void CombineLocationData_When_Coordinates_Is_NullOrEmpty_SkipsEmpty()
	{
		// NULL
		// SETUP
		_p2gWorkoutFaker.RuleSet($"{nameof(CombineLocationData_When_Coordinates_Is_NullOrEmpty_SkipsEmpty)}_Null", (f) =>
		{
			f.RuleFor(f => f.WorkoutSamples, f => new WorkoutSamples() 
			{
				Location_Data = new List<LocationData>()
				{
					new () { Coordinates = null }
				}
			});
		});

		var workoutsToStack = _p2gWorkoutFaker.Generate(10, $"{nameof(CombineLocationData_When_Coordinates_Is_NullOrEmpty_SkipsEmpty)}_Null");

		// ACT
		var result = StackedWorkoutsCalculator.CombineLocationData(workoutsToStack);

		// ASSERT
		result.Should().BeEmpty();

		// EMPTY
		// SETUP
		_p2gWorkoutFaker.RuleSet($"{nameof(CombineLocationData_When_Coordinates_Is_NullOrEmpty_SkipsEmpty)}_Empty", (f) =>
		{
			f.RuleFor(f => f.WorkoutSamples, f => new WorkoutSamples()
			{
				Location_Data = new List<LocationData>()
				{
					new () { Coordinates = new List<Coordinate>() }
				}
			});
		});

		workoutsToStack = _p2gWorkoutFaker.Generate(10, $"{nameof(CombineLocationData_When_Coordinates_Is_NullOrEmpty_SkipsEmpty)}_Emptu");

		// ACT
		result = StackedWorkoutsCalculator.CombineLocationData(workoutsToStack);

		// ASSERT
		result.Should().BeEmpty();
	}

	[Test]
	public void CombineLocationData_When_Coordinate_Is_Null_SkipsEmpty()
	{
		// NULL
		// SETUP
		_p2gWorkoutFaker.RuleSet($"{nameof(CombineLocationData_When_Coordinates_Is_NullOrEmpty_SkipsEmpty)}_Null", (f) =>
		{
			f.RuleFor(f => f.WorkoutSamples, f => new WorkoutSamples()
			{
				Location_Data = new List<LocationData>()
				{
					new () { Coordinates = new List<Coordinate>() { null, null } }
				}
			});
		});

		var workoutsToStack = _p2gWorkoutFaker.Generate(10, $"{nameof(CombineLocationData_When_Coordinates_Is_NullOrEmpty_SkipsEmpty)}_Null");

		// ACT
		var result = StackedWorkoutsCalculator.CombineLocationData(workoutsToStack);

		// ASSERT
		result.Should().NotBeEmpty(because: "Logic currently allows nulls to be written.");
		result.Count().Should().Be(10, because: "10 workouts yields 10 LocationData objects, with list of coordinates within each.");
		result.Select(l => l.Coordinates.Count).Sum().Should().Be(20, because: "Each LocationData object should have a list of Coordinates with two items.");
	}

	[Test]
	public void CombineLocationData_Calculates_Correctly()
	{
		var initialLatitude = 1;
		var initialLongitude = 100;
		var initialSeconds = 0;

		// SETUP

		var coordinateFaker = new Faker<Coordinate>().UseSeed(200).UseDateTimeReference(DateTime.Parse("1/1/1980"));
		coordinateFaker.Rules((f, c) =>
		{
			c.Latitude = initialLatitude;
			c.Longitude = initialLongitude;
			c.Seconds_Offset_From_Start = initialSeconds;
		})
		.FinishWith((f,c) => 
		{
			initialLatitude++;
			initialLongitude++;
			initialSeconds++;
		});

		_p2gWorkoutFaker.RuleSet($"{nameof(CombineLocationData_Calculates_Correctly)}", (f) =>
		{
			f.RuleFor(f => f.WorkoutSamples, f => new WorkoutSamples()
			{
				Location_Data = new List<LocationData>()
				{
					new () { Coordinates = coordinateFaker.Generate(10)} // 10 1s coordinates
				},
				Duration = 10 // 10 total seconds
			})
			.FinishWith((f,w) => 
			{
				initialSeconds = 0;
			});
		});

		var workoutsToStack = _p2gWorkoutFaker.Generate(10, $"{nameof(CombineLocationData_Calculates_Correctly)}");

		// ACT
		var result = StackedWorkoutsCalculator.CombineLocationData(workoutsToStack);

		// ASSERT
		result.Should().NotBeEmpty();
		result.Count().Should().Be(10, because: "10 workouts yields 10 LocationData objects, with list of coordinates within each.");
		result.Select(l => l.Coordinates.Count).Sum().Should().Be(100, because: "Each LocationData object should have a list of Coordinates with 10 items.");

		var expectedLatitude = 1;
		var expectedLongitude = 100;
		var expectedSeconds = 0;

		foreach (var data in result)
		{
			foreach (var coordinate in data.Coordinates)
			{
				coordinate.Latitude.Should().Be(expectedLatitude);
				coordinate.Longitude.Should().Be(expectedLongitude);
				coordinate.Seconds_Offset_From_Start.Should().Be(expectedSeconds);

				expectedLatitude++;
				expectedLongitude++;
				expectedSeconds++;
			}
		}
	}

	[Test]
	public void CombineMetricsData_When_WorkoutsToStack_Is_NullOrEmpty_NoOp()
	{
		// NULL
		// SETUP
		ICollection<P2GWorkout> workouts = null;

		// ACT
		var result = StackedWorkoutsCalculator.CombineMetricsData(workouts);

		// ASSERT
		result.Should().BeEmpty();

		// EMPTY
		// SETUP
		workouts = new List<P2GWorkout>();

		// ACT
		result = StackedWorkoutsCalculator.CombineMetricsData(workouts);

		// ASSERT
		result.Should().BeEmpty();
	}

	[Test]
	public void CombineMetricsData_When_WorkoutSamples_Is_Null_SkipsEmpty()
	{
		// NULL
		// SETUP
		_p2gWorkoutFaker.RuleSet($"{nameof(CombineMetricsData_When_WorkoutSamples_Is_Null_SkipsEmpty)}", (f) =>
		{
			f.RuleFor(f => f.WorkoutSamples, f => null);
		});

		var workoutsToStack = _p2gWorkoutFaker.Generate(10, $"{nameof(CombineMetricsData_When_WorkoutSamples_Is_Null_SkipsEmpty)}");

		// ACT
		var result = StackedWorkoutsCalculator.CombineMetricsData(workoutsToStack);

		// ASSERT
		result.Should().BeEmpty();
	}

	[Test]
	public void CombineMetricData_When_Metrics_Is_Null_SkipsEmpty()
	{
		// NULL
		// SETUP
		_p2gWorkoutFaker.RuleSet($"{nameof(CombineMetricData_When_Metrics_Is_Null_SkipsEmpty)}_Null", (f) =>
		{
			f.RuleFor(f => f.WorkoutSamples, f => new WorkoutSamples() { Metrics = null });
		});

		var workoutsToStack = _p2gWorkoutFaker.Generate(10, $"{nameof(CombineMetricData_When_Metrics_Is_Null_SkipsEmpty)}_Null");

		// ACT
		var result = StackedWorkoutsCalculator.CombineMetricsData(workoutsToStack);

		// ASSERT
		result.Should().BeEmpty();

		// Empty
		// SETUP
		_p2gWorkoutFaker.RuleSet($"{nameof(CombineMetricData_When_Metrics_Is_Null_SkipsEmpty)}_Empty", (f) =>
		{
			f.RuleFor(f => f.WorkoutSamples, f => new WorkoutSamples() { Metrics = new List<Metric>() });
		});

		workoutsToStack = _p2gWorkoutFaker.Generate(10, $"{nameof(CombineMetricData_When_Metrics_Is_Null_SkipsEmpty)}_Empty");

		// ACT
		result = StackedWorkoutsCalculator.CombineMetricsData(workoutsToStack);

		// ASSERT
		result.Should().BeEmpty();
	}
}
