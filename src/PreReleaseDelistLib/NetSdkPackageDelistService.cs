using System.Runtime.CompilerServices;
using CliInvoke.Core;
using CliInvoke.Core.Factories;
using EnhancedLinq.Deferred;

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
        NuGetVersion[] versionToDelist = await _packageVersionService.GetAllPackageVersionsAsync
            (nugetApiUrl, nugetApiKey, packageId, cancellationToken);
        
        IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)> delistResults =
            RequestPackageDelistAsync(nugetApiUrl, nugetApiKey, packageId, cancellationToken, versionToDelist
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
        
        IDictionary<NuGetVersion, bool> checkVersionsForDelist = await _packageVersionService.CheckPackageVersionsListedAsync(nugetApiUrl, nugetApiKey, packageId,
            true, versions, cancellationToken);

        NuGetVersion[] alreadyDelistedVersions = checkVersionsForDelist.Where(kvp => !kvp.Value).Select(kvp => kvp.Key)
            .ToArray();
        
        NuGetVersion[] versionsToDelist = versions.Exclude(alreadyDelistedVersions)
            .ToArray();
        
        foreach (NuGetVersion version in alreadyDelistedVersions)
        {
            yield return new ValueTuple<NuGetVersion, bool, string>(version, false, Resources.Info_Package_AlreadyDelisted);
        }
        
        foreach (NuGetVersion version in versionsToDelist)
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