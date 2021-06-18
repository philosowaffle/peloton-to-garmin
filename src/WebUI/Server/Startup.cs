using Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Serilog;
using Serilog.Enrichers.Span;

namespace WebUI.Server
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton<IAppConfiguration, Configuration>();

			services.Configure<IAppConfiguration>((configOptions) => 
			{
				Configuration.GetSection(nameof(App)).Bind(configOptions.App);
				Configuration.GetSection(nameof(Format)).Bind(configOptions.Format);
				Configuration.GetSection(nameof(Peloton)).Bind(configOptions.Peloton);
				Configuration.GetSection(nameof(Garmin)).Bind(configOptions.Garmin);
				Configuration.GetSection(nameof(Observability)).Bind(configOptions.Observability);
				Configuration.GetSection(nameof(Developer)).Bind(configOptions.Developer);

				Log.Logger = new LoggerConfiguration()
					.ReadFrom.Configuration(Configuration, sectionName: $"{nameof(Observability)}:Serilog")
					.Enrich.WithSpan()
					.CreateLogger();

				// TODO: OnChange not working
				ChangeToken.OnChange(() => Configuration.GetReloadToken(), () =>
				{
					Log.Information("Config change detected, reloading config values.");
					Configuration.GetSection(nameof(App)).Bind(configOptions.App);
					Configuration.GetSection(nameof(Format)).Bind(configOptions.Format);
					Configuration.GetSection(nameof(Peloton)).Bind(configOptions.Peloton);
					Configuration.GetSection(nameof(Garmin)).Bind(configOptions.Garmin);
					Configuration.GetSection(nameof(Developer)).Bind(configOptions.Developer);

					Log.Information("Config reloaded. Changes will take effect at the end of the current sleeping cycle.");
				});
			});
			services.AddControllersWithViews();
			services.AddRazorPages();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseWebAssemblyDebugging();
			}
			else
			{
				app.UseExceptionHandler("/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseSerilogRequestLogging();

			app.UseHttpsRedirection();
			app.UseBlazorFrameworkFiles();
			app.UseStaticFiles();

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapRazorPages();
				endpoints.MapControllers();
				endpoints.MapFallbackToFile("index.html");
			});
		}
	}
}
