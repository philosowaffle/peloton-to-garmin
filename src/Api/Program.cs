using Api.Services;
using Common;
using Common.Database;
using Common.Observe;
using Common.Service;
using Conversion;
using Garmin;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Peloton;
using Prometheus;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Sync;

///////////////////////////////////////////////////////////
/// HOST
///////////////////////////////////////////////////////////
var builder = WebApplication
				.CreateBuilder(args);

var configProvider = builder.Configuration.AddJsonFile(Path.Join(Environment.CurrentDirectory, "configuration.local.json"), optional: true, reloadOnChange: true)
				.AddEnvironmentVariables(prefix: "P2G_")
				.AddCommandLine(args)
				.Build();

var config = new AppConfiguration();
builder.Configuration.GetSection("Api").Bind(config.Api);
builder.Configuration.GetSection(nameof(Observability)).Bind(config.Observability);
builder.Configuration.GetSection(nameof(Developer)).Bind(config.Developer);

builder.WebHost.UseUrls(config.Api.HostUrl);

builder.Host.UseSerilog((ctx, logConfig) =>
{
	logConfig
	.ReadFrom.Configuration(ctx.Configuration, sectionName: $"{nameof(Observability)}:Serilog")
	.Enrich.WithSpan()
	.Enrich.FromLogContext();
});

builder.Host.ConfigureServices(services => services.AddHostedService<BackgroundSyncJob>());

///////////////////////////////////////////////////////////
/// SERVICES
///////////////////////////////////////////////////////////

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo() { Title = "P2G API", Version = "v1" });
});

builder.Services.AddSingleton<ISettingsDb, SettingsDb>();
builder.Services.AddSingleton<ISettingsService, SettingsService>();

builder.Services.AddTransient<Settings>((serviceProvider) =>
{
	using var tracing = Tracing.Trace($"{nameof(Program)}.DI");
	var settingsService = serviceProvider.GetService<ISettingsService>();
	return settingsService.GetSettingsAsync().GetAwaiter().GetResult();
});

builder.Services.AddSingleton<AppConfiguration>((serviceProvider) =>
{
	var config = new AppConfiguration();
	builder.Configuration.GetSection("Api").Bind(config.Api);
	builder.Configuration.GetSection(nameof(Observability)).Bind(config.Observability);
	builder.Configuration.GetSection(nameof(Developer)).Bind(config.Developer);
	return config;
});

builder.Services.AddSingleton<IFileHandling, IOWrapper>();
builder.Services.AddTransient<IPelotonApi, Peloton.ApiClient>();
builder.Services.AddTransient<IPelotonService, PelotonService>();
builder.Services.AddTransient<IGarminUploader, GarminUploader>();

builder.Services.AddSingleton<ISyncStatusDb, SyncStatusDb>();
builder.Services.AddTransient<ISyncService, SyncService>();

builder.Services.AddTransient<IConverter, FitConverter>();
builder.Services.AddTransient<IConverter, TcxConverter>();

FlurlConfiguration.Configure(config.Observability);

Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(builder.Configuration, sectionName: $"{nameof(Observability)}:Serilog")
				.Enrich.FromLogContext()
				.CreateLogger();

if (config.Observability.Jaeger.Enabled)
	ConfigureTracing(builder.Services, config);

var runtimeVersion = Environment.Version.ToString();
var os = Environment.OSVersion.Platform.ToString();
var osVersion = Environment.OSVersion.VersionString;
var version = Constants.AppVersion;

Prometheus.Metrics.CreateGauge("p2g_build_info", "Build info for the running instance.", new GaugeConfiguration()
{
	LabelNames = new[] { Common.Observe.Metrics.Label.Version, Common.Observe.Metrics.Label.Os, Common.Observe.Metrics.Label.OsVersion, Common.Observe.Metrics.Label.DotNetRuntime }
}).WithLabels(version, os, osVersion, runtimeVersion)
.Set(1);

Log.Debug("P2G Version: {@Version}", version);
Log.Debug("Operating System: {@Os}", osVersion);
Log.Debug("DotNet Runtime: {@DotnetRuntime}", runtimeVersion);

///////////////////////////////////////////////////////////
/// APP
///////////////////////////////////////////////////////////

var app = builder.Build();

app.UseCors(options =>
{
	options
	.SetIsOriginAllowed((_) => true)
	.AllowAnyHeader();
});

app.UseSwagger();
app.UseSwaggerUI();

if (Log.IsEnabled(LogEventLevel.Verbose))
	app.UseSerilogRequestLogging();

app.Use(async (context, next) =>
{
	using var tracing = Tracing.Trace($"{nameof(Program)}.Entrypoint");
	await next.Invoke();
});

if (config.Observability.Prometheus.Enabled)
{
	Log.Information("Metrics Enabled");
	Common.Observe.Metrics.EnableCollector(config.Observability.Prometheus, Constants.ApiName);

	app.MapMetrics();
	app.UseHttpMetrics();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

///////////////////////////////////////////////////////////
/// METHODS
///////////////////////////////////////////////////////////
void ConfigureTracing(IServiceCollection services, AppConfiguration config)
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

					if (name.Equals("OnStartActivity")
						&& rawEventObject is HttpRequest httpRequest)
					{
						if (httpRequest.Headers.TryGetValue("TraceId", out var incomingTraceParent))
							activity.SetParentId(incomingTraceParent);

						if (httpRequest.Headers.TryGetValue("uber-trace-id", out incomingTraceParent))
							activity.SetParentId(incomingTraceParent);
					}
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
				o.AgentHost = config.Observability.Jaeger.AgentHost;
				o.AgentPort = config.Observability.Jaeger.AgentPort.GetValueOrDefault();
			})
		);
}