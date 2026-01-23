using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

ConsoleApp.ConsoleAppBuilder app = ConsoleApp
    .Create()
    .ConfigureServices(services =>
    {
        services.AddHttpClient();
        services.AddSingleton<IPackageVersionService, PackageVersionService>();
        services.AddSingleton<IPackageDelistService, PackageDelistService>();

        IConfigurationBuilder configurationBuilder;

        // Fallback to avoid using AppSettings.Json if it is not present.
        try
        {
            configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables("PreReleaseDelistCLI_");
        }
        catch
        {
            configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables("PreReleaseDelistCLI_");
        }
        
        IConfiguration configuration = configurationBuilder.Build();
        
        services.AddSingleton(configuration);
    });

app.Add<DelistCommand>();

await app.RunAsync(args);
