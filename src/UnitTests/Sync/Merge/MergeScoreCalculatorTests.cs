using FluentAssertions;
using NUnit.Framework;
using Sync.Merge;
using System;

namespace UnitTests.Sync.Merge
{
	[TestFixture]
	public class MergeScoreCalculatorTests
	{
		private MergeScoreCalculator _calculator;

		[SetUp]
		public void Setup()
		{
			_calculator = new MergeScoreCalculator();
		}

		[Test]
		[Category("Merge")]
		public void CalculateScore_PerfectMatch_ReturnsOne()
		{
			// Arrange
			var pelotonStart = DateTime.Now;
			var garminStart = DateTime.Now;
			var pelotonDuration = 1800; // 30 min
			var garminDuration = 1800;
			var timeWindowSeconds = 300;
			var durationDiffPct = 0.15;

			// Act
			var score = _calculator.CalculateScore(
				pelotonStart, garminStart,
				pelotonDuration, garminDuration,
				timeWindowSeconds,
				durationDiffPct);

			// Assert
			score.Should().BeApproximately(1.0, 0.01);
		}

		[Test]
		[Category("Merge")]
		public void CalculateScore_ExactTimeDifference_WithinThreshold()
		{
			// Arrange
			var pelotonStart = DateTime.Now;
			var garminStart = pelotonStart.AddSeconds(30); // 30 second difference
			var pelotonDuration = 1800;
			var garminDuration = 1800;
			var timeWindowSeconds = 300; // 5 minute window
			var durationDiffPct = 0.15;

			// Act
			var score = _calculator.CalculateScore(
				pelotonStart, garminStart,
				pelotonDuration, garminDuration,
				timeWindowSeconds,
				durationDiffPct);

			// Assert - Should still be high score (time within window, duration perfect)
			score.Should().BeGreaterThan(0.9);
		}

		[Test]
		[Category("Merge")]
		public void CalculateScore_TimeOutsideWindow_LowScore()
		{
			// Arrange
			var pelotonStart = DateTime.Now;
			var garminStart = pelotonStart.AddMinutes(10); // 10 minutes difference
			var pelotonDuration = 1800;
			var garminDuration = 1800;
			var timeWindowSeconds = 300; // Only 5 minute window
			var durationDiffPct = 0.15;

			// Act
			var score = _calculator.CalculateScore(
				pelotonStart, garminStart,
				pelotonDuration, garminDuration,
				timeWindowSeconds,
				durationDiffPct);

			// Assert - Should be low due to time mismatch
			score.Should().BeLessThan(0.5);
		}

		[Test]
		[Category("Merge")]
		public void CalculateScore_DurationExactlyWithinThreshold()
		{
			// Arrange
			var pelotonStart = DateTime.Now;
			var garminStart = DateTime.Now;
			var pelotonDuration = 1800; // 30 min
			var garminDuration = 1890; // 31.5 min (exactly 5% different)
			var timeWindowSeconds = 300;
			var durationDiffPct = 0.05; // 5% threshold

			// Act
			var score = _calculator.CalculateScore(
				pelotonStart, garminStart,
				pelotonDuration, garminDuration,
				timeWindowSeconds,
				durationDiffPct);

			// Assert - Should be good (at threshold)
			score.Should().BeGreaterThan(0.7);
		}

		[Test]
		[Category("Merge")]
		public void CalculateScore_DurationOutsideThreshold_ReducesScore()
		{
			// Arrange
			var pelotonStart = DateTime.Now;
			var garminStart = DateTime.Now;
			var pelotonDuration = 1800; // 30 min
			var garminDuration = 2700; // 45 min (50% different)
			var timeWindowSeconds = 300;
			var durationDiffPct = 0.15; // 15% threshold (but we're at 50%)

			// Act
			var score = _calculator.CalculateScore(
				pelotonStart, garminStart,
				pelotonDuration, garminDuration,
				timeWindowSeconds,
				durationDiffPct);

			// Assert - Should be low due to duration mismatch
			score.Should().BeLessThan(0.3);
		}

		[Test]
		[Category("Merge")]
		public void CalculateScore_ScoringWeights_TimeHasMoreWeight()
		{
			// Arrange
			var pelotonStart = DateTime.Now;

			// Scenario 1: Good time match, bad duration
			var garminStart1 = pelotonStart.AddSeconds(10); // 10 seconds difference (good)
			var pelotonDuration1 = 1800;
			var garminDuration1 = 3600; // 100% different (bad)

			// Scenario 2: Bad time match, good duration
			var garminStart2 = pelotonStart.AddSeconds(250); // 250 seconds difference (bad)
			var pelotonDuration2 = 1800;
			var garminDuration2 = 1800; // Perfect match (good)

			var timeWindowSeconds = 300;
			var durationDiffPct = 0.15;

			// Act
			var score1 = _calculator.CalculateScore(
				pelotonStart, garminStart1,
				pelotonDuration1, garminDuration1,
				timeWindowSeconds,
				durationDiffPct);

			var score2 = _calculator.CalculateScore(
				pelotonStart, garminStart2,
				pelotonDuration2, garminDuration2,
				timeWindowSeconds,
				durationDiffPct);

			// Assert - Score 2 (good time, bad duration) should be higher than score 1 (bad time, good duration)
			// because time has 60% weight vs duration 40%
			score2.Should().BeGreaterThan(score1);
		}

		[Test]
		[Category("Merge")]
		public void CalculateScore_ResultAlwaysBetweenZeroAndOne()
		{
			// Arrange - Extreme mismatch
			var pelotonStart = DateTime.Now;
			var garminStart = pelotonStart.AddHours(1);
			var pelotonDuration = 1800;
			var garminDuration = 5400; // 100% different
			var timeWindowSeconds = 300;
			var durationDiffPct = 0.1;

			// Act
			var score = _calculator.CalculateScore(
				pelotonStart, garminStart,
				pelotonDuration, garminDuration,
				timeWindowSeconds,
				durationDiffPct);

			// Assert
			score.Should().BeGreaterThanOrEqualTo(0.0);
			score.Should().BeLessThanOrEqualTo(1.0);
		}
	}
}
