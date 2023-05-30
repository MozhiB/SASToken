
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SasTokenGeneration.Models;
using SasTokenGeneration.Services;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;

[assembly: FunctionsStartup(typeof(SasTokenGeneration.Startup))]
namespace SasTokenGeneration
{
    /// <summary>
    /// Start up class
    /// </summary>
    public class Startup : FunctionsStartup
    {

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<IGithubService, GithubService>();

            builder.Services.AddLogging();
            builder.Services.AddOptions<GithubApiSettings>().Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("GitHubApiSettings").Bind(settings);
            });
            builder.Services.AddOptions<AzApimSettings>().Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("AzApimSettings").Bind(settings);
            });
        }
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
        }

    }
}
