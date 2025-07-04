using Conversion;
using Dynastream.Fit;
using FluentAssertions;
using NUnit.Framework;

namespace UnitTests.Conversion
{
	public class ExerciseMappingTests
	{
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
	}
}