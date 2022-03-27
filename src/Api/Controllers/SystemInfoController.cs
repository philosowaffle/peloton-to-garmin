﻿using Api.Contracts;
using Common;
using Common.Observe;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
	[ApiController]
	public class SystemInfoController : Controller
	{
		private readonly AppConfiguration _appConfiguration;

		public SystemInfoController(AppConfiguration appConfiguration)
		{
			_appConfiguration = appConfiguration;
		}

		/// <summary>
		/// Fetches information about the service and system.
		/// </summary>
		/// <returns>SystemInfoGetResponse</returns>
		/// <response code="200">Returns the system information</response>
		[HttpGet]
		[Route("/api/systeminfo")]
		[Produces("application/json")]
		public SystemInfoGetResponse Get()
		{
			using var tracing = Tracing.Trace($"{nameof(SystemInfoController)}.{nameof(Get)}");

			return GetData();
		}

		private SystemInfoGetResponse GetData()
		{
			using var tracing = Tracing.Trace($"{nameof(SystemInfoController)}.{nameof(GetData)}");

			return new SystemInfoGetResponse()
			{
				OperatingSystem = Environment.OSVersion.Platform.ToString(),
				OperatingSystemVersion = Environment.OSVersion.VersionString,

				RunTimeVersion = Environment.Version.ToString(),

				Version = Constants.AppVersion,

				GitHub = "https://github.com/philosowaffle/peloton-to-garmin",
				Documentation = "https://philosowaffle.github.io/peloton-to-garmin/",
				Forums = "https://github.com/philosowaffle/peloton-to-garmin/discussions",
				Donate = "https://www.buymeacoffee.com/philosowaffle",
				Issues = "https://github.com/philosowaffle/peloton-to-garmin/issues",
				Api = $"{_appConfiguration.Api.HostUrl}/swagger"
			};
		}
	}
}
