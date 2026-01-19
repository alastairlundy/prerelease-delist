using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace PreReleaseDelistLib;

public class PackageVersionService : IPackageVersionService
{
    public PackageVersionService()
    {
        _cacheContext = new SourceCacheContext()
        {
            MaxAge = DateTimeOffset.Now + TimeSpan.FromMinutes(3),
            NoCache = false,
        };
    }

    private readonly SourceCacheContext _cacheContext;
    
    public async Task<NuGetVersion[]> GetPrereleasePackageVersionsAsync(string nugetApiUrl, string nugetApiKey, string packageId,
        CancellationToken cancellationToken)
    {
        var repoInfo = GetRepoInfo(nugetApiUrl);
        
        FindPackageByIdResource resource = await repoInfo.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        IEnumerable<NuGetVersion>? allPackageVersions =
            await resource.GetAllVersionsAsync(packageId, _cacheContext, NullLogger.Instance,
                cancellationToken);

        if (allPackageVersions is null)
            return [];
        
        return allPackageVersions.Where(v => v.IsPrerelease || v.Major == 0)
            .ToArray();
    }

    public async Task<NuGetVersion[]> GetAllPackageVersionsAsync(string nugetApiUrl, string nugetApiKey, string packageId,
        CancellationToken cancellationToken)
    {
        var repoInfo = GetRepoInfo(nugetApiUrl);
        
        FindPackageByIdResource resource = await repoInfo.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        IEnumerable<NuGetVersion>? allPackageVersions =
            await resource.GetAllVersionsAsync(packageId, _cacheContext, NullLogger.Instance,
                cancellationToken);

        return allPackageVersions is null ? [] : allPackageVersions.ToArray();
    }

    private SourceRepository GetRepoInfo(string nugetApiUrl) => Repository.Factory.GetCoreV3(nugetApiUrl);
}