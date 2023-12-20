using simple_budget.bff.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureServices(builder.Configuration);
builder.Host.ConfigureHost();

var app = builder.Build();
app.ConfigurePipeline();
try
{
    app.Run();    
}
catch (Exception)
{    
    throw;
}