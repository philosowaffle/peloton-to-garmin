﻿using Common;
using Microsoft.Extensions.Logging;
using SharedUI;
using Common.Stateful;
using Microsoft.Extensions.Configuration;
using SharedStartup;
using Common.Database;

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
		Statics.ConfigPath = Path.Join(Environment.CurrentDirectory, "configuration.local.json");

		Directory.CreateDirectory(Statics.DefaultOutputDirectory);

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

		var observabilityConfigFilePath = Statics.ConfigPath;
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

		ObservabilityStartup.ConfigureClientUI(builder.Services, builder.Configuration, config);

		///////////////////////////////////////////////////////////
		/// APP
		///////////////////////////////////////////////////////////

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

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