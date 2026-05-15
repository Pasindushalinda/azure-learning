using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Common;
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
                sql => sql.EnableRetryOnFailure())
                .AddInterceptors(new SqlTokenInterceptor(credential));
        });
    })
    .Build();

host.Run();

sealed class SqlTokenInterceptor(TokenCredential credential) : DbConnectionInterceptor
{
    private static readonly string[] Scopes = ["https://database.windows.net/.default"];

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        ((SqlConnection)connection).AccessToken = (await credential.GetTokenAsync(
            new TokenRequestContext(Scopes), cancellationToken)).Token;
        return result;
    }
}
