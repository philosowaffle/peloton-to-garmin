using Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Enrichers.Span;
using System;
using System.IO;
using WebApp.Services;

namespace WebApp
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureAppConfiguration(configBuilder =>
				{
					configBuilder.AddJsonFile(Path.Join(Environment.CurrentDirectory, "configuration.local.json"), optional: true, reloadOnChange: true)
					.AddEnvironmentVariables(prefix: "P2G_")
					.AddCommandLine(args)
					.Build();

				})
				.UseSerilog((ctx, logConfig) =>
				{
					logConfig
					.ReadFrom.Configuration(ctx.Configuration, sectionName: $"{nameof(Observability)}:Serilog")
					.Enrich.WithSpan()
					.Enrich.FromLogContext();
				})
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				})
				.ConfigureServices(services =>
				{
					services.AddHostedService<BackgroundSyncJob>();
				});
	}
}
