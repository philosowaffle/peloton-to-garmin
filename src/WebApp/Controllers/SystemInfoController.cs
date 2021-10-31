using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Reflection;
using WebApp.Models;

namespace WebApp.Controllers
{
	[ApiController]
	public class SystemInfoController : Controller
	{
		[HttpGet]
		[Route("/about")]
		[ApiExplorerSettings(IgnoreApi = true)]
		public ActionResult Index()
		{
			return View(GetData());
		}

		[HttpGet]
		[Route("/api/systeminfo")]
		public SystemInfoGetResponse Get()
		{
			return GetData();
		}

		private SystemInfoGetResponse GetData()
		{
			return new SystemInfoGetResponse()
			{
				OperatingSystem = Environment.OSVersion.Platform.ToString(),
				OperatingSystemVersion = Environment.OSVersion.VersionString,

				RunTimeVersion = Environment.Version.ToString(),

				Version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion,

				Website = "https://github.com/philosowaffle/peloton-to-garmin",
				Documentation = "https://github.com/philosowaffle/peloton-to-garmin/wiki/Peloton-To-Garmin---v2",
				Forums = "https://github.com/philosowaffle/peloton-to-garmin/discussions",
				Donate = "https://www.buymeacoffee.com/philosowaffle",
				Issues = "https://github.com/philosowaffle/peloton-to-garmin/issues",
				Api = "/swagger"
			};
		}
	}
}
