using System.Runtime.CompilerServices;
using CliInvoke.Core;
using CliInvoke.Core.Factories;

namespace PreReleaseDelistLib;

/// <summary>
/// This class is an alternative implementation for the <see cref="PackageDelistService"/> that uses .NET SDK CLI's nuget subcommand.
/// </summary>
public class NetSdkPackageDelistService : IPackageDelistService
{
    private readonly IPackageVersionService _packageVersionService;
    private readonly IPackageAvailabilityDetector _packageAvailabilityDetector;
    private readonly IProcessConfigurationFactory _processConfigurationFactory;
    private readonly IProcessInvoker _processInvoker;

    public NetSdkPackageDelistService(IPackageVersionService packageVersionService,
        IPackageAvailabilityDetector packageAvailabilityDetector,
        IProcessConfigurationFactory processConfigurationFactory,
        IProcessInvoker processInvoker)
    {
        _packageVersionService = packageVersionService;
        _packageAvailabilityDetector = packageAvailabilityDetector;
        _processConfigurationFactory = processConfigurationFactory;
        _processInvoker = processInvoker;
    }
    
    public async Task<(NuGetVersion version, bool delistSuccess, string responseMessage)[]> RequestPackageDelistAsync(string nugetApiUrl, string nugetApiKey, 
        string packageId, CancellationToken cancellationToken)
    {
        PackageVersionListingInfo[] versionToDelist = await _packageVersionService.GetAllPackageVersionsAsync
            (nugetApiUrl, nugetApiKey, packageId, true, cancellationToken);
        
        IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)> delistResults = RequestPackageDelistAsync(nugetApiUrl, nugetApiKey, packageId, cancellationToken, versionToDelist.Select(v => v.PackageVersion)
            .ToArray());

        return await delistResults.ToArrayAsync(cancellationToken: cancellationToken);
    }

    public async IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)> RequestPackageDelistAsync(string nugetApiUrl, string nugetApiKey, string packageId,
        [EnumeratorCancellation] CancellationToken cancellationToken, params NuGetVersion[] versions)
    {
        ArgumentException.ThrowIfNullOrEmpty(nugetApiKey);
        ArgumentException.ThrowIfNullOrEmpty(packageId);
        ArgumentNullException.ThrowIfNull(versions);
        
        bool doesPackageExists = await _packageAvailabilityDetector.CheckPackageExistsAsync(nugetApiUrl, packageId, cancellationToken);

        if(!doesPackageExists)
            throw new ArgumentException(string.Format(Resources.Exceptions_Package_NotFoundOnServer, packageId, nugetApiUrl));
        
        PackageVersionListingInfo[] allVersions = await _packageVersionService.GetAllPackageVersionsAsync(nugetApiUrl, nugetApiKey, packageId, true, cancellationToken);

        IEnumerable<NuGetVersion> alreadyDelistedVersions = allVersions.Where(v => !v.IsListed)
            .Select(v => v.PackageVersion)
            .Where(v => versions.Contains(v));

        foreach (var version in alreadyDelistedVersions)
        {
            yield return new ValueTuple<NuGetVersion, bool, string>(version, false, Resources.Info_Package_AlreadyDelisted);
        }

        IEnumerable<NuGetVersion> allPackageVersions = allVersions
            .Where(v => v.IsListed)
            .Select(v => v.PackageVersion);
        
        IEnumerable<NuGetVersion> versionsToDeList = versions.Where(v => allPackageVersions.Contains(v));
        
        foreach (NuGetVersion version in versionsToDeList)
        {
            using ProcessConfiguration configuration = _processConfigurationFactory.Create(OperatingSystem.IsWindows()
                    ? "dotnet.exe" : "dotnet", 
                $"nuget delete {packageId.ToLowerInvariant()} {version.ToNormalizedString()} --api-key {nugetApiKey} --source {nugetApiUrl} --non-interactive");
            
            BufferedProcessResult result = await _processInvoker.ExecuteBufferedAsync(configuration, ProcessExitConfiguration.DefaultNoException,
                false, cancellationToken);

            if (result.StandardOutput.ToLower().EndsWith("was deleted successfully"))
            {
                yield return new ValueTuple<NuGetVersion, bool, string>(version, true, "");
            }
            else
            {
                yield return new ValueTuple<NuGetVersion, bool, string>(version, false, result.StandardOutput);
            }
        }
    }
}