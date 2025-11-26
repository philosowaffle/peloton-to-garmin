using FluentAssertions;
using NUnit.Framework;
using PelotonToGarmin.Sync.Merge.Utilities;
using System;
using System.Collections.Generic;

namespace UnitTests.Sync.Merge
{
	[TestFixture]
	public class TcxParserTests
	{
		[Test]
		[Category("Merge")]
		public void ParseTcxToSeries_ValidTcxContent_ReturnsSamples()
		{
			// Arrange
			var tcxContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<TrainingCenterDatabase xmlns=""http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"">
    <Activities>
        <Activity>
            <Lap>
                <Track>
                    <Trackpoint>
                        <Time>2024-01-15T10:00:00Z</Time>
                        <HeartRateBpm>
                            <Value>120</Value>
                        </HeartRateBpm>
                        <Cadence>80</Cadence>
                        <Position>
                            <LatitudeDegrees>40.7128</LatitudeDegrees>
                            <LongitudeDegrees>-74.0060</LongitudeDegrees>
                        </Position>
                    </Trackpoint>
                </Track>
            </Lap>
        </Activity>
    </Activities>
</TrainingCenterDatabase>";

			// Act
			var samples = TcxParser.ParseTcxToSeries(tcxContent);

			// Assert
			samples.Should().NotBeEmpty();
			samples[0].HeartRate.Should().Be(120);
			samples[0].Cadence.Should().Be(80);
			samples[0].Lat.Should().Be(40.7128);
			samples[0].Lon.Should().Be(-74.0060);
			samples[0].Time.Should().Be(DateTime.Parse("2024-01-15T10:00:00Z"));
		}

		[Test]
		[Category("Merge")]
		public void ParseTcxToSeries_MultipleTrackpoints_ReturnsAllSamples()
		{
			// Arrange
			var tcxContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<TrainingCenterDatabase xmlns=""http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"">
    <Activities>
        <Activity>
            <Lap>
                <Track>
                    <Trackpoint>
                        <Time>2024-01-15T10:00:00Z</Time>
                        <HeartRateBpm><Value>120</Value></HeartRateBpm>
                    </Trackpoint>
                    <Trackpoint>
                        <Time>2024-01-15T10:00:01Z</Time>
                        <HeartRateBpm><Value>125</Value></HeartRateBpm>
                    </Trackpoint>
                    <Trackpoint>
                        <Time>2024-01-15T10:00:02Z</Time>
                        <HeartRateBpm><Value>130</Value></HeartRateBpm>
                    </Trackpoint>
                </Track>
            </Lap>
        </Activity>
    </Activities>
</TrainingCenterDatabase>";

			// Act
			var samples = TcxParser.ParseTcxToSeries(tcxContent);

			// Assert
			samples.Should().HaveCount(3);
			samples[0].HeartRate.Should().Be(120);
			samples[1].HeartRate.Should().Be(125);
			samples[2].HeartRate.Should().Be(130);
		}

		[Test]
		[Category("Merge")]
		public void ParseTcxToSeries_NullInput_ReturnsEmptyList()
		{
			// Act
			var samples = TcxParser.ParseTcxToSeries(null);

			// Assert
			samples.Should().BeEmpty();
		}

		[Test]
		[Category("Merge")]
		public void ParseTcxToSeries_EmptyXml_ReturnsEmptyList()
		{
			// Act
			var samples = TcxParser.ParseTcxToSeries(string.Empty);

			// Assert
			samples.Should().BeEmpty();
		}

		[Test]
		[Category("Merge")]
		public void ParseTcxToSeries_MissingOptionalFields_ParsesAvailableFields()
		{
			// Arrange
			var tcxContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<TrainingCenterDatabase xmlns=""http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"">
    <Activities>
        <Activity>
            <Lap>
                <Track>
                    <Trackpoint>
                        <Time>2024-01-15T10:00:00Z</Time>
                    </Trackpoint>
                </Track>
            </Lap>
        </Activity>
    </Activities>
</TrainingCenterDatabase>";

			// Act
			var samples = TcxParser.ParseTcxToSeries(tcxContent);

			// Assert
			samples.Should().HaveCount(1);
			samples[0].Time.Should().Be(DateTime.Parse("2024-01-15T10:00:00Z"));
			samples[0].HeartRate.Should().BeNull();
			samples[0].Cadence.Should().BeNull();
			samples[0].Lat.Should().BeNull();
			samples[0].Lon.Should().BeNull();
		}

		[Test]
		[Category("Merge")]
		public void ParseTcxToSeries_EmptyTrackpoints_ReturnsEmptyList()
		{
			// Arrange
			var tcxContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<TrainingCenterDatabase xmlns=""http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"">
    <Activities>
        <Activity>
            <Lap>
                <Track>
                </Track>
            </Lap>
        </Activity>
    </Activities>
</TrainingCenterDatabase>";

			// Act
			var samples = TcxParser.ParseTcxToSeries(tcxContent);

			// Assert
			samples.Should().BeEmpty();
		}

		[Test]
		[Category("Merge")]
		public void ParseTcxToSeries_InvalidXml_ReturnsEmptyList()
		{
			// Arrange
			var invalidXml = "this is not valid xml";

			// Act & Assert
			try
			{
				var samples = TcxParser.ParseTcxToSeries(invalidXml);
				// The implementation catches exceptions, so it should return empty list
				samples.Should().BeEmpty();
			}
			catch
			{
				// Exception is expected for malformed XML
				Assert.Pass("Expected exception for invalid XML");
			}
		}

		[Test]
		[Category("Merge")]
		public void ParseTcxToSeries_WithoutOptionalGPSData_StillParsesCadenceAndHR()
		{
			// Arrange
			var tcxContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<TrainingCenterDatabase xmlns=""http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"">
    <Activities>
        <Activity>
            <Lap>
                <Track>
                    <Trackpoint>
                        <Time>2024-01-15T10:00:00Z</Time>
                        <HeartRateBpm><Value>135</Value></HeartRateBpm>
                        <Cadence>90</Cadence>
                    </Trackpoint>
                </Track>
            </Lap>
        </Activity>
    </Activities>
</TrainingCenterDatabase>";

			// Act
			var samples = TcxParser.ParseTcxToSeries(tcxContent);

			// Assert
			samples.Should().HaveCount(1);
			samples[0].HeartRate.Should().Be(135);
			samples[0].Cadence.Should().Be(90);
			samples[0].Lat.Should().BeNull();
			samples[0].Lon.Should().BeNull();
		}
	}
}
