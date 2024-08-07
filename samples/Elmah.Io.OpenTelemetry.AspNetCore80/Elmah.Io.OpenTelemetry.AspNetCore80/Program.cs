#pragma warning disable S125 // Sections of code should not be commented out
using Elmah.Io.OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(options =>
{
    options.AddElmahIoExporter(options =>
    {
        options.ApiKey = "API_KEY";
        options.LogId = new Guid("LOG_ID");

        // Custom properties can be set using the OnMessage action like known from other elmah.io integrations:
        //options.OnMessage = msg => msg.Version = "42";

        // Optional application name
        //options.Application = "ASP.NET Core 8.0 Application";

        // Remove comment on the following line to log through a proxy (in this case Fiddler).
        //options.WebProxy = new WebProxy("localhost", 8888);
    });

    // Custom properties can also be set using ResourceBuilder:
    //options.SetResourceBuilder(ResourceBuilder.CreateEmpty()
    //    .AddService("Elmah.Io.OpenTelemetry.AspNetCore80")
    //    .AddAttributes(new Dictionary<string, object>
    //    {
    //        { "deployment.environment", builder.Environment.EnvironmentName }
    //    }));

    // Microsoft.Extensions.Logging scopes can be included in log messages:
    //options.IncludeScopes = true;

    // The elmah.io exporter pulls the log message already but in case there is issues getting the generated message it can be enabled like this:
    //options.IncludeFormattedMessage = true;
});
// Only log warning and more severe
builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("*", LogLevel.Warning);

var app = builder.Build();

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

app.UseAuthorization();

app.MapRazorPages();

app.Run();
#pragma warning restore S125 // Sections of code should not be commented out
