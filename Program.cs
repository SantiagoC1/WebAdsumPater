using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AdsumPater;
using AdsumPater.Services;   // ← IMPORTANTE

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient global
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }
);
builder.Services.AddScoped<LocalStorageService>();


builder.Services.AddScoped<FirebaseService>();
builder.Logging.SetMinimumLevel(LogLevel.Debug);



await builder.Build().RunAsync();
