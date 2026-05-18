using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using TravelInspiration.API.Itineraries.DbContexts;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((appBuilder, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddSingleton<TokenCredential>(new DefaultAzureCredential());

        services.AddDbContext<TravelInspirationDbContext>((sp, options) =>
        {
            var credential = sp.GetRequiredService<TokenCredential>();
            options.UseSqlServer(
                appBuilder.Configuration.GetConnectionString("TravelInspirationDbConnection"),
                sql => sql.EnableRetryOnFailure());
        });
    })
    .Build();

host.Run();
