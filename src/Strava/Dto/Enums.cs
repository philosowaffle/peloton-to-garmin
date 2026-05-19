namespace P2G.Strava.Dto
{
    /// <summary>
    /// Типы активностей Strava
    /// </summary>
    public enum StravaActivityType
    {
        // Водные виды
        AlpineSki,
        BackcountrySki,
        Canoeing,
        Kayaking,
        Kitesurf,
        NordicSki,
        Rowing,
        StandUpPaddling,
        Surfing,
        Swim,
        Snowboard,
        Snowshoe,

        // Велоспорт
        Ride,
        VirtualRide,
        MountainBikeRide,
        GravelRide,
        EBikeRide,
        EBikeMountainRide,
        Handcycle,
        Velomobile,

        // Бег и ходьба
        Run,
        TrailRun,
        VirtualRun,
        Walk,
        Hike,

        // Силовые и фитнес
        WeightTraining,
        Workout,
        Crossfit,
        Elliptical,
        StairStepper,

        // Другие
        InlineSkate,
        IceSkate,
        Yoga,
        Pilates,
        RockClimbing,
        Sail,
        Skateboard,
        Wheelchair,
        Windsurf,
        VirtualRow,
        Hiit,
        Pickleball,
        Badminton,
        Squash,
        TableTennis,
        Tennis,
        Golf,
        eBikeRide, // Дубликат для совместимости
        Unknown
    }

    public static class StravaActivityTypeExtensions
    {
        public static StravaActivityType Parse(string type)
        {
            if (string.IsNullOrEmpty(type))
                return StravaActivityType.Unknown;

            // Убираем префикс "WorkoutType:" если есть
            var cleanType = type.Replace("WorkoutType:", "");

            if (Enum.TryParse<StravaActivityType>(cleanType, true, out var result))
                return result;

            // Маппинг альтернативных названий
            return type.ToLower() switch
            {
                "run" => StravaActivityType.Run,
                "ride" => StravaActivityType.Ride,
                "walk" => StravaActivityType.Walk,
                "weighttraining" or "weight_training" => StravaActivityType.WeightTraining,
                "swim" => StravaActivityType.Swim,
                "workout" => StravaActivityType.Workout,
                "hike" => StravaActivityType.Hike,
                "yoga" => StravaActivityType.Yoga,
                _ => StravaActivityType.Unknown
            };
        }
    }
}
