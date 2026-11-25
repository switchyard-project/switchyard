using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Switchyard.Tests;

internal sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath;

    public TestApiFactory(string databasePath)
    {
        _databasePath = databasePath;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:Path", _databasePath);
    }
}
