using EnhancedLinq.Deferred;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
﻿/*
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
    public PackageVersionService()
    {
        _cacheContext = new SourceCacheContext()
        {
            MaxAge = DateTimeOffset.Now + TimeSpan.FromMinutes(3),
            NoCache = false,
        };
    }

    private readonly SourceCacheContext _cacheContext;

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
    /// Retrieves all available versions of a NuGet package from a specified repository.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API.</param>
    /// <param name="nugetApiKey">The API key for authentication against the NuGet API.</param>
    /// <param name="packageId">The identifier of the package to retrieve versions for.</param>
    /// <param name="excludeUnlistedVersions">Indicates whether to exclude unlisted versions from the result.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An array of NuGet version strings matching the specified criteria.</returns>
    public async Task<NuGetVersion[]> GetAllPackageVersionsAsync(string nugetApiUrl, string nugetApiKey,
        string packageId, bool excludeUnlistedVersions,
        CancellationToken cancellationToken)
    {
        SourceRepository repoInfo = GetRepoInfo(nugetApiUrl);

        FindPackageByIdResource resource = await repoInfo.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        
        IEnumerable<NuGetVersion>? allPackageVersions =
            await resource.GetAllVersionsAsync(packageId, _cacheContext, NullLogger.Instance,
                cancellationToken);
        
        if (allPackageVersions is null)
            return [];
        
        NuGetVersion[] allPackageVersionsArray = allPackageVersions.ToArray();

        if (!excludeUnlistedVersions)
            return allPackageVersionsArray;
        
        IEnumerable<NuGetVersion> delistedVersions = await GetDelistedPackageVersionsAsync(nugetApiUrl, nugetApiKey, packageId, cancellationToken);
        
        return allPackageVersionsArray.Exclude(delistedVersions).ToArray();
    }

    /// <summary>
    /// Retrieves a list of delisted package versions from a NuGet repository for a specified package.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API.</param>
    /// <param name="nugetApiKey">The API key for authentication against the NuGet API.</param>
    /// <param name="packageId">The identifier of the package to retrieve versions for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An array of delisted NuGet version strings matching the specified criteria.</returns>
    public async Task<NuGetVersion[]> GetDelistedPackageVersionsAsync(string nugetApiUrl, string nugetApiKey,
        CancellationToken cancellationToken)
    {
        SourceRepository repoInfo = GetRepoInfo(nugetApiUrl);
        
        PackageSearchResource searchResource =
            await repoInfo.GetResourceAsync<PackageSearchResource>(cancellationToken);

        SearchFilter searchFilter = new(true, SearchFilterType.IsAbsoluteLatestVersion);

        IEnumerable<IPackageSearchMetadata> packages = await searchResource.SearchAsync($"{packageId}", searchFilter, 0, 10000,
            NullLogger.Instance, cancellationToken);

        IPackageSearchMetadata? package = packages.FirstOrDefault(p => p.IsListed && p.Identity.Id == packageId);

        if (package is null)
            return [];

        IEnumerable<VersionInfo> actualVersions = await package.GetVersionsAsync();
        
        return actualVersions
            .Where(vInfo => !vInfo.PackageSearchMetadata.IsListed)
            .Select(v => v.Version)
            .ToArray();
    }

    private SourceRepository GetRepoInfo(string nugetApiUrl) => Repository.Factory.GetCoreV3(nugetApiUrl);
}