using DeviceManagement.Api.Infrastructure.Repositories;
using DeviceManagement.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeviceManagement.Api.IntegrationTests;

public sealed class AuthenticatedApiApplicationFactory : WebApplicationFactory<Program>
{
    private readonly CompactTestStore _store = new();

    public void ResetState() => _store.Reset();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAccountRepository>();
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IDeviceRepository>();
            services.RemoveAll<IDeviceDescriptionGenerator>();

            services.AddSingleton(_store);
            services.AddScoped<IAccountRepository, CompactAccountRepository>();
            services.AddScoped<IUserRepository, CompactUserRepository>();
            services.AddScoped<IDeviceRepository, CompactDeviceRepository>();
            services.AddScoped<IDeviceDescriptionGenerator, CompactDescriptionGenerator>();
        });
    }
}
