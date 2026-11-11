using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AMZN.Tests;

// Настоящий Kestrel на случайном localhost-порту: нужен там, где TestServer не проверяет сетевые ограничения.
internal sealed class LocalHttpTestHost : IAsyncDisposable
{
    private readonly IWebHost _host;
    public HttpClient Client { get; }

    private LocalHttpTestHost(IWebHost host)
    {
        _host = host;
        string address = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        Client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(15) };
    }

    public static async Task<LocalHttpTestHost> StartAsync(
        Action<IServiceCollection> configureServices, Action<IApplicationBuilder> configureApp)
    {
        var host = new WebHostBuilder()
            .UseSetting(WebHostDefaults.ApplicationKey, typeof(LocalHttpTestHost).Assembly.GetName().Name)
            .UseKestrel(options => options.Listen(IPAddress.Loopback, 0))
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureServices(configureServices)
            .Configure(configureApp)
            .Build();
        try
        {
            await host.StartAsync();
            return new LocalHttpTestHost(host);
        }
        catch { host.Dispose(); throw; }
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }
}
