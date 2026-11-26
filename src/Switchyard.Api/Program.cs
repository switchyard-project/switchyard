using Switchyard.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<RobotRepository>(services =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var environment = services.GetRequiredService<IHostEnvironment>();
    var configuredPath = configuration["Database:Path"] ?? "data/switchyard.db";
    var databasePath = Path.GetFullPath(configuredPath, environment.ContentRootPath);
    return new RobotRepository(databasePath);
});

var app = builder.Build();

app.Services.GetRequiredService<RobotRepository>().Initialize();
app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.Run();

// Permite arrancar esta misma aplicación desde las pruebas de integración.
public partial class Program;
