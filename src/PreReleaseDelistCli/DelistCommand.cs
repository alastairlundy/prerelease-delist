using Microsoft.Extensions.Configuration;
using NuGet.Versioning;
using PreReleaseDelistCli.Localizations;
using PreReleaseDelistLib;

namespace PreReleaseDelistCli;

public class DelistCommand
{
    [Command("")]
    public async Task<int> RunAsync(
        [FromServices] IConfiguration configuration,
        [FromServices] IPackageDelistService packageDelistService,
        string packageId,
        CancellationToken cancellationToken,
        bool delistAllVersions = false,
        bool useStrictParsing = true,
        [Argument] params string[] versions)
    {
        ArgumentException.ThrowIfNullOrEmpty(packageId);

        string nugetApiUrl = configuration["NuGetServer:ApiBaseUrl"] ?? throw new 
            ArgumentNullException(Resources.Exceptions_Configuration_NugetApiUrl);
        string nugetApiKey = configuration["NuGetServer:ApiKey"] ?? throw new 
            ArgumentNullException(Resources.Exceptions_Configuration_NugetApiKey);
        
        ArgumentException.ThrowIfNullOrEmpty(nugetApiUrl);
        ArgumentException.ThrowIfNullOrEmpty(nugetApiKey);

        (NuGetVersion version, bool isDelisted, string responseMessage)[] results; 
        
        if (delistAllVersions)
        {
            results = await packageDelistService.RequestPackageDelistAsync(nugetApiUrl, nugetApiKey, packageId, cancellationToken);
        }
        else
        {
            NuGetVersion[] parsedVersions = ParseVersions(versions, useStrictParsing);
            
            results = await packageDelistService.RequestPackageDelistAsync(nugetApiUrl, nugetApiKey, packageId, cancellationToken,
                    parsedVersions)
                .ToArrayAsync(cancellationToken);    
        }

        await Console.Out.WriteLineAsync($"Versions Delisted for Package: {packageId}");

        IEnumerable<(NuGetVersion version, bool isDelisted, string responseMessage)> delistedVersions = results
            .Where(x => x.isDelisted);

        int delistedVersionsCount = 0;
        foreach ((NuGetVersion version, bool isDelisted, string responseMessage) result in delistedVersions)
        {
            await Console.Out.WriteLineAsync($"{result.version.ToFullString()}");
            delistedVersionsCount++;
        }

        await Console.Out.WriteLineAsync();

        int exitCode = 0;
        
        if (delistedVersionsCount < results.Length)
        {
            await Console.Out.WriteLineAsync($"The following versions of {packageId} could not be delisted:");

            foreach ((NuGetVersion version, bool isDelisted, string responseMessage) result in 
                     results.Where(x => !x.isDelisted))
            {
                await Console.Out.WriteLineAsync($"{result.version.ToFullString()} - With Reason: {result.responseMessage}");
            }

            exitCode = 1;
        }
        
        return exitCode;
    }
    
    private NuGetVersion[] ParseVersions(string[] versions, bool throwOnError)
    {
        List<NuGetVersion> output = new(capacity: versions.Length);

        foreach (string versionString in versions)
        {
            bool success = NuGetVersion.TryParse(versionString, out NuGetVersion? version);

            if (success && version is not null)
            {
                output.Add(version);
            }
            else
            {
                if(throwOnError)
                    throw new ArgumentException("Invalid version string: " + versionString);
            }
        }
        
        return output.ToArray();
    }
}