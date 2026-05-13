using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelInspiration.API.Itineraries.DbContexts;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((appBuilder, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var connectionString = appBuilder.Configuration
            .GetConnectionString("TravelInspirationDbConnection");

        var isLocal = appBuilder.HostingEnvironment.IsDevelopment();

        if (isLocal)
        {
            // Local: directly use connection string (SQL Server / Docker)
            services.AddDbContext<TravelInspirationDbContext>(options =>
                options.UseSqlServer(connectionString,
                    sqlOptions => sqlOptions.EnableRetryOnFailure()));
        }
        else
        {
            // Azure: use Managed Identity token
            var credential = new DefaultAzureCredential();
            var accessTokenResponse = credential.GetToken(
                new Azure.Core.TokenRequestContext(
                    ["https://database.windows.net/.default"]));

            var sqlConnection = new SqlConnection(connectionString)
            {
                AccessToken = accessTokenResponse.Token
            };

            services.AddDbContext<TravelInspirationDbContext>(options =>
                options.UseSqlServer(sqlConnection,
                    sqlOptions => sqlOptions.EnableRetryOnFailure()));
        }
    })
    .Build();

host.Run();