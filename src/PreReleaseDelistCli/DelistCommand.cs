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
        [FromServices] IPackageVersionService packageVersionService,
        string packageId,
        CancellationToken cancellationToken,
        bool delistAllVersions = false,
        bool useStrictParsing = true,
        [Argument] params string[] versions)
    {
        ArgumentException.ThrowIfNullOrEmpty(packageId);

        string nugetApiUrl = configuration["NuGetApiUrl"] ?? throw new ArgumentNullException(Resources.Exceptions_Configuration_NugetApiUrl);
        string nugetApiKey = configuration["NuGetApiKey"] ?? throw new ArgumentNullException(Resources.Exceptions_Configuration_NugetApiKey);
        
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

        foreach ((NuGetVersion version, bool isDelisted, string responseMessage) result in results)
        {
            result.version.
        }

        return 0;
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