using System;

namespace P2G.Common.Dto.Strava
{
    /// <summary>
    /// Strava Athlete - данные атлета
    /// </summary>
    public record Athlete
    {
        public long Id { get; init; }
        public string Username { get; init; }
        public string ResourceState { get; init; }
        public string Firstname { get; init; }
        public string Lastname { get; init; }
        public string Bio { get; init; }
        public string City { get; init; }
        public string State { get; init; }
        public string Country { get; init; }
        public string Sex { get; init; }
        public bool Premium { get; init; }
        public bool Summit { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public double? Weight { get; init; }
        public string ProfileMedium { get; init; }
        public string Profile { get; init; }
        public string Friend { get; init; }
        public string Follower { get; init; }
        
        // FTP settings
        public int? Ftp { get; init; }
        
        // Bikes
        public Bike[] Bikes { get; init; }
        
        // Shoes
        public Shoe[] Shoes { get; init; }
    }

    /// <summary>
    /// Bike - велосипед атлета
    /// </summary>
    public record Bike
    {
        public string Id { get; init; }
        public bool Primary { get; init; }
        public string Name { get; init; }
        public double Distance { get; init; }
        public string ResourceState { get; init; }
    }

    /// <summary>
    /// Shoe - обувь атлета
    /// </summary>
    public record Shoe
    {
        public string Id { get; init; }
        public bool Primary { get; init; }
        public string Name { get; init; }
        public double Distance { get; init; }
        public string ResourceState { get; init; }
    }
}
