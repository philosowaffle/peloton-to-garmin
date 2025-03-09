using Dynastream.Fit;
using System.Collections.Generic;

namespace Conversion;

public static class ExerciseMapping
{
	public static IReadOnlyCollection<string> IgnoredPelotonExercises = new HashSet<string>() 
	{
		// A
		/* Active Recovery */ "b067c9f7a1e4412190b0f8eb3c6128e3",
		/* AMRAP */ "98cde50f696746ff98727d5362229cfb",

		// D
		/* Demo */ "e54412161b594e54a86d6ef23ea3d017",

		// T
		/* Transition */ "a76c5edc0641475189442ecad456057a",

		// W
		/* Warmup */ "0b67e950426440fe8b4eadc426320a56",
	};

	public static readonly Dictionary<string, GarminExercise> StrengthExerciseMappings = new()
	{
		// A
		/* Arnold Press */ { "01251235527748368069f9dc898aadf3", new (ExerciseCategory.ShoulderPress, ShoulderPressExerciseName.ArnoldPress) }, 
		/* Arm Circles */ { "1b3400e0aade45e58d2f42cf19af0f40", new (ExerciseCategory.WarmUp, WarmUpExerciseName.ArmCircles) },

		// B
		/* Bear Crawl */ { "46112d47abe24b53ad1fa8e75edcf545", new (ExerciseCategory.Plank, PlankExerciseName.BearCrawl) },
		/* Bent Over Row */ { "d60a1dd8824a49a4926f826b24f3b061", new (ExerciseCategory.Row, RowExerciseName.OneArmBentOverRow) },
		/* Bicep Curl */ { "43d404595338443baab306a6589ae7fc", new (ExerciseCategory.Curl, CurlExerciseName.StandingDumbbellBicepsCurl) },
		/* Bicycle */ { "550b2c852a9547b18ca8e6240c5c6750", new (ExerciseCategory.Crunch, CrunchExerciseName.BicycleCrunch) },
		/* Bird Dog */ { "df8e18c5082f408b8490c4adcb0678b5", new (ExerciseCategory.Plank, PlankExerciseName.PlankWithKneeToElbow) },
		/* Boat Pose */ { "adbac11d3b714403ba22a175a8c95837", new (ExerciseCategory.Crunch, CrunchExerciseName.HollowRock) },
		/* Body Weight Squat */ { "46d30aa1a4a245ada8a5e5ff8f3e7662", new (ExerciseCategory.Squat, SquatExerciseName.Squat) },

		// C
		/* Chest Fly */ { "45207949aa384783b5d71451f7fe1c3d", new (ExerciseCategory.Flye, FlyeExerciseName.DumbbellFlye) },
		/* Clam Shell */ { "5749daaf9be3448397af9d813d760ff3", new (ExerciseCategory.HipRaise, HipRaiseExerciseName.Clams) },
		/* Clean */ { "f2a28d3ebf3c4844a704d2b94e283099", new (ExerciseCategory.OlympicLift, OlympicLiftExerciseName.DumbbellClean) },
		/* Concentrated Curl */ { "3695ef0ec2ce484faedc8ce2bfa2819d", new (ExerciseCategory.Curl, CurlExerciseName.SeatedDumbbellBicepsCurl) },
		/* Criss-Cross */ { "b62c0d2189ae4c2fb6f5203fa145010a", new (ExerciseCategory.Core, CoreExerciseName.CrissCross) },
		/* Crunch */ { "61ac0d64602c48fba25af7e5e5dc1f97", new (ExerciseCategory.Crunch, CrunchExerciseName.Crunch) },
		/* Cursh Press */ { "4899db2664ce47da8ec14282282d3b0d", new (ExerciseCategory.BenchPress, BenchPressExerciseName.CloseGripBarbellBenchPress) },
		/* Cross Body Curl */ { "a66b797fc2014b799cc0cb114d9c5079", new (ExerciseCategory.Curl, CurlExerciseName.CrossBodyDumbbellHammerCurl) },

		// D
		/* Dead Bug */ { "3001f790c7ca471e8ba6d1b57a3a842d", new (ExerciseCategory.HipStability, HipStabilityExerciseName.DeadBug) },
		/* Deadlift */ { "cd6046306b2c4c4a8f40e169ec924eb9", new (ExerciseCategory.Deadlift, DeadliftExerciseName.DumbbellDeadlift) },
		/* Dumbbell Pushup */ { "ce8c746fb5224e9dbc401fef0013a54f", new (ExerciseCategory.PushUp, PushUpExerciseName.PushUp) },
		/* Dumbbell Single Leg Deadlift */ { "6dd608bcc9394b49a68a918359839202", new (ExerciseCategory.Deadlift, DeadliftExerciseName.SingleLegRomanianDeadliftWithDumbbell) },
		/* Dumbbell Squat */ { "7d82b59462a54e61926077ded0becae5", new (ExerciseCategory.Squat, SquatExerciseName.DumbbellSquat) },
		/* Dumbbell Sumo Deadlisft */ { "cd25b61809884d60adb1d97cd646f4fd", new (ExerciseCategory.Deadlift, DeadliftExerciseName.SumoDeadlift) },
		/* Dumbbell Swing */ { "4460e019d86c4e4ebe7284bb16d128d2", new (ExerciseCategory.HipSwing, HipSwingExerciseName.SingleArmDumbbellSwing) },
		/* Dumbbell Thruster */ { "5ab0baeebee94d3995cb7f2b0332f430", new (ExerciseCategory.Squat, SquatExerciseName.Thrusters) },
		/* Dolphin */ { "843d434f59f941c0826fc0fe15eb0236", new (ExerciseCategory.Plank, PlankExerciseName.PlankPikes) },

		// F
		/* Flutter Kick */ { "6091566fa0674afd96a22fcec3ab18ce", new (ExerciseCategory.Crunch, CrunchExerciseName.FlutterKicks) },
		/* Forearm Side Plank */ { "1c0403c4d7264d83b1c75d18c8cdac4f", new (ExerciseCategory.Plank, PlankExerciseName.SidePlank) },
		/* Forearm Side Plank Rotation */ { "223e8e6918d64d9097064d34e3b17e77", new (ExerciseCategory.Plank, PlankExerciseName.SidePlankWithReachUnder) },
		/* ForeArm Plank */ { "feb44f24e2b8487b870a35f4501069be", new (ExerciseCategory.Plank, PlankExerciseName.Plank) },
		/* French Door */ { "78b0a09ce8274e9c8beac6aadd50454b", new (ExerciseCategory.TricepsExtension, TricepsExtensionExerciseName.DumbbellLyingTricepsExtension) },
		/* Front to Back Lunge */ { "ed18d837c14746c5af38d4fa03b56918", new (ExerciseCategory.Lunge, LungeExerciseName.DumbbellLunge) },
		/* Front Lunge */ { "8ef53816dc414bed800e8bf0cee3c484", new (ExerciseCategory.Lunge, LungeExerciseName.DumbbellLunge) },
		/* Front Raise */ { "a9cefac3b8234bc0bc0ee8deb62d67d3", new (ExerciseCategory.LateralRaise, LateralRaiseExerciseName.FrontRaise) },
		/* Front Raise Circles */ { "e099e49b59564ef4a7198bf87cdc1446", new (ExerciseCategory.LateralRaise, LateralRaiseExerciseName.FrontRaise) },

		// G
		/* Goblet Squat */ { "588e35f7067842979485ff1e4f80df26", new (ExerciseCategory.Squat,SquatExerciseName.GobletSquat) },

		// H
		/* Hammer Curl */ { "114ce849b47a4fabbaad961188bf4f7d", new (ExerciseCategory.Curl, CurlExerciseName.DumbbellHammerCurl) },
		/* High Plank */ { "194cc4f6a88c4abd80afe9bbddb25915", new (ExerciseCategory.Plank, PlankExerciseName.StraightArmPlank) },
		/* High Pull */ { "d06c8e68481741de849e4101eda76855", new (ExerciseCategory.Shrug, ShrugExerciseName.DumbbellUprightRow) },
		/* High Side Plank */ { "a6833a0f9c35489585398d1f293600de", new (ExerciseCategory.Plank, PlankExerciseName.SidePlank) },
		/* Hip Bridge */ { "06a504988ace45faabd927af1479f454", new (ExerciseCategory.HipRaise, HipRaiseExerciseName.BarbellHipThrustOnFloor) },
		/* Hollow Hold */ { "060174b84e3744e6a19fe4ce80411113", new (ExerciseCategory.Crunch, CrunchExerciseName.HollowRock) },
		/* Hollow Rock */ { "b264d06330c5442d83ffeaff878cf31d", new (ExerciseCategory.Crunch, CrunchExerciseName.HollowRock) },

		// L
		/* Lateral Lunge */ { "fb63e1ea19264145ae6856eefacbcb22", new (ExerciseCategory.Lunge, LungeExerciseName.SlidingLateralLunge)},
		/* Lawn Mower Row */ { "c80ac3adb6f74487808f361876ba326c", new (ExerciseCategory.Row, RowExerciseName.OneArmBentOverRow)},
		/* Lateral Raise */ { "2635cbe093a140e0be83be83fa594d8b", new (ExerciseCategory.LateralRaise, LateralRaiseExerciseName.SeatedLateralRaise)},
		/* Leg Lift */ { "7a5f7d80783f4b77b44dad8d6a0d2fae", new (ExerciseCategory.HipRaise, HipRaiseExerciseName.LegLift)},

		// N
		/* Narrow-Grip Overhead Press */ { "3caccc04fea1402cab9887ce589833ea", new (ExerciseCategory.ShoulderPress, ShoulderPressExerciseName.OverheadDumbbellPress) },
		/* Neutral Grip Chest Press */ { "802f10996b5048d08f320d8661f13ee1", new (ExerciseCategory.BenchPress, BenchPressExerciseName.NeutralGripDumbbellBenchPress) },

		// O
		/* Oblique Heel Tap */ { "d5ec25fe793f4318a6891607bd3c9259", new (ExerciseCategory.Crunch, CoreExerciseName.SideBend) },
		/* Open Lateral Raise */ { "00046ee377554425866b0a1963b98589", new (ExerciseCategory.LateralRaise, LateralRaiseExerciseName.SeatedLateralRaise) },
		/* Overhead Carry */ { "12057d5f9e144913a824bcae5706966c", new (ExerciseCategory.Carry, CarryExerciseName.OverheadCarry) },
		/* Overhead Extension */ { "f260623343e74d37b165071ee5903199", new (ExerciseCategory.TricepsExtension, TricepsExtensionExerciseName.OverheadDumbbellTricepsExtension) },
		/* Overhead Press */ { "ef0279948228409298cd6bf62c5b122c", new (ExerciseCategory.ShoulderPress, ShoulderPressExerciseName.OverheadDumbbellPress) },

		// P
		/* Pike Push Up */ { "8af39d3485224ac19f7d8659d30524e7", new (ExerciseCategory.PushUp, PushUpExerciseName.ShoulderPushUp) },
		/* Push Up */ { "1c4d81ad487849a6995f93e1a6a4b1e4", new (ExerciseCategory.PushUp, PushUpExerciseName.PushUp) },
		/* Plank Hand Tap */ { "ad8e4f16bf5a450db7d3b72b8ff7b014", new (ExerciseCategory.Plank, PlankExerciseName.StraightArmPlankWithShoulderTouch) },
		/* Plank Pike Reach */ { "67c956e4da6542d1bbfa0625d569f018", new (ExerciseCategory.Plank, PlankExerciseName.PlankPikes) },
		/* Push Press */ { "ae8ada57d3f0424ba391effec04e1e5f", new (ExerciseCategory.ShoulderPress, ShoulderPressExerciseName.DumbbellPushPress) },

		// R
		/* Reverse Lunge */ { "c430accc3802486a86ad2de9cb8f01cc", new (ExerciseCategory.Lunge, LungeExerciseName.ReverseSlidingLunge) },
		/* Reverse Fly */ { "3df6a1136a7a4e4db31e104c7d5a0fcf", new (ExerciseCategory.Flye, FlyeExerciseName.InclineDumbbellFlye) },
		/* Renegade Row */ { "ed9adea36e77459dab7c189884ceb7ab", new (ExerciseCategory.Row, RowExerciseName.RenegadeRow) },
		/* Roll Up */ { "0b853e45afb04c31968b20fc7deaa718", new (ExerciseCategory.Core, CoreExerciseName.RollUp) },
		/* Row */ { "165ed4b439204800b9d88d85363f0609", new (ExerciseCategory.Row, RowExerciseName.DumbbellRow) },
		/* Romanian Deadlift */ { "a17b8d35d1264a2fbabe3ab28df458dc", new (ExerciseCategory.Deadlift, DeadliftExerciseName.DumbbellDeadlift) },// RDL exists in Connect but not in SDK definition
		/* Russian Twist */ { "5c7b2bc65abc4c44849e2119f1338120", new (ExerciseCategory.Core, CoreExerciseName.RussianTwist) },

		// S
		/* Scissor Kick */ { "f6a10df381004afba2a2b63447d9968f", new (ExerciseCategory.Crunch, CrunchExerciseName.LegLevers) },
		/* Scissors */ { "0f9b2d6f18b247bd950d60bbbefd19f3", new (ExerciseCategory.Crunch, CrunchExerciseName.LegLevers) },
		/* Shoulder Tap */ { "5b33283433e7479390c0d5fc11722f80", new (ExerciseCategory.Plank, PlankExerciseName.StraightArmPlankWithShoulderTouch) },
		/* Single Arm Press */ { "97f0a46ff7ad4f03ac9b2cfac96e3b40", new (ExerciseCategory.ShoulderPress, ShoulderPressExerciseName.OverheadDumbbellPress) },
		/* Single Arm Bicep Curl */ { "a466cebb07794281a367dd686794aa62", new (ExerciseCategory.Curl, CurlExerciseName.StandingDumbbellBicepsCurl) },
		/* Single Leg Stretch */ { "32c3f3f1f90446ad8e58589b45ae891b", new (ExerciseCategory.Core, CoreExerciseName.SingleLegStretch) },
		/* Skull Crusher */ { "3c72e60de73d43f4b5a774c90dea90cd", new (ExerciseCategory.TricepsExtension, TricepsExtensionExerciseName.DumbbellLyingTricepsExtension) },
		/* Snatch */ { "0ddf8f94acfe4c2289aef5a9bf59e8d9", new (ExerciseCategory.OlympicLift, OlympicLiftExerciseName.SingleArmDumbbellSnatch) },
		/* Split Squat */ { "28833fd99466476ea273d6b94747e3db", new (ExerciseCategory.Squat, SquatExerciseName.DumbbellSplitSquat) },
		/* Standing Chest Fly */ { "021047bf0cff470bb2d11f94d3539cfe", new (ExerciseCategory.Flye, FlyeExerciseName.DumbbellFlye) },
		/* Straight Leg Bicycle */ { "b617914877c24dac85df81621872e056", new (ExerciseCategory.Core, CoreExerciseName.Bicycle) },
		/* Supinated Row */ { "34dd5f694fd44d15bb0eead604dfebae", new (ExerciseCategory.Row, RowExerciseName.ReverseGripBarbellRow) },

		// T
		/* Table Top Lateral Leg Lift */ { "5bb2d37f052e4d2faf1b0f1de4489531", new (ExerciseCategory.Plank, PlankExerciseName.KneelingSidePlankWithLegLift) },
		/* Tall Kneeling Side Bend */ { "6be91da1de1c49f4b34bf358bdbf3bbc", new (ExerciseCategory.Crunch, CrunchExerciseName.StandingSideCrunch) },
		/* Tricep Dip */ { "0a983b7dfca7400a92761380ff9d351a", new (ExerciseCategory.TricepsExtension, TricepsExtensionExerciseName.BodyWeightDip) },
		/* Tricep Kickback */ { "da89d743904640d58e8b3f667f08783c", new (ExerciseCategory.TricepsExtension, TricepsExtensionExerciseName.DumbbellKickback) },
		/* Tuck up */ { "3069e7ba28b84005b71c16a3781dda8d", new (ExerciseCategory.SitUp, SitUpExerciseName.BentKneeVUp) },
		/* Tricep Push Up */ { "d463a4dc0cf640e0a58f3aa058c5b1a0", new (ExerciseCategory.PushUp, PushUpExerciseName.PushUp) },
		/* Twisting Mountain Climber */ { "cc70d143627c45e5b64e2cb116619899", new (ExerciseCategory.Plank, PlankExerciseName.CrossBodyMountainClimber) },

		// V
		/* V-Up */ { "715caba11593427299342c378b444e05", new(ExerciseCategory.SitUp, SitUpExerciseName.VUp) },

		// W
		/* Woodchop */ { "05d60a7d022f41dd8d3a8da07bca6041", new (ExerciseCategory.Chop, ChopExerciseName.DumbbellChop) },
		/* Wide Grip Chest Press */ { "94e3c37e0cb245f78e195b115a400112", new (ExerciseCategory.BenchPress, BenchPressExerciseName.WideGripBarbellBenchPress) },
		/* Wide Grip Bent Over Row */ { "d861cb497fcc4e1cba994b7a949a3bac", new (ExerciseCategory.Row, RowExerciseName.WideGripSeatedCableRow) },
		/* Wide Grip Overhead Press */ { "258884d9586b45b3973228147a6b0c48", new (ExerciseCategory.ShoulderPress, ShoulderPressExerciseName.OverheadDumbbellPress) },
		/* Wide Row */ { "f05ccd12e95c49aa93ac66bff7ec8df0", new (ExerciseCategory.Row, RowExerciseName.WideGripSeatedCableRow) },
		/* Wrist Curl */ { "d5dfeae09db149fca2c5781d0478e87b", new (ExerciseCategory.Curl, CurlExerciseName.DumbbellWristCurl) },
		/* W-Y's */ { "52dfd0316bd94aae9e461d8d3a69dff1", new (ExerciseCategory.LateralRaise, LateralRaiseExerciseName.SeatedLateralRaise) },

		// Z
		/* Zottman Curl */ { "96b11092c5064b779b371462e2509e82", new (ExerciseCategory.Curl, CurlExerciseName.StandingAlternatingDumbbellCurls) }, // Zottman exists in Connect but not in SDK definition
	};

	public static bool IsRest(this string pelotonExerciseId)
	{
		return pelotonExerciseId == "3ca251f6d68746ad91aebea5c89694ca";
	}
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
