/*
    PreReleaseDelistLib
    Copyright (C) 2026 Alastair Lundy

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
     any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

namespace PreReleaseDelistLib;

/// <summary>
/// This class provides functionality to retrieve package versions from a NuGet repository.
/// </summary>
public class PackageVersionService : IPackageVersionService
{
    private readonly IPackageAvailabilityDetector _packageAvailabilityDetector;

    public PackageVersionService(IPackageAvailabilityDetector packageAvailabilityDetector)
    {
        _packageAvailabilityDetector = packageAvailabilityDetector;
        _cacheContext = new SourceCacheContext()
        {
            DirectDownload = false,
            MaxAge = DateTimeOffset.UtcNow.AddMinutes(10)
        };
    }

    private readonly SourceCacheContext _cacheContext;

    /// <summary>
    /// Enumerates prerelease package versions from a NuGet repository.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API.</param>
    /// <param name="nugetApiKey">The API key for authentication against the NuGet API.</param>
    /// <param name="packageId">The identifier of the package to retrieve versions for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An asynchronous sequence of prerelease <see cref="NuGetVersion"/> objects matching the specified criteria.</returns>
    public async IAsyncEnumerable<NuGetVersion> EnumeratePrereleasePackageVersionsAsync(string nugetApiUrl,
        string nugetApiKey, string packageId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        SourceRepository repoInfo = GetRepoInfo(nugetApiUrl);
        
        FindPackageByIdResource resource = await repoInfo.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        IEnumerable<NuGetVersion>? allPackageVersions =
            await resource.GetAllVersionsAsync(packageId, _cacheContext, NullLogger.Instance,
                cancellationToken);

        if (allPackageVersions is null)
        {
            yield break;
        }

        foreach (NuGetVersion version in allPackageVersions.Where(v => v.IsPrerelease || v.Major == 0))
        {
            yield return version;
        }
    }

    /// <summary>
    /// Retrieves a list of prerelease package versions from a NuGet repository.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API.</param>
    /// <param name="nugetApiKey">The API key for authentication against the NuGet API.</param>
    /// <param name="packageId">The identifier of the package to retrieve versions for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An array of prerelease NuGet version strings matching the specified criteria.</returns>
    public async Task<NuGetVersion[]> GetPrereleasePackageVersionsAsync(string nugetApiUrl, string nugetApiKey,
        string packageId,
        CancellationToken cancellationToken)
    {
        SourceRepository repoInfo = GetRepoInfo(nugetApiUrl);
        
        FindPackageByIdResource resource = await repoInfo.GetResourceAsync<FindPackageByIdResource>(cancellationToken);

        IEnumerable<NuGetVersion>? allPackageVersions =
            await resource.GetAllVersionsAsync(packageId, _cacheContext, NullLogger.Instance,
                cancellationToken);

        if (allPackageVersions is null)
            return [];
        
        return allPackageVersions.Where(v => v.IsPrerelease || v.Major == 0)
            .ToArray();
    }

    /// <summary>
    /// Enumerates all package versions from a NuGet repository.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API.</param>
    /// <param name="nugetApiKey">The API key for authentication against the NuGet API.</param>
    /// <param name="packageId">The identifier of the package to retrieve versions for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An asynchronous sequence of all <see cref="NuGetVersion"/> objects matching the specified criteria.</returns>
    public async IAsyncEnumerable<PackageVersionListingInfo> EnumerateAllPackageVersionsAsync(string nugetApiUrl,
        string nugetApiKey, string packageId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        SourceRepository repoInfo = GetRepoInfo(nugetApiUrl);

        bool packageExists = await _packageAvailabilityDetector.CheckPackageExistsAsync(nugetApiUrl, packageId, cancellationToken);

        if (!packageExists)
            throw new ArgumentException($"Package with Id of '{packageId}' does not exist.");
        
        FindPackageByIdResource resource = await repoInfo.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        
        IEnumerable<NuGetVersion>? allPackageVersions =
            await resource.GetAllVersionsAsync(packageId, _cacheContext, NullLogger.Instance,
                cancellationToken);
        
        if (allPackageVersions is null)
            yield break;

        PackageVersionListingInfo[] allPackageVersionsArray = allPackageVersions
            .Select(v => new PackageVersionListingInfo
            {
                IsListed = true,
                PackageVersion = v,
                PackageVersionExists = true
            })
            .ToArray();

        foreach (PackageVersionListingInfo versionListingInfo in allPackageVersionsArray)
        {
            yield return versionListingInfo;
        }
    }

    /// <summary>
    /// Retrieves all available versions of a NuGet package from a specified repository.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API.</param>
    /// <param name="nugetApiKey">The API key for authentication against the NuGet API.</param>
    /// <param name="packageId">The identifier of the package to retrieve versions for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An array of NuGet version strings matching the specified criteria.</returns>
    public async Task<NuGetVersion[]> GetAllPackageVersionsAsync(string nugetApiUrl, string nugetApiKey,
        string packageId, CancellationToken cancellationToken)
    {
        SourceRepository repoInfo = GetRepoInfo(nugetApiUrl);

        bool packageExists = await _packageAvailabilityDetector.CheckPackageExistsAsync(nugetApiUrl, packageId, cancellationToken);

        if (!packageExists)
            throw new ArgumentException($"Package with Id of '{packageId}' does not exist.");
        
        FindPackageByIdResource resource = await repoInfo.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        
        IEnumerable<NuGetVersion>? allPackageVersions =
            await resource.GetAllVersionsAsync(packageId, _cacheContext, NullLogger.Instance,
                cancellationToken);
        
        if (allPackageVersions is null)
            return [];

        return allPackageVersions.ToArray();
    }

    /// <summary>
    /// Determines whether a specific package version has been delisted from the NuGet repository.
    /// </summary>
    /// <remarks>This method does not perform a comprehensive check and can report false negatives.</remarks>
    /// <param name="nugetApiUrl">The URL of the NuGet API.</param>
    /// <param name="nugetApiKey">The API key for authentication against the NuGet API.</param>
    /// <param name="packageId">The identifier of the package to check for delisting status.</param>
    /// <param name="includePreReleaseVersions">Whether to include pre-release versions in the search results.</param>
    /// <param name="packageVersion">The specific version of the package to verify against.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A boolean value indicating whether the specified package version is delisted. Returns true if delisted, false otherwise.</returns>
    public async Task<bool> IsPackageVersionDelistedAsync(string nugetApiUrl, string nugetApiKey, string packageId,
        bool includePreReleaseVersions, NuGetVersion packageVersion, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(packageId);
        ArgumentException.ThrowIfNullOrEmpty(nugetApiUrl);
        ArgumentException.ThrowIfNullOrEmpty(nugetApiKey);
        
        SourceRepository repoInfo = GetRepoInfo(nugetApiUrl);

        PackageSearchResourceV3? searchResource =
            await repoInfo.GetResourceAsync<PackageSearchResourceV3>(cancellationToken);
        
        IEnumerable<IPackageSearchMetadata> results = await searchResource.SearchAsync(packageId, 
            new SearchFilter(includePreReleaseVersions), 0, 1000, NullLogger.Instance, cancellationToken);
        
        return !results.Any(r => r.Identity.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase)
                                 && r.Identity.Version == packageVersion);
    }

    /// <summary>
    /// Checks if a specific list of packages is listed in the repository.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API.</param>
    /// <param name="nugetApiKey">The API key for authentication against the NuGet API.</param>
    /// <param name="packageId">The identifier of the package to check if it is listed.</param>
    /// <param name="includePreReleaseVersions">Whether to include pre-release versions in the search results.</param>
    /// <param name="packageVersions">The specific versions of the package to verify against.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An asynchronous sequence indicating whether the specified package is listed in the repository.</returns>
    public async Task<IDictionary<NuGetVersion, bool>> CheckPackageVersionsListedAsync(string nugetApiUrl,
        string nugetApiKey, string packageId,
        bool includePreReleaseVersions,
        IList<NuGetVersion> packageVersions, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(packageId);
        ArgumentException.ThrowIfNullOrEmpty(nugetApiUrl);
        ArgumentException.ThrowIfNullOrEmpty(nugetApiKey);

        SourceRepository repoInfo = GetRepoInfo(nugetApiUrl);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(packageVersions.Count, 1000);
        
        PackageSearchResourceV3? searchResource =
            await repoInfo.GetResourceAsync<PackageSearchResourceV3>(cancellationToken);
        
        IEnumerable<IPackageSearchMetadata> results = await searchResource.SearchAsync(packageId, 
            new SearchFilter(includePreReleaseVersions), 0, 1000, NullLogger.Instance, cancellationToken);
        
        Dictionary<NuGetVersion, bool> output = new Dictionary<NuGetVersion, bool>(capacity: packageVersions.Count);

        foreach (NuGetVersion version in packageVersions)
        {
            output.Add(version, false);
        }
        
        NuGetVersion[] allPackageVersions = await GetAllPackageVersionsAsync(nugetApiUrl, nugetApiKey, packageId, cancellationToken);
        
        foreach (IPackageSearchMetadata result in results
                     .Where(r => r.Identity.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase)))
        {
            if (packageVersions.Contains(result.Identity.Version) && allPackageVersions.Contains(result.Identity.Version))
            {
                output[result.Identity.Version] = true;
            }
        }
       
        return output;
    }

    private SourceRepository GetRepoInfo(string nugetApiUrl) => Repository.Factory.GetCoreV3(nugetApiUrl);
}
