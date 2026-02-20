using System.Text.Json;
using EnhancedLinq.Deferred;

namespace PreReleaseDelistLib.Detectors;

public class PackageAvailabilityDetector : IPackageAvailabilityDetector
{
    private readonly SourceCacheContext _cacheContext;
    
    public PackageAvailabilityDetector()
    {
        _cacheContext = new()
        {
            NoCache = false,
            MaxAge = DateTime.Now + TimeSpan.FromMinutes(5)
        };
    }
    
    public async Task<bool> CheckPackageExistsAsync(string nugetApiUrl, string packageId, CancellationToken cancellationToken)
    {
        SourceRepository repository = Repository.Factory.GetCoreV3(nugetApiUrl);

        try
        {
            FindPackageByIdResource? searchResource =
                await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
            
            IEnumerable<NuGetVersion>? packageVersions = await searchResource.GetAllVersionsAsync(packageId,
                _cacheContext,
                NullLogger.Instance, cancellationToken);

            return packageVersions is not null && packageVersions.Any();
        }
        catch(ArgumentException)
        {
            return false;
        }
    }
}