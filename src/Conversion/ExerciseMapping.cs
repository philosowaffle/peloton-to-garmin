using Dynastream.Fit;
using System.Collections.Generic;

namespace Conversion;

public static class ExerciseMapping
{
	public static readonly Dictionary<string, GarminExercise> StrengthExerciseMappings = new()
	{
		// A
		/* AMRAP */ { "98cde50f696746ff98727d5362229cfb", new (ExerciseCategory.Invalid, 0) },

		// B
		/* Bent Over Row */ { "d60a1dd8824a49a4926f826b24f3b061", new (ExerciseCategory.Row, RowExerciseName.OneArmBentOverRow) },
		/* Bicep Curl */ { "43d404595338443baab306a6589ae7fc", new (ExerciseCategory.Curl, CurlExerciseName.StandingDumbbellBicepsCurl) },
		/* Bird Dog */ { "df8e18c5082f408b8490c4adcb0678b5", new (ExerciseCategory.Plank, PlankExerciseName.PlankWithKneeToElbow) }, 

		// C
		/* Chest Fly */ { "45207949aa384783b5d71451f7fe1c3d", new (ExerciseCategory.Flye, FlyeExerciseName.DumbbellFlye) },
		/* Crunch */ { "61ac0d64602c48fba25af7e5e5dc1f97", new (ExerciseCategory.Crunch, CrunchExerciseName.Crunch) },
		/* Concentrated Curl */ { "3695ef0ec2ce484faedc8ce2bfa2819d", new (ExerciseCategory.Curl, CurlExerciseName.SeatedDumbbellBicepsCurl) },

		// D
		/* Deadlift */ { "cd6046306b2c4c4a8f40e169ec924eb9", new (ExerciseCategory.Deadlift, DeadliftExerciseName.DumbbellDeadlift) },
		/* Dumbbell Squat */ { "7d82b59462a54e61926077ded0becae5", new (ExerciseCategory.Squat, SquatExerciseName.DumbbellSquat) },
		/* Dumbbell Thruster */ { "5ab0baeebee94d3995cb7f2b0332f430", new (ExerciseCategory.Squat, SquatExerciseName.Thrusters) },

		// F
		/* Flutter Kick */ { "6091566fa0674afd96a22fcec3ab18ce", new (ExerciseCategory.Crunch, CrunchExerciseName.FlutterKicks) },
		/* Forearm Side Plank */ { "1c0403c4d7264d83b1c75d18c8cdac4f", new (ExerciseCategory.Plank, PlankExerciseName.SidePlank) },
		/* ForeArm Plank */ { "feb44f24e2b8487b870a35f4501069be", new (ExerciseCategory.Plank, PlankExerciseName.Plank) },
		/* Front to Back Lunge */ { "ed18d837c14746c5af38d4fa03b56918", new (ExerciseCategory.Lunge, LungeExerciseName.DumbbellLunge) },

		// G
		/* Goblet Squat */ { "588e35f7067842979485ff1e4f80df26", new (ExerciseCategory.Squat,SquatExerciseName.GobletSquat) },

		// H
		/* Hammer Curl */ { "114ce849b47a4fabbaad961188bf4f7d", new (ExerciseCategory.Curl, CurlExerciseName.DumbbellHammerCurl) },
		/* High Plank */ { "194cc4f6a88c4abd80afe9bbddb25915", new (ExerciseCategory.Plank, PlankExerciseName.StraightArmPlank) },
		/* Hip Bridge */ { "06a504988ace45faabd927af1479f454", new (ExerciseCategory.HipRaise, HipRaiseExerciseName.BridgeWithLegExtension) },
		/* Hollow Hold */ { "060174b84e3744e6a19fe4ce80411113", new (ExerciseCategory.Crunch, CrunchExerciseName.HollowRock) },

		// L
		/* Lateral Lunge */ { "fb63e1ea19264145ae6856eefacbcb22", new (ExerciseCategory.Lunge, LungeExerciseName.SlidingLateralLunge)},

		// N
		/* Neutral Grip Chest Press */ { "802f10996b5048d08f320d8661f13ee1", new (ExerciseCategory.BenchPress, BenchPressExerciseName.NeutralGripDumbbellBenchPress) },

		// O
		/* Overhead Carry */ { "12057d5f9e144913a824bcae5706966c", new (ExerciseCategory.Carry, CarryExerciseName.OverheadCarry) },
		/* Overhead Extension */ { "f260623343e74d37b165071ee5903199", new (ExerciseCategory.TricepsExtension, TricepsExtensionExerciseName.OverheadDumbbellTricepsExtension) },
		/* Overhead Press */ { "ef0279948228409298cd6bf62c5b122c", new (ExerciseCategory.ShoulderPress, ShoulderPressExerciseName.OverheadDumbbellPress) },

		// P
		/* Push Up */ { "1c4d81ad487849a6995f93e1a6a4b1e4", new (ExerciseCategory.PushUp, PushUpExerciseName.PushUp) },
		/* Punches */ { "d56b610f9958400eb4c40d2385f32aaf", new (ExerciseCategory.Invalid, 0) },

		// R
		/* Russian Twist */ { "5c7b2bc65abc4c44849e2119f1338120", new (ExerciseCategory.Core, CoreExerciseName.RussianTwist) },
		/* Reverse Lunge */ { "c430accc3802486a86ad2de9cb8f01cc", new (ExerciseCategory.Lunge, LungeExerciseName.ReverseSlidingLunge) },

		// S
		///
		/* Scissor Kick */ { "f6a10df381004afba2a2b63447d9968f", new (ExerciseCategory.Crunch, CrunchExerciseName.LegLevers) },
		/* Shoulder Tap */ { "5b33283433e7479390c0d5fc11722f80", new (ExerciseCategory.Plank, PlankExerciseName.StraightArmPlankWithShoulderTouch) },
		/* Skull Crusher */ { "3c72e60de73d43f4b5a774c90dea90cd", new (ExerciseCategory.TricepsExtension, TricepsExtensionExerciseName.DumbbellLyingTricepsExtension) },
		/* Snatch */ { "0ddf8f94acfe4c2289aef5a9bf59e8d9", new (ExerciseCategory.OlympicLift, OlympicLiftExerciseName.SingleArmDumbbellSnatch) },
		/* Split Squat */ { "28833fd99466476ea273d6b94747e3db", new (ExerciseCategory.Squat, SquatExerciseName.DumbbellSplitSquat) },
		///* Squat Jump */ { "", new (ExerciseCategory.Plyo, PlyoExerciseName.DumbbellJumpSquat) },

		// T
		/* Tricep Kickback */ { "da89d743904640d58e8b3f667f08783c", new (ExerciseCategory.TricepsExtension, TricepsExtensionExerciseName.DumbbellKickback) },
		/* Tuck up */ { "3069e7ba28b84005b71c16a3781dda8d", new (ExerciseCategory.SitUp, SitUpExerciseName.BentKneeVUp) },
		/* Twisting Mountain Climber */ { "cc70d143627c45e5b64e2cb116619899", new (ExerciseCategory.Plank, PlankExerciseName.CrossBodyMountainClimber) },

		// V
		/* V-Up */ { "715caba11593427299342c378b444e05", new(ExerciseCategory.SitUp, SitUpExerciseName.VUp) },

		// W
		/* Wide Grip Bent Over Row */ { "d861cb497fcc4e1cba994b7a949a3bac", new (ExerciseCategory.Row, RowExerciseName.WideGripSeatedCableRow) },
		/* Wide Grip Overhead Press */ { "258884d9586b45b3973228147a6b0c48", new (ExerciseCategory.ShoulderPress, ShoulderPressExerciseName.OverheadDumbbellPress) },
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
