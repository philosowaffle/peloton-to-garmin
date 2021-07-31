using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Reflection;
using WebUI.Shared;

namespace WebUI.Server.Controllers
{
	[ApiController]
	[Route("/api/systeminfo")]
	public class SystemInfoController : Controller
	{
		[HttpGet]
		public SystemInfoGetResponse Index()
		{
			var systemInfo = new SystemInfoGetResponse()
			{
				OperatingSystem = Environment.OSVersion.Platform.ToString(),
				OperatingSystemVersion = Environment.OSVersion.VersionString,

				RunTimeVersion = Environment.Version.ToString(),

				Version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion,

				Website = "https://github.com/philosowaffle/peloton-to-garmin",
				Documentation = "https://github.com/philosowaffle/peloton-to-garmin/wiki/Peloton-To-Garmin---v2",
				Forums = "https://github.com/philosowaffle/peloton-to-garmin/discussions",
				Donate = "https://www.buymeacoffee.com/philosowaffle",
				Issues = "https://github.com/philosowaffle/peloton-to-garmin/issues"

			};

			return systemInfo;
		}
	}
}
