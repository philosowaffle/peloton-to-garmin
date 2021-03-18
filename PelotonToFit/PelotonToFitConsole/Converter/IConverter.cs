namespace PelotonToFitConsole.Converter
{
	public interface IConverter
	{
		public void Convert();
		public void Decode(string filePath);
	}
}
