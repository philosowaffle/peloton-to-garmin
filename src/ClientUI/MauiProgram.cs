﻿using Common.Observe;
using Common;
using Microsoft.Extensions.Logging;
using SharedUI;
using Common.Stateful;
using Microsoft.Extensions.Configuration;
using SharedStartup;

namespace ClientUI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		///////////////////////////////////////////////////////////
		/// STATICS
		///////////////////////////////////////////////////////////
		Statics.AppType = Constants.ClientUIName;
		Statics.MetricPrefix = Constants.ClientUIName;
		Statics.TracingService = Constants.ClientUIName;

		Statics.DefaultDataDirectory = FileSystem.Current.AppDataDirectory;
		Statics.DefaultOutputDirectory = FileSystem.Current.AppDataDirectory;
		Statics.DefaultTempDirectory = FileSystem.Current.CacheDirectory;

		Directory.CreateDirectory(Path.Combine(Statics.DefaultOutputDirectory, "output"));

		///////////////////////////////////////////////////////////
		/// HOST
		///////////////////////////////////////////////////////////
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		var observabilityConfigFilePath = Path.Join(Statics.DefaultDataDirectory, "configuration.local.json");
		if (!File.Exists(observabilityConfigFilePath))
			InitObservabilityConfigFile("configuration.local.json", observabilityConfigFilePath);

		var configProvider = builder.Configuration.AddJsonFile(observabilityConfigFilePath, optional: true, reloadOnChange: true)
				.AddEnvironmentVariables(prefix: "P2G_");

		var config = new AppConfiguration();
		ConfigurationSetup.LoadConfigValues(builder.Configuration, config);

		///////////////////////////////////////////////////////////
		/// SERVICES
		///////////////////////////////////////////////////////////

		// API CLIENT
		builder.Services.AddSingleton<IApiClient, ServiceClient>();

		builder.Services.ConfigureSharedUIServices();
		builder.Services.ConfigureP2GApiServices();

		ObservabilityStartup.Configure(builder.Services, builder.Configuration, config, hardcodeFileLogging: true);

		///////////////////////////////////////////////////////////
		/// APP
		///////////////////////////////////////////////////////////

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		// Setup initial Tracing Source
		Tracing.Source = new(Statics.TracingService);

		return builder.Build();
	}

	private static void InitObservabilityConfigFile(string sourceFileName, string destinationPath)
	{
		using FileStream outputStream = File.OpenWrite(destinationPath);
		using Stream fs = FileSystem.Current.OpenAppPackageFileAsync(sourceFileName).GetAwaiter().GetResult();
		using BinaryWriter writer = new BinaryWriter(outputStream);
		using (BinaryReader reader = new BinaryReader(fs))
		{
			var bytesRead = 0;

			int bufferSize = 1024;
			var buffer = new byte[bufferSize];
			using (fs)
			{
				do
				{
					buffer = reader.ReadBytes(bufferSize);
					bytesRead = buffer.Count();
					writer.Write(buffer);
				}

				while (bytesRead > 0);

			}
		}
	}
}