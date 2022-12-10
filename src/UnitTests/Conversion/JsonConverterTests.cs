using Common;
using Common.Service;
using Conversion;
using FluentAssertions;
using Moq.AutoMock;
using NUnit.Framework;

namespace UnitTests.Conversion;

public class JsonConverterTests
{
	[Test]
	public void Converter_Should_Provide_Formt_of_TCX()
	{
		var mocker = new AutoMocker();
		var converter = mocker.CreateInstance<JsonConverterInstance>();

		converter.Format.Should().Be(FileFormat.Json);
	}

	[Test]
	public void ShouldConvert_ShouldOnly_Support_TCX([Values] bool tcx, [Values] bool json, [Values] bool fit)
	{
		var mocker = new AutoMocker();
		var converter = mocker.CreateInstance<JsonConverterInstance>();

		var formatSettings = new Format()
		{
			Fit = fit,
			Tcx = tcx,
			Json = json
		};

		converter.ShouldConvert(formatSettings).Should().Be(json);
	}

	private class JsonConverterInstance : JsonConverter
	{
		public new FileFormat Format => base.Format;

		public JsonConverterInstance(ISettingsService settings, IFileHandling fileHandler) : base(settings, fileHandler) { }

		public new bool ShouldConvert(Format settings) => base.ShouldConvert(settings);
	}
}
