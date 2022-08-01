using Api.Services;
using Common;
using Common.Database;
using Common.Observe;
using Common.Service;
using Common.Stateful;
using Conversion;
using Garmin;
using Peloton;
using Prometheus;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Sync;
using System.Reflection;

///////////////////////////////////////////////////////////
/// STATICS
///////////////////////////////////////////////////////////
Statics.MetricPrefix = Constants.ApiName;
Statics.TracingService = Constants.ApiName;

///////////////////////////////////////////////////////////
/// HOST
///////////////////////////////////////////////////////////
var builder = WebApplication
				.CreateBuilder(args);

var configProvider = builder.Configuration.AddJsonFile(Path.Join(Environment.CurrentDirectory, "configuration.local.json"), optional: true, reloadOnChange: true)
				.AddEnvironmentVariables(prefix: "P2G_")
				.AddCommandLine(args);

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
	var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
	c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

builder.Services.AddSingleton<ISettingsDb, SettingsDb>();
builder.Services.AddSingleton<ISettingsService, SettingsService>();

builder.Services.AddTransient<Settings>((serviceProvider) =>
{
	using var tracing = Tracing.Trace($"{nameof(Program)}.DI");
	var settingsService = serviceProvider.GetService<ISettingsService>();
	return settingsService?.GetSettingsAsync().GetAwaiter().GetResult() ?? new Settings();
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
builder.Services.AddTransient<IConverter, JsonConverter>();

FlurlConfiguration.Configure(config.Observability);
Tracing.EnableTracing(builder.Services, config.Observability.Jaeger);

Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(builder.Configuration, sectionName: $"{nameof(Observability)}:Serilog")
				.Enrich.FromLogContext()
				.CreateLogger();

var runtimeVersion = Environment.Version.ToString();
var os = Environment.OSVersion.Platform.ToString();
var osVersion = Environment.OSVersion.VersionString;
var version = Constants.AppVersion;

Prometheus.Metrics.CreateGauge("p2g_build_info", "Build info for the running instance.", new GaugeConfiguration()
{
	LabelNames = new[] { Common.Observe.Metrics.Label.Version, Common.Observe.Metrics.Label.Os, Common.Observe.Metrics.Label.OsVersion, Common.Observe.Metrics.Label.DotNetRuntime }
}).WithLabels(version, os, osVersion, runtimeVersion)
.Set(1);

Log.Information("Api Version: {@Version}", version);
Log.Information("Operating System: {@Os}", osVersion);
Log.Information("DotNet Runtime: {@DotnetRuntime}", runtimeVersion);

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

app.Use((context, next) =>
{
	return next.Invoke();
});

if (config.Observability.Prometheus.Enabled)
{
	Log.Information("Metrics Enabled");
	Common.Observe.Metrics.EnableCollector(config.Observability.Prometheus);

	app.MapMetrics();
	app.UseHttpMetrics();
}

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();