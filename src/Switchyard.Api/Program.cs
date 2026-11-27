using Switchyard.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(services => new RobotRepository(GetDatabasePath(services)));
builder.Services.AddSingleton(services => new TransportTaskRepository(GetDatabasePath(services)));

var app = builder.Build();

app.Services.GetRequiredService<RobotRepository>().Initialize();
app.Services.GetRequiredService<TransportTaskRepository>().Initialize();
app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.Run();

static string GetDatabasePath(IServiceProvider services)
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var environment = services.GetRequiredService<IHostEnvironment>();
    var configuredPath = configuration["Database:Path"] ?? "data/switchyard.db";
    return Path.GetFullPath(configuredPath, environment.ContentRootPath);
}

// Permite arrancar esta misma aplicación desde las pruebas de integración.
public partial class Program;
