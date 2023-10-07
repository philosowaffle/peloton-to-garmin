using Api.Services;
using Common;
using Common.Database;
using Common.Observe;
using Common.Stateful;
using Prometheus;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using SharedStartup;
using System.Reflection;

///////////////////////////////////////////////////////////
/// STATICS
///////////////////////////////////////////////////////////
Statics.AppType = Constants.ApiName;
Statics.MetricPrefix = Constants.ApiName;
Statics.TracingService = Constants.ApiName;
Statics.ConfigPath = Path.Join(Environment.CurrentDirectory, "configuration.local.json");

///////////////////////////////////////////////////////////
/// HOST
///////////////////////////////////////////////////////////
var builder = WebApplication
				.CreateBuilder(args);

var configProvider = builder.Configuration.AddJsonFile(Statics.ConfigPath, optional: true, reloadOnChange: true)
				.AddEnvironmentVariables(prefix: "P2G_")
				.AddCommandLine(args);

var config = new AppConfiguration();
ConfigurationSetup.LoadConfigValues(builder.Configuration, config);

builder.WebHost.UseUrls(config.Api.HostUrl);

builder.Host.UseSerilog((ctx, logConfig) =>
{
	logConfig
	.ReadFrom.Configuration(ctx.Configuration, new ConfigurationReaderOptions() { SectionName = $"{nameof(Observability)}:Serilog" })
	.Enrich.WithSpan()
	.Enrich.FromLogContext();
});

///////////////////////////////////////////////////////////
/// SERVICES
///////////////////////////////////////////////////////////

builder.Services.AddHostedService<BackgroundSyncJob>();

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

builder.Services.ConfigureP2GApiServices();

ObservabilityStartup.Configure(builder.Services, builder.Configuration, config);
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
var migrationService = app.Services.GetService<IDbMigrations>();
await migrationService!.PreformMigrations();

///////////////////////////////////////////////////////////
/// START
///////////////////////////////////////////////////////////

await app.RunAsync();