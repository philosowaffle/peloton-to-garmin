using Api.Services;
using Common;
using Common.Database;
using Common.Http;
using Common.Observe;
using Common.Service;
using Common.Stateful;
using Conversion;
using Garmin;
using Microsoft.Extensions.Caching.Memory;
using Peloton;
using Philosowaffle.Capability.ReleaseChecks;
using Prometheus;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Sync;
using System.Reflection;

///////////////////////////////////////////////////////////
/// STATICS
///////////////////////////////////////////////////////////
Statics.AppType = Constants.ApiName;
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
ConfigurationSetup.LoadConfigValues(builder.Configuration, config);

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
	var executingAssembly = Assembly.GetExecutingAssembly();
	var referencedAssemblies = executingAssembly.GetReferencedAssemblies();
	var docPaths = referencedAssemblies
					.Union(new AssemblyName[] { executingAssembly.GetName() })
					.Select(a => Path.Combine(AppContext.BaseDirectory, $"{a.Name}.xml"))
					.Where(f => File.Exists(f)).ToArray();
	foreach (var docPath in docPaths)
		c.IncludeXmlComments(docPath);
});

// CACHE
builder.Services.AddSingleton<IMemoryCache, MemoryCache>();

// SETTINGS
builder.Services.AddSingleton<ISettingsDb, SettingsDb>();
builder.Services.AddSingleton<ISettingsService, SettingsService>();

// IO
builder.Services.AddSingleton<IFileHandling, IOWrapper>();

// PELOTON
builder.Services.AddSingleton<IPelotonApi, Peloton.ApiClient>();
builder.Services.AddSingleton<IPelotonService, PelotonService>();

// GARMIN
builder.Services.AddSingleton<IGarminUploader, GarminUploader>();
builder.Services.AddSingleton<IGarminApiClient, Garmin.ApiClient>();

// RELEASE CHECKS
builder.Services.AddGitHubReleaseChecker();

// SYNC
builder.Services.AddSingleton<ISyncStatusDb, SyncStatusDb>();
builder.Services.AddSingleton<ISyncService, SyncService>();

// CONVERT
builder.Services.AddSingleton<IConverter, FitConverter>();
builder.Services.AddSingleton<IConverter, TcxConverter>();
builder.Services.AddSingleton<IConverter, JsonConverter>();

FlurlConfiguration.Configure(config.Observability);
Tracing.EnableApiTracing(builder.Services, config.Observability.Jaeger);

Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(builder.Configuration, sectionName: $"{nameof(Observability)}:Serilog")
				.Enrich.FromLogContext()
				.CreateLogger();

Logging.LogSystemInformation();
Common.Observe.Metrics.CreateAppInfo();

///////////////////////////////////////////////////////////
/// APP
///////////////////////////////////////////////////////////

var app = builder.Build();

// Setup initial Tracing Source
Tracing.Source = new(Statics.TracingService);

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

///////////////////////////////////////////////////////////
/// MIGRATIONS
///////////////////////////////////////////////////////////

// Migrate to Encrypted Credentials V1
var settingsDb = app.Services.GetService<ISettingsDb>();
var settings = await settingsDb!.GetSettingsAsync();

if (settings.Peloton.EncryptionVersion != EncryptionVersion.V1
	|| settings.Garmin.EncryptionVersion != EncryptionVersion.V1)
{
	try
	{
		await settingsDb.UpsertSettingsAsync(settings);
		Log.Information("Successfully encrypted Peloton and Garmin credentials.");
	} catch (Exception e)
	{
		Log.Error(e, "Failed to encrypt Peloton and Garmin credentials.");
	}	
}

///////////////////////////////////////////////////////////
/// START
///////////////////////////////////////////////////////////

await app.RunAsync();