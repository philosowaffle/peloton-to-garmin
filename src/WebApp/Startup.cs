using Common;
using Common.Database;
using Common.Observe;
using Common.Service;
using Conversion;
using Garmin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Peloton;
using Prometheus;
using Prometheus.DotNetRuntime;
using Serilog;
using Serilog.Events;
using Sync;
using System;
using System.Diagnostics;
using System.Reflection;

namespace WebApp
{
    public class Startup
	{
		private static readonly Gauge BuildInfo = Prometheus.Metrics.CreateGauge("p2g_build_info", "Build info for the running instance.", new GaugeConfiguration()
		{
			LabelNames = new[] { Common.Observe.Metrics.Label.Version, Common.Observe.Metrics.Label.Os, Common.Observe.Metrics.Label.OsVersion, Common.Observe.Metrics.Label.DotNetRuntime }
		});

		private readonly AppConfiguration _config;
		private static IDisposable _metricCollector;

		public IConfiguration ConfigurationProvider { get; }

		public Startup(IConfiguration configuration)
		{
			ConfigurationProvider = configuration;
			_config = new AppConfiguration();

			LoadConfigValues(_config);

			if (_config.Observability.Prometheus.Enabled)
			{
				_metricCollector = CreateMetricCollector();
			}
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton<ISettingsDb, SettingsDb>();
			services.AddSingleton<ISettingsService, SettingsService>();

			services.AddTransient<Settings>((serviceProvider) => 
			{
				using var tracing = Tracing.Trace($"{nameof(Startup)}.{nameof(ConfigureServices)}.DI");
				var settingsService = serviceProvider.GetService<ISettingsService>();
				return settingsService.GetSettingsAsync().GetAwaiter().GetResult();
			});

			services.AddSingleton<AppConfiguration>((serviceProvider) =>
			{
				var config = new AppConfiguration();
				LoadConfigValues(config);

				ChangeToken.OnChange(() => ConfigurationProvider.GetReloadToken(), () =>
				{
					Log.Information("Config change detected, reloading config values.");
					LoadConfigValues(config);
					Log.Information("Config reloaded. Changes will take effect at the end of the current sleeping cycle.");
				});

				return config;
			});

			services.AddSingleton<IDbClient, DbClient>();
			services.AddSingleton<IFileHandling, IOWrapper>();
			services.AddTransient<IPelotonApi, Peloton.ApiClient>();
			services.AddTransient<IPelotonService, PelotonService>();
			services.AddTransient<IGarminUploader, GarminUploader>();

			services.AddSingleton<ISyncStatusDb, SyncStatusDb>();
			services.AddTransient<ISyncService, SyncService>();

			services.AddTransient<IConverter, FitConverter>();
			services.AddTransient<IConverter, TcxConverter>();

			FlurlConfiguration.Configure(_config.Observability);

			Log.Logger = new LoggerConfiguration()
					.ReadFrom.Configuration(ConfigurationProvider, sectionName: $"{nameof(Observability)}:Serilog")
					.Enrich.FromLogContext()
					.CreateLogger();

			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo() { Title = "P2G API", Version = "v1" });
			});

			if (_config.Observability.Jaeger.Enabled)
				ConfigureTracing(services);

			var runtimeVersion = Environment.Version.ToString();
			var os = Environment.OSVersion.Platform.ToString();
			var osVersion = Environment.OSVersion.VersionString;
			var assembly = Assembly.GetExecutingAssembly();
			var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
			var version = versionInfo.ProductVersion;

			BuildInfo.WithLabels(version, os, osVersion, runtimeVersion).Set(1);
			Log.Debug("P2G Version: {@Version}", version);
			Log.Debug("Operating System: {@Os}", osVersion);
			Log.Debug("DotNet Runtime: {@DotnetRuntime}", runtimeVersion);

			services.AddControllersWithViews();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Home/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			if (Log.IsEnabled(LogEventLevel.Verbose))
				app.UseSerilogRequestLogging();

			app.UseSwagger();
			app.UseSwaggerUI();

			if (_config.Observability.Prometheus.Enabled)
				app.UseHttpMetrics();

			app.UseHttpsRedirection();
			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=Home}/{action=Index}/{id?}");

				if (_config.Observability.Prometheus.Enabled)
					endpoints.MapMetrics();
			});

            app.Use(async (context, next) =>
            {
                using var tracing = Tracing.Trace($"{nameof(Startup)}.{nameof(Configure)}.Entrypoint");
                await next.Invoke();
            });
        }

		private void LoadConfigValues(AppConfiguration config)
		{
			ConfigurationProvider.GetSection(nameof(Observability)).Bind(config.Observability);
			ConfigurationProvider.GetSection(nameof(Developer)).Bind(config.Developer);
		}

		private static IDisposable CreateMetricCollector()
		{
			Log.Information("Metrics Enabled");
			return DotNetRuntimeStatsBuilder
					.Customize()
					.WithContentionStats()
					.WithJitStats()
					.WithThreadPoolStats()
					.WithGcStats()
					.WithExceptionStats()
					//.WithDebuggingMetrics(true)
					.WithErrorHandler(ex => Log.Error(ex, "Unexpected exception occurred in prometheus-net.DotNetRuntime"))
					.StartCollecting();
		}

		private void ConfigureTracing(IServiceCollection services)
		{
			services.AddOpenTelemetryTracing(
				(builder) => builder
					.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("p2g"))
					.AddSource("P2G")
					.SetSampler(new AlwaysOnSampler())
					.SetErrorStatusOnException()
					.AddAspNetCoreInstrumentation(c =>
					{
						c.RecordException = true;
						c.Enrich = (activity, name, rawEventObject) =>
						{
							activity.SetTag(TagKey.App, TagValue.P2G);
							activity.SetTag("SpanId", activity.SpanId);
							activity.SetTag("TraceId", activity.TraceId);
						};
					})
					.AddHttpClientInstrumentation(h =>
					{
						h.RecordException = true;
						h.Enrich = (activity, name, rawEventObject) =>
						{
							activity.SetTag(TagKey.App, TagValue.P2G);
							activity.SetTag("SpanId", activity.SpanId);
							activity.SetTag("TraceId", activity.TraceId);
						};
					})
					.AddJaegerExporter(o =>
					{
						o.AgentHost = _config.Observability.Jaeger.AgentHost;
						o.AgentPort = _config.Observability.Jaeger.AgentPort.GetValueOrDefault();
					})
				);
		}
	}
}
