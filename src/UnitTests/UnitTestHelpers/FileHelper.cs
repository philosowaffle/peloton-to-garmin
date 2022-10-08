using System.IO;
using System.Threading.Tasks;

namespace UnitTests.UnitTestHelpers;

public static class FileHelper
{
	public static readonly string DataDirectory = Path.Join(Directory.GetCurrentDirectory(), "..", "..", "..", "Data");

	public static async Task<string> ReadTextFromFileAsync(string path)
	{
		using (var reader = new StreamReader(path))
		{
			var content = await reader.ReadToEndAsync();
			return content;
		}
	}
}
