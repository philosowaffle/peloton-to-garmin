using Dynastream.Fit;
using System.Collections.Generic;

namespace Conversion;

public static class ExerciseMapping
{
	public static readonly Dictionary<string, GarminExercise> StrengthExerciseMappings = new()
	{
		{ "43d404595338443baab306a6589ae7fc", new (ExerciseCategory.Curl, CurlExerciseName.StandingDumbbellBicepsCurl) }, // Peloton: Bicep Curl

		{ "1c0403c4d7264d83b1c75d18c8cdac4f", new (ExerciseCategory.Plank, PlankExerciseName.SidePlank) }, // Peloton: Forearm Side Plank
		{ "ed18d837c14746c5af38d4fa03b56918", new (ExerciseCategory.Lunge, LungeExerciseName.DumbbellLunge) }, // Peloton: Front to Back Lunge

		{ "588e35f7067842979485ff1e4f80df26", new (ExerciseCategory.Squat,SquatExerciseName.GobletSquat) }, // Peloton: Goblet Squat

		{ "114ce849b47a4fabbaad961188bf4f7d", null }, // Peloton: Hammer Curl
		{ "060174b84e3744e6a19fe4ce80411113", null }, // Peloton: Hollow Hold

		{ "12057d5f9e144913a824bcae5706966c", null }, // Peloton: Overhead Carry
		{ "ef0279948228409298cd6bf62c5b122c", null }, // Peloton: Overhead Press

		{ "5b33283433e7479390c0d5fc11722f80", null }, // Peloton: Shoulder Tap
		{ "28833fd99466476ea273d6b94747e3db", null }, // Peloton: Split Squat

		{ "da89d743904640d58e8b3f667f08783c", null }, // Peloton: Tricep Kickback
		{ "3069e7ba28b84005b71c16a3781dda8d", null }, // Peloton: Tuck up
	};
}

public record GarminExercise
{
	public ushort ExerciseCategory { get; init; }
	public ushort ExerciseName { get; init; }

	public GarminExercise(ushort exerciseCategory, ushort exerciseName)
	{
		ExerciseCategory = exerciseCategory;
		ExerciseName = exerciseName;
	}
}
