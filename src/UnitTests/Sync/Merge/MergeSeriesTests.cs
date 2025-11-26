using FluentAssertions;
using NUnit.Framework;
using PelotonToGarmin.Sync.Merge.Utilities;
using System;
using System.Collections.Generic;

namespace UnitTests.Sync.Merge
{
	[TestFixture]
	public class MergeSeriesTests
	{
		private static TcxParser.Sample CreateTcxSample(DateTime time, int? hr = null, int? cadence = null, double? lat = null, double? lon = null)
		{
			return new TcxParser.Sample
			{
				Time = time,
				HeartRate = hr,
				Cadence = cadence,
				Lat = lat,
				Lon = lon
			};
		}

		private static PelotonParser.Sample CreatePelotonSample(DateTime time, int? hr = null, double? power = null, int? cadence = null)
		{
			return new PelotonParser.Sample
			{
				Time = time,
				HeartRate = hr,
				Power = power,
				Cadence = cadence
			};
		}

		[Test]
		[Category("Merge")]
		public void Merge_BothSourcesPresent_CombinesSamples()
		{
			// Arrange
			var baseTime = DateTime.Now;
			var garminSamples = new List<TcxParser.Sample>
			{
				CreateTcxSample(baseTime, hr: 150)
			};
			var pelotonSamples = new List<PelotonParser.Sample>
			{
				CreatePelotonSample(baseTime, power: 250)
			};

			// Act
			var merged = MergeSeries.Merge(garminSamples, pelotonSamples, resolutionSeconds: 1);

			// Assert
			merged.Should().HaveCount(1);
			merged[0].HeartRate.Should().Be(150);
			merged[0].Power.Should().Be(250);
			merged[0].HrSource.Should().Be("garmin");
		}

		[Test]
		[Category("Merge")]
		public void Merge_GarminHRPreferred_SelectsGarminWhenBothAvailable()
		{
			// Arrange
			var baseTime = DateTime.Now;
			var garminSamples = new List<TcxParser.Sample>
			{
				CreateTcxSample(baseTime, hr: 150)
			};
			var pelotonSamples = new List<PelotonParser.Sample>
			{
				CreatePelotonSample(baseTime, hr: 140)
			};

			// Act
			var merged = MergeSeries.Merge(garminSamples, pelotonSamples, resolutionSeconds: 1);

			// Assert
			merged.Should().HaveCount(1);
			merged[0].HeartRate.Should().Be(150); // Prefers Garmin
			merged[0].HrSource.Should().Be("garmin");
		}

		[Test]
		[Category("Merge")]
		public void Merge_FallbackTopelotonHR_SelectsPelotonWhenGarminMissing()
		{
			// Arrange
			var baseTime = DateTime.Now;
			var garminSamples = new List<TcxParser.Sample>
			{
				CreateTcxSample(baseTime, hr: null)
			};
			var pelotonSamples = new List<PelotonParser.Sample>
			{
				CreatePelotonSample(baseTime, hr: 140)
			};

			// Act
			var merged = MergeSeries.Merge(garminSamples, pelotonSamples, resolutionSeconds: 1);

			// Assert
			merged.Should().HaveCount(1);
			merged[0].HeartRate.Should().Be(140); // Falls back to Peloton
			merged[0].HrSource.Should().Be("peloton");
		}

		[Test]
		[Category("Merge")]
		public void Merge_AlwaysUsesPelotonPower_IgnoresGarminPower()
		{
			// Arrange
			var baseTime = DateTime.Now;
			var garminSamples = new List<TcxParser.Sample>
			{
				CreateTcxSample(baseTime) // No power field in TcxParser.Sample
			};
			var pelotonSamples = new List<PelotonParser.Sample>
			{
				CreatePelotonSample(baseTime, power: 250)
			};

			// Act
			var merged = MergeSeries.Merge(garminSamples, pelotonSamples, resolutionSeconds: 1);

			// Assert
			merged.Should().HaveCount(1);
			merged[0].Power.Should().Be(250);
		}

		[Test]
		[Category("Merge")]
		public void Merge_GarminCadencePreferred_SelectsGarminWhenBothAvailable()
		{
			// Arrange
			var baseTime = DateTime.Now;
			var garminSamples = new List<TcxParser.Sample>
			{
				CreateTcxSample(baseTime, cadence: 100)
			};
			var pelotonSamples = new List<PelotonParser.Sample>
			{
				CreatePelotonSample(baseTime, cadence: 90)
			};

			// Act
			var merged = MergeSeries.Merge(garminSamples, pelotonSamples, resolutionSeconds: 1);

			// Assert
			merged.Should().HaveCount(1);
			merged[0].Cadence.Should().Be(100); // Prefers Garmin
		}

		[Test]
		[Category("Merge")]
		public void Merge_FallbackToPelotonCadence_SelectsPelotonWhenGarminMissing()
		{
			// Arrange
			var baseTime = DateTime.Now;
			var garminSamples = new List<TcxParser.Sample>
			{
				CreateTcxSample(baseTime, cadence: null)
			};
			var pelotonSamples = new List<PelotonParser.Sample>
			{
				CreatePelotonSample(baseTime, cadence: 90)
			};

			// Act
			var merged = MergeSeries.Merge(garminSamples, pelotonSamples, resolutionSeconds: 1);

			// Assert
			merged.Should().HaveCount(1);
			merged[0].Cadence.Should().Be(90);
		}

		[Test]
		[Category("Merge")]
		public void Merge_GPSDataAlwaysFromGarmin_IgnoresPelotonGPS()
		{
			// Arrange
			var baseTime = DateTime.Now;
			var garminSamples = new List<TcxParser.Sample>
			{
				CreateTcxSample(baseTime, lat: 40.7128, lon: -74.0060)
			};
			var pelotonSamples = new List<PelotonParser.Sample>
			{
				CreatePelotonSample(baseTime)
			};

			// Act
			var merged = MergeSeries.Merge(garminSamples, pelotonSamples, resolutionSeconds: 1);

			// Assert
			merged.Should().HaveCount(1);
			merged[0].Lat.Should().Be(40.7128);
			merged[0].Lon.Should().Be(-74.0060);
		}

		[Test]
		[Category("Merge")]
		public void Merge_TimeResolution_CreatesIntermediatePoints()
		{
			// Arrange
			var baseTime = DateTime.Now;
			var garminSamples = new List<TcxParser.Sample>
			{
				CreateTcxSample(baseTime, hr: 140),
				CreateTcxSample(baseTime.AddSeconds(3), hr: 160)
			};
			var pelotonSamples = new List<PelotonParser.Sample>();

			// Act
			var merged = MergeSeries.Merge(garminSamples, pelotonSamples, resolutionSeconds: 1);

			// Assert
			merged.Should().HaveCount(4); // baseTime, +1s, +2s, +3s
			merged[0].Time.Should().Be(baseTime);
			merged[1].Time.Should().Be(baseTime.AddSeconds(1));
			merged[2].Time.Should().Be(baseTime.AddSeconds(2));
			merged[3].Time.Should().Be(baseTime.AddSeconds(3));
		}

		[Test]
		[Category("Merge")]
		public void Merge_DifferentTimeOffsets_UsesCompleteTimeline()
		{
			// Arrange
			var baseTime = DateTime.Now;
			var garminSamples = new List<TcxParser.Sample>
			{
				CreateTcxSample(baseTime, hr: 140),
				CreateTcxSample(baseTime.AddSeconds(2), hr: 150)
			};
			var pelotonSamples = new List<PelotonParser.Sample>
			{
				CreatePelotonSample(baseTime.AddSeconds(1), power: 250)
			};

			// Act
			var merged = MergeSeries.Merge(garminSamples, pelotonSamples, resolutionSeconds: 1);

			// Assert
			merged.Should().HaveCount(3); // Covers all timestamps
			merged[0].HeartRate.Should().Be(140); // From Garmin at baseTime
			merged[1].Power.Should().Be(250); // From Peloton at baseTime+1s
			merged[2].HeartRate.Should().Be(150); // From Garmin at baseTime+2s
		}

		[Test]
		[Category("Merge")]
		public void Merge_NoSourcesProvided_ReturnsEmptyList()
		{
			// Act
			var merged = MergeSeries.Merge(null, null, resolutionSeconds: 1);

			// Assert
			merged.Should().BeEmpty();
		}

		[Test]
		[Category("Merge")]
		public void Merge_OnlyGarminProvided_ReturnsGarminData()
		{
			// Arrange
			var baseTime = DateTime.Now;
			var garminSamples = new List<TcxParser.Sample>
			{
				CreateTcxSample(baseTime, hr: 140, cadence: 90, lat: 40.7128, lon: -74.0060)
			};

			// Act
			var merged = MergeSeries.Merge(garminSamples, null, resolutionSeconds: 1);

			// Assert
			merged.Should().HaveCount(1);
			merged[0].HeartRate.Should().Be(140);
			merged[0].Cadence.Should().Be(90);
			merged[0].Lat.Should().Be(40.7128);
			merged[0].Lon.Should().Be(-74.0060);
			merged[0].Power.Should().BeNull();
		}

		[Test]
		[Category("Merge")]
		public void Merge_OnlyPelotonProvided_ReturnsPelotonData()
		{
			// Arrange
			var baseTime = DateTime.Now;
			var pelotonSamples = new List<PelotonParser.Sample>
			{
				CreatePelotonSample(baseTime, hr: 140, power: 250, cadence: 90)
			};

			// Act
			var merged = MergeSeries.Merge(null, pelotonSamples, resolutionSeconds: 1);

			// Assert
			merged.Should().HaveCount(1);
			merged[0].HeartRate.Should().Be(140);
			merged[0].Power.Should().Be(250);
			merged[0].Cadence.Should().Be(90);
			merged[0].Lat.Should().BeNull();
			merged[0].Lon.Should().BeNull();
		}

		[Test]
		[Category("Merge")]
		public void Merge_ResolutionOfTwoSeconds_CreatesCorrectInterval()
		{
			// Arrange
			var baseTime = DateTime.Now;
			var garminSamples = new List<TcxParser.Sample>
			{
				CreateTcxSample(baseTime, hr: 140),
				CreateTcxSample(baseTime.AddSeconds(4), hr: 160)
			};

			// Act
			var merged = MergeSeries.Merge(garminSamples, null, resolutionSeconds: 2);

			// Assert
			merged.Should().HaveCount(3); // baseTime, +2s, +4s
			merged[0].Time.Should().Be(baseTime);
			merged[1].Time.Should().Be(baseTime.AddSeconds(2));
			merged[2].Time.Should().Be(baseTime.AddSeconds(4));
		}

		[Test]
		[Category("Merge")]
		public void Merge_AllFieldsPresent_CreatesCompleteUnifiedSample()
		{
			// Arrange
			var baseTime = DateTime.Now;
			var garminSamples = new List<TcxParser.Sample>
			{
				CreateTcxSample(baseTime, hr: 150, cadence: 100, lat: 40.7128, lon: -74.0060)
			};
			var pelotonSamples = new List<PelotonParser.Sample>
			{
				CreatePelotonSample(baseTime, hr: 140, power: 250, cadence: 90)
			};

			// Act
			var merged = MergeSeries.Merge(garminSamples, pelotonSamples, resolutionSeconds: 1);

			// Assert
			merged.Should().HaveCount(1);
			var sample = merged[0];
			sample.Time.Should().Be(baseTime);
			sample.HeartRate.Should().Be(150); // From Garmin
			sample.Power.Should().Be(250); // From Peloton
			sample.Cadence.Should().Be(100); // From Garmin (preferred)
			sample.Lat.Should().Be(40.7128); // From Garmin
			sample.Lon.Should().Be(-74.0060); // From Garmin
			sample.HrSource.Should().Be("garmin");
		}

		[Test]
		[Category("Merge")]
		public void Merge_EmptyLists_ReturnsEmptyList()
		{
			// Act
			var merged = MergeSeries.Merge(new List<TcxParser.Sample>(), new List<PelotonParser.Sample>(), resolutionSeconds: 1);

			// Assert
			merged.Should().BeEmpty();
		}
	}
}
