using Common;
using WebUI;
using Serilog;
using Serilog.Events;
using Prometheus;
using Common.Stateful;
using SharedUI;

///////////////////////////////////////////////////////////
/// STATICS
///////////////////////////////////////////////////////////
Statics.AppType = Constants.WebUIName;
Statics.MetricPrefix = Constants.WebUIName;
Statics.TracingService = Constants.WebUIName;
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

builder.WebHost.UseUrls(config.WebUI.HostUrl);

///////////////////////////////////////////////////////////
/// SERVICES
///////////////////////////////////////////////////////////

builder.Services.AddScoped<IApiClient>(sp => new ApiClient(config.Api.HostUrl));
builder.Services.ConfigureSharedUIServices();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

ObservabilityStartup.ConfigureWebUI(builder.Services, builder.Configuration, config);
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

app.Use((context, next) =>
{
	return next.Invoke();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();