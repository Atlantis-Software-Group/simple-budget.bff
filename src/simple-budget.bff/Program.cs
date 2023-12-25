using Serilog;
using simple_budget.bff.Utilities;

Log.Information("Starting BFF");
var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureServices(builder.Configuration);
builder.Host.ConfigureHost();

var app = builder.Build();
app.ConfigurePipeline();
try
{
    app.Run();    
}
catch (Exception ex)
{    
    Log.Fatal(ex, "Application Terminated unexpectedly");
    throw;
}
finally
{
    Log.Information("Application Terminated");
    Log.CloseAndFlush();
}