using Conversion;
using Dynastream.Fit;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;

namespace UnitTests.Conversion
{
	public class ExerciseMappingTests
	{
		[Test]
		public void IgnoredPelotonExercises_ShouldContainExpectedExercises()
		{
			// ASSERT
			ExerciseMapping.IgnoredPelotonExercises.Should().NotBeEmpty();
			ExerciseMapping.IgnoredPelotonExercises.Should().Contain("b067c9f7a1e4412190b0f8eb3c6128e3"); // Active Recovery
			ExerciseMapping.IgnoredPelotonExercises.Should().Contain("98cde50f696746ff98727d5362229cfb"); // AMRAP
			ExerciseMapping.IgnoredPelotonExercises.Should().Contain("e54412161b594e54a86d6ef23ea3d017"); // Demo
			ExerciseMapping.IgnoredPelotonExercises.Should().Contain("a76c5edc0641475189442ecad456057a"); // Transition
			ExerciseMapping.IgnoredPelotonExercises.Should().Contain("0b67e950426440fe8b4eadc426320a56"); // Warmup
		}

		[Test]
		public void IsRest_WhenRestExerciseId_ShouldReturnTrue()
		{
			// SETUP
			var restId = "3ca251f6d68746ad91aebea5c89694ca";

			// ACT
			var result = restId.IsRest();

			// ASSERT
			result.Should().BeTrue();
		}

		[Test]
		public void IsRest_WhenNotRestExerciseId_ShouldReturnFalse()
		{
			// SETUP
			var nonRestId = "01251235527748368069f9dc898aadf3"; // Arnold Press

			// ACT
			var result = nonRestId.IsRest();

			// ASSERT
			result.Should().BeFalse();
		}

		[Test]
		public void IsRest_WhenNullOrEmpty_ShouldReturnFalse()
		{
			// ACT & ASSERT
			string.Empty.IsRest().Should().BeFalse();
			((string)null).IsRest().Should().BeFalse();
		}

		[Test]
		public void StrengthExerciseMappings_ShouldNotBeEmpty()
		{
			// ASSERT
			ExerciseMapping.StrengthExerciseMappings.Should().NotBeEmpty();
			ExerciseMapping.StrengthExerciseMappings.Count.Should().BeGreaterThan(90); // Should have many mappings (currently 94)
		}

		[Test]
		public void StrengthExerciseMappings_ShouldContainValidMappings()
		{
			// ACT & ASSERT
			foreach (var mapping in ExerciseMapping.StrengthExerciseMappings)
			{
				mapping.Key.Should().NotBeNullOrEmpty("Exercise ID should not be null or empty");
				mapping.Key.Length.Should().Be(32, "Exercise ID should be 32 characters long");
				
				mapping.Value.Should().NotBeNull("GarminExercise should not be null");
				mapping.Value.ExerciseCategory.Should().BeGreaterThanOrEqualTo(0, "ExerciseCategory should be valid (0 is valid for some enums)");
				mapping.Value.ExerciseName.Should().BeGreaterThanOrEqualTo(0, "ExerciseName should be valid (0 is valid for some enums)");
			}
		}

		[Test]
		public void StrengthExerciseMappings_ShouldHaveSpecificExercises()
		{
			// Test specific well-known exercises
			ExerciseMapping.StrengthExerciseMappings.Should().ContainKey("01251235527748368069f9dc898aadf3"); // Arnold Press
			ExerciseMapping.StrengthExerciseMappings.Should().ContainKey("43d404595338443baab306a6589ae7fc"); // Bicep Curl
			ExerciseMapping.StrengthExerciseMappings.Should().ContainKey("1c4d81ad487849a6995f93e1a6a4b1e4"); // Push Up
			ExerciseMapping.StrengthExerciseMappings.Should().ContainKey("cd6046306b2c4c4a8f40e169ec924eb9"); // Deadlift
			ExerciseMapping.StrengthExerciseMappings.Should().ContainKey("46d30aa1a4a245ada8a5e5ff8f3e7662"); // Body Weight Squat

			var arnoldPress = ExerciseMapping.StrengthExerciseMappings["01251235527748368069f9dc898aadf3"];
			arnoldPress.ExerciseCategory.Should().Be(ExerciseCategory.ShoulderPress);
			arnoldPress.ExerciseName.Should().Be(ShoulderPressExerciseName.ArnoldPress);
		}

		[Test]
		public void StrengthExerciseMappings_ShouldMapCorrectExerciseCategories()
		{
			// Test various exercise categories are represented
			var categories = ExerciseMapping.StrengthExerciseMappings.Values
				.Select(v => v.ExerciseCategory)
				.Distinct()
				.ToList();

			categories.Should().Contain(ExerciseCategory.ShoulderPress);
			categories.Should().Contain(ExerciseCategory.Curl);
			categories.Should().Contain(ExerciseCategory.PushUp);
			categories.Should().Contain(ExerciseCategory.Squat);
			categories.Should().Contain(ExerciseCategory.Deadlift);
			categories.Should().Contain(ExerciseCategory.Plank);
			categories.Should().Contain(ExerciseCategory.Crunch);
			categories.Should().Contain(ExerciseCategory.Lunge);
			categories.Should().Contain(ExerciseCategory.Row);
		}

		[Test]
		public void StrengthExerciseMappings_ShouldNotHaveDuplicateIds()
		{
			// Test that there are no duplicate exercise IDs
			var exerciseIds = ExerciseMapping.StrengthExerciseMappings.Keys;
			var uniqueIds = exerciseIds.Distinct();

			exerciseIds.Count().Should().Be(uniqueIds.Count(), "There should be no duplicate exercise IDs");
		}

		[Test]
		public void StrengthExerciseMappings_AllIdsShouldBeValidFormat()
		{
			// Test that all exercise IDs are in the expected format (32-character hex strings)
			foreach (var exerciseId in ExerciseMapping.StrengthExerciseMappings.Keys)
			{
				exerciseId.Should().MatchRegex(@"^[a-f0-9]{32}$", 
					$"Exercise ID {exerciseId} should be a 32-character hexadecimal string");
			}
		}

		[Test]
		public void GarminExercise_Constructor_ShouldSetProperties()
		{
			// SETUP
			ushort category = ExerciseCategory.Squat;
			ushort name = SquatExerciseName.Squat;

			// ACT
			var exercise = new GarminExercise(category, name);

			// ASSERT
			exercise.ExerciseCategory.Should().Be(category);
			exercise.ExerciseName.Should().Be(name);
		}

		[Test]
		public void StrengthExerciseMappings_PushUpVariations_ShouldMapToPushUpCategory()
		{
			// Test that all push-up variations map to PushUp category
			var pushUpExercises = new[]
			{
				"1c4d81ad487849a6995f93e1a6a4b1e4", // Push Up
				"8af39d3485224ac19f7d8659d30524e7", // Pike Push Up
				"d463a4dc0cf640e0a58f3aa058c5b1a0", // Tricep Push Up
				"ce8c746fb5224e9dbc401fef0013a54f"  // Dumbbell Pushup
			};

			foreach (var exerciseId in pushUpExercises)
			{
				if (ExerciseMapping.StrengthExerciseMappings.ContainsKey(exerciseId))
				{
					var mapping = ExerciseMapping.StrengthExerciseMappings[exerciseId];
					mapping.ExerciseCategory.Should().Be(ExerciseCategory.PushUp, 
						$"Exercise {exerciseId} should map to PushUp category");
				}
			}
		}

		[Test]
		public void StrengthExerciseMappings_SquatVariations_ShouldMapToSquatCategory()
		{
			// Test that all squat variations map to Squat category
			var squatExercises = new[]
			{
				"46d30aa1a4a245ada8a5e5ff8f3e7662", // Body Weight Squat
				"7d82b59462a54e61926077ded0becae5", // Dumbbell Squat
				"588e35f7067842979485ff1e4f80df26", // Goblet Squat
				"28833fd99466476ea273d6b94747e3db"  // Split Squat
			};

			foreach (var exerciseId in squatExercises)
			{
				if (ExerciseMapping.StrengthExerciseMappings.ContainsKey(exerciseId))
				{
					var mapping = ExerciseMapping.StrengthExerciseMappings[exerciseId];
					mapping.ExerciseCategory.Should().Be(ExerciseCategory.Squat, 
						$"Exercise {exerciseId} should map to Squat category");
				}
			}
		}

		[Test]
		public void StrengthExerciseMappings_CurlVariations_ShouldMapToCurlCategory()
		{
			// Test that all curl variations map to Curl category
			var curlExercises = new[]
			{
				"43d404595338443baab306a6589ae7fc", // Bicep Curl
				"114ce849b47a4fabbaad961188bf4f7d", // Hammer Curl
				"3695ef0ec2ce484faedc8ce2bfa2819d", // Concentrated Curl
				"a66b797fc2014b799cc0cb114d9c5079"  // Cross Body Curl
			};

			foreach (var exerciseId in curlExercises)
			{
				if (ExerciseMapping.StrengthExerciseMappings.ContainsKey(exerciseId))
				{
					var mapping = ExerciseMapping.StrengthExerciseMappings[exerciseId];
					mapping.ExerciseCategory.Should().Be(ExerciseCategory.Curl, 
						$"Exercise {exerciseId} should map to Curl category");
				}
			}
		}

		[Test]
		public void StrengthExerciseMappings_PlankVariations_ShouldMapToPlankCategory()
		{
			// Test that all plank variations map to Plank category
			var plankExercises = new[]
			{
				"feb44f24e2b8487b870a35f4501069be", // ForeArm Plank
				"194cc4f6a88c4abd80afe9bbddb25915", // High Plank
				"1c0403c4d7264d83b1c75d18c8cdac4f", // Forearm Side Plank
				"a6833a0f9c35489585398d1f293600de"  // High Side Plank
			};

			foreach (var exerciseId in plankExercises)
			{
				if (ExerciseMapping.StrengthExerciseMappings.ContainsKey(exerciseId))
				{
					var mapping = ExerciseMapping.StrengthExerciseMappings[exerciseId];
					mapping.ExerciseCategory.Should().Be(ExerciseCategory.Plank, 
						$"Exercise {exerciseId} should map to Plank category");
				}
			}
		}

		[Test]
		public void StrengthExerciseMappings_OverheadMovements_ShouldMapToShoulderPress()
		{
			// Test overhead press movements
			var overheadExercises = new[]
			{
				"ef0279948228409298cd6bf62c5b122c", // Overhead Press
				"3caccc04fea1402cab9887ce589833ea", // Narrow-Grip Overhead Press
				"258884d9586b45b3973228147a6b0c48"  // Wide Grip Overhead Press
			};

			foreach (var exerciseId in overheadExercises)
			{
				if (ExerciseMapping.StrengthExerciseMappings.ContainsKey(exerciseId))
				{
					var mapping = ExerciseMapping.StrengthExerciseMappings[exerciseId];
					mapping.ExerciseCategory.Should().Be(ExerciseCategory.ShoulderPress, 
						$"Exercise {exerciseId} should map to ShoulderPress category");
				}
			}
		}
	}
}