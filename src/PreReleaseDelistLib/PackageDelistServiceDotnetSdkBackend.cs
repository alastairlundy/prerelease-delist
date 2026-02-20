using CliInvoke.Core;
using CliInvoke.Core.Factories;

namespace PreReleaseDelistLib;

public class PackageDelistServiceDotnetSdkBackend : IPackageDelistService
{
    private readonly IPackageVersionService _packageVersionService;
    private readonly IPackageAvailabilityDetector _packageAvailabilityDetector;
    private readonly IProcessConfigurationFactory _processConfigurationFactory;
    private readonly IProcessInvoker _processInvoker;

    public PackageDelistServiceDotnetSdkBackend(IPackageVersionService packageVersionService,
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

    public IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)> RequestPackageDelistAsync(string nugetApiUrl, string nugetApiKey, string packageName,
        CancellationToken cancellationToken, params NuGetVersion[] version)
    {
        
    }
}