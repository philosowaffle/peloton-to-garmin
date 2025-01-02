using Api.Services;
using Common;
using Common.Database;
using Common.Stateful;
using Prometheus;
using Serilog;
using Serilog.Events;
using SharedStartup;
using System.Reflection;

///////////////////////////////////////////////////////////
/// STATICS
///////////////////////////////////////////////////////////
Statics.AppType = Constants.ApiName;
Statics.MetricPrefix = Constants.ApiName;
Statics.TracingService = Constants.ApiName;
Statics.ConfigPath = Path.Join(Statics.DefaultConfigDirectory, "configuration.local.json");

///////////////////////////////////////////////////////////
/// HOST
///////////////////////////////////////////////////////////
var builder = WebApplication.CreateBuilder(args);

var configProvider = builder.Configuration.AddJsonFile(Statics.ConfigPath, optional: true, reloadOnChange: true)
				.AddEnvironmentVariables(prefix: $"{Constants.EnvironmentVariablePrefix}_")
				.AddCommandLine(args);

var config = new AppConfiguration();
ConfigurationSetup.LoadConfigValues(builder.Configuration, config);

builder.WebHost.UseUrls(config.Api.HostUrl);

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

builder.Services.ConfigureP2GApiServices();

ObservabilityStartup.ConfigureApi(builder.Services, builder.Configuration, config);
Common.Observe.Metrics.CreateAppInfo();

///////////////////////////////////////////////////////////
/// APP
///////////////////////////////////////////////////////////

var app = builder.Build();

if (Log.IsEnabled(LogEventLevel.Verbose))
	app.UseSerilogRequestLogging();

if (config.Observability.Prometheus.Enabled)
{
	Log.Information("Metrics Enabled");
	Common.Observe.Metrics.EnableCollector(config.Observability.Prometheus);

	app.MapMetrics();
	app.UseHttpMetrics();
}

app.UseCors(options =>
{
	options
	.SetIsOriginAllowed((_) => true)
	.AllowAnyHeader();
});

app.UseSwagger();
app.UseSwaggerUI();

app.Use((context, next) =>
{
	return next.Invoke();
});


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