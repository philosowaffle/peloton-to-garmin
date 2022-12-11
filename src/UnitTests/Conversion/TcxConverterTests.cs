using NUnit.Framework;
using Moq.AutoMock;
using Conversion;
using Common;
using Common.Service;
using FluentAssertions;

namespace UnitTests.Conversion;

public class TcxConverterTests
{
	[Test]
	public void Converter_Should_Provide_Formt_of_TCX()
	{
		var mocker = new AutoMocker();
		var converter = mocker.CreateInstance<TcxConverterInstance>();

		converter.Format.Should().Be(FileFormat.Tcx);
	}

	[Test]
	public void ShouldConvert_ShouldOnly_Support_TCX([Values] bool tcx, [Values] bool json, [Values] bool fit)
	{
		var mocker = new AutoMocker();
		var converter = mocker.CreateInstance<TcxConverterInstance>();

		var formatSettings = new Format()
		{
			Fit = fit,
			Tcx = tcx,
			Json = json
		};

		converter.ShouldConvert(formatSettings).Should().Be(tcx);
	}

	private class TcxConverterInstance : TcxConverter
	{
		public new FileFormat Format => base.Format;

		public TcxConverterInstance(ISettingsService settings, IFileHandling fileHandler) : base(settings, fileHandler) { }

		public new bool ShouldConvert(Format settings) => base.ShouldConvert(settings);
	}
}
