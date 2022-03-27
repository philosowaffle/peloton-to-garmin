using Common;
using Havit.Blazor.Components.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebUI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

var apiSettings = new ApiSettings();
builder.Configuration.GetSection("Api").Bind(apiSettings);

var webUISettings = new WebUISettings();
builder.Configuration.GetSection("WebUI").Bind(webUISettings);

builder.RootComponents.Add<WebUI.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<IApiClient>(sp => new ApiClient(apiSettings.HostUrl));
builder.Services.AddHxServices();

await builder.Build().RunAsync();