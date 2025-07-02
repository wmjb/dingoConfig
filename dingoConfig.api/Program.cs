using dingoConfig.Persistence.Interfaces;
using dingoConfig.Persistence.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/dingoConfig-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register catalog services
builder.Services.AddSingleton<DeviceCatalogLoader>();
builder.Services.AddSingleton<CatalogValidator>();
builder.Services.AddSingleton<IDeviceCatalogService, DeviceCatalogService>();

// Configure CORS for future frontend development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "dingoConfig API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Load catalogs at startup
var catalogService = app.Services.GetRequiredService<IDeviceCatalogService>();
var catalogDirectory = builder.Configuration.GetValue<string>("CatalogDirectory") ?? "catalogs";

try
{
    await catalogService.LoadCatalogsAsync(catalogDirectory);
    Log.Information("Application started successfully with {CatalogCount} catalogs loaded", 
        catalogService.GetAvailableDeviceTypes().Count());
}
catch (Exception ex)
{
    Log.Warning(ex, "Failed to load catalogs from {CatalogDirectory} at startup", catalogDirectory);
}

app.Run();