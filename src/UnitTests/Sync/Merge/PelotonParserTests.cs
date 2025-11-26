using FluentAssertions;
using NUnit.Framework;
using PelotonToGarmin.Sync.Merge.Utilities;
using Common.Dto;
using System;
using System.Collections.Generic;

namespace UnitTests.Sync.Merge
{
	[TestFixture]
	public class PelotonParserTests
	{
		[Test]
		[Category("Merge")]
		public void ParsePelotonToSeries_ValidWorkout_ExtractsSamples()
		{
			// Arrange
			var workout = new P2GWorkout
			{
				Workout = new Workout { Created = DateTime.Now },
				WorkoutSamples = new WorkoutSamples
				{
					Metrics = new List<WorkoutSamplesMetric>
					{
						new WorkoutSamplesMetric
						{
							Slug = "heart_rate",
							Values = new List<WorkoutSamplesMetricValue>
							{
								new WorkoutSamplesMetricValue { Value = 130 },
								new WorkoutSamplesMetricValue { Value = 135 },
								new WorkoutSamplesMetricValue { Value = 140 }
							}
						},
						new WorkoutSamplesMetric
						{
							Slug = "output",
							Values = new List<WorkoutSamplesMetricValue>
							{
								new WorkoutSamplesMetricValue { Value = 200 },
								new WorkoutSamplesMetricValue { Value = 210 },
								new WorkoutSamplesMetricValue { Value = 220 }
							}
						}
					}
				}
			};

			// Act
			var samples = PelotonParser.ParsePelotonToSeries(workout);

			// Assert
			samples.Should().HaveCount(3);
			samples[0].HeartRate.Should().Be(130);
			samples[0].Power.Should().Be(200);
			samples[1].HeartRate.Should().Be(135);
			samples[1].Power.Should().Be(210);
			samples[2].HeartRate.Should().Be(140);
			samples[2].Power.Should().Be(220);
		}

		[Test]
		[Category("Merge")]
		public void ParsePelotonToSeries_WithCadence_ParsesCadenceMetric()
		{
			// Arrange
			var startTime = DateTime.Now;
			var workout = new P2GWorkout
			{
				Workout = new Workout { Created = startTime },
				WorkoutSamples = new WorkoutSamples
				{
					Metrics = new List<WorkoutSamplesMetric>
					{
						new WorkoutSamplesMetric
						{
							Slug = "cadence",
							Values = new List<WorkoutSamplesMetricValue>
							{
								new WorkoutSamplesMetricValue { Value = 85 },
								new WorkoutSamplesMetricValue { Value = 90 }
							}
						}
					}
				}
			};

			// Act
			var samples = PelotonParser.ParsePelotonToSeries(workout);

			// Assert
			samples.Should().HaveCount(2);
			samples[0].Cadence.Should().Be(85);
			samples[1].Cadence.Should().Be(90);
		}

		[Test]
		[Category("Merge")]
		public void ParsePelotonToSeries_NullWorkout_ReturnsEmptyList()
		{
			// Act
			var samples = PelotonParser.ParsePelotonToSeries(null);

			// Assert
			samples.Should().BeEmpty();
		}

		[Test]
		[Category("Merge")]
		public void ParsePelotonToSeries_NoWorkoutSamples_ReturnsEmptyList()
		{
			// Arrange
			var workout = new P2GWorkout
			{
				Workout = new Workout { Created = DateTime.Now },
				WorkoutSamples = null
			};

			// Act
			var samples = PelotonParser.ParsePelotonToSeries(workout);

			// Assert
			samples.Should().BeEmpty();
		}

		[Test]
		[Category("Merge")]
		public void ParsePelotonToSeries_NoMetrics_ReturnsEmptyList()
		{
			// Arrange
			var workout = new P2GWorkout
			{
				Workout = new Workout { Created = DateTime.Now },
				WorkoutSamples = new WorkoutSamples
				{
					Metrics = new List<WorkoutSamplesMetric>()
				}
			};

			// Act
			var samples = PelotonParser.ParsePelotonToSeries(workout);

			// Assert
			samples.Should().BeEmpty();
		}

		[Test]
		[Category("Merge")]
		public void ParsePelotonToSeries_PartialMetricValues_SkipsMissingValues()
		{
			// Arrange
			var startTime = DateTime.Now;
			var workout = new P2GWorkout
			{
				Workout = new Workout { Created = startTime },
				WorkoutSamples = new WorkoutSamples
				{
					Metrics = new List<WorkoutSamplesMetric>
					{
						new WorkoutSamplesMetric
						{
							Slug = "heart_rate",
							Values = new List<WorkoutSamplesMetricValue>
							{
								new WorkoutSamplesMetricValue { Value = 130 },
								null, // Missing value
								new WorkoutSamplesMetricValue { Value = 140 }
							}
						}
					}
				}
			};

			// Act
			var samples = PelotonParser.ParsePelotonToSeries(workout);

			// Assert
			samples.Should().HaveCount(3);
			samples[0].HeartRate.Should().Be(130);
			samples[1].HeartRate.Should().BeNull(); // Skipped null value
			samples[2].HeartRate.Should().Be(140);
		}

		[Test]
		[Category("Merge")]
		public void ParsePelotonToSeries_UnknownMetricSlug_SkipsMetric()
		{
			// Arrange
			var workout = new P2GWorkout
			{
				Workout = new Workout { Created = DateTime.Now },
				WorkoutSamples = new WorkoutSamples
				{
					Metrics = new List<WorkoutSamplesMetric>
					{
						new WorkoutSamplesMetric
						{
							Slug = "unknown_metric",
							Values = new List<WorkoutSamplesMetricValue>
							{
								new WorkoutSamplesMetricValue { Value = 999 }
							}
						},
						new WorkoutSamplesMetric
						{
							Slug = "heart_rate",
							Values = new List<WorkoutSamplesMetricValue>
							{
								new WorkoutSamplesMetricValue { Value = 130 }
							}
						}
					}
				}
			};

			// Act
			var samples = PelotonParser.ParsePelotonToSeries(workout);

			// Assert
			samples.Should().HaveCount(1);
			samples[0].HeartRate.Should().Be(130);
		}

		[Test]
		[Category("Merge")]
		public void ParsePelotonToSeries_TimeIncrementsPerSample()
		{
			// Arrange
			var startTime = new DateTime(2024, 1, 15, 10, 0, 0);
			var workout = new P2GWorkout
			{
				Workout = new Workout { Created = startTime },
				WorkoutSamples = new WorkoutSamples
				{
					Metrics = new List<WorkoutSamplesMetric>
					{
						new WorkoutSamplesMetric
						{
							Slug = "heart_rate",
							Values = new List<WorkoutSamplesMetricValue>
							{
								new WorkoutSamplesMetricValue { Value = 130 },
								new WorkoutSamplesMetricValue { Value = 135 },
								new WorkoutSamplesMetricValue { Value = 140 }
							}
						}
					}
				}
			};

			// Act
			var samples = PelotonParser.ParsePelotonToSeries(workout);

			// Assert
			samples.Should().HaveCount(3);
			samples[0].Time.Should().Be(startTime);
			samples[1].Time.Should().Be(startTime.AddSeconds(1));
			samples[2].Time.Should().Be(startTime.AddSeconds(2));
		}

		[Test]
		[Category("Merge")]
		public void ParsePelotonToSeries_NonNumericValues_SkipsInvalidValues()
		{
			// Arrange
			var workout = new P2GWorkout
			{
				Workout = new Workout { Created = DateTime.Now },
				WorkoutSamples = new WorkoutSamples
				{
					Metrics = new List<WorkoutSamplesMetric>
					{
						new WorkoutSamplesMetric
						{
							Slug = "heart_rate",
							Values = new List<WorkoutSamplesMetricValue>
							{
								new WorkoutSamplesMetricValue { Value = "not_a_number" },
								new WorkoutSamplesMetricValue { Value = 130 }
							}
						}
					}
				}
			};

			// Act
			var samples = PelotonParser.ParsePelotonToSeries(workout);

			// Assert
			samples.Should().HaveCount(2);
			samples[0].HeartRate.Should().BeNull(); // Failed to parse
			samples[1].HeartRate.Should().Be(130);
		}

		[Test]
		[Category("Merge")]
		public void ParsePelotonToSeries_AllMetricsPresent_CombinesAllData()
		{
			// Arrange
			var startTime = DateTime.Now;
			var workout = new P2GWorkout
			{
				Workout = new Workout { Created = startTime },
				WorkoutSamples = new WorkoutSamples
				{
					Metrics = new List<WorkoutSamplesMetric>
					{
						new WorkoutSamplesMetric
						{
							Slug = "heart_rate",
							Values = new List<WorkoutSamplesMetricValue>
							{
								new WorkoutSamplesMetricValue { Value = 140 }
							}
						},
						new WorkoutSamplesMetric
						{
							Slug = "output",
							Values = new List<WorkoutSamplesMetricValue>
							{
								new WorkoutSamplesMetricValue { Value = 250 }
							}
						},
						new WorkoutSamplesMetric
						{
							Slug = "cadence",
							Values = new List<WorkoutSamplesMetricValue>
							{
								new WorkoutSamplesMetricValue { Value = 95 }
							}
						}
					}
				}
			};

			// Act
			var samples = PelotonParser.ParsePelotonToSeries(workout);

			// Assert
			samples.Should().HaveCount(1);
			samples[0].HeartRate.Should().Be(140);
			samples[0].Power.Should().Be(250);
			samples[0].Cadence.Should().Be(95);
			samples[0].Time.Should().Be(startTime);
		}
	}
}
