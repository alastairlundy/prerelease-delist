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

namespace PreReleaseDelistLib.Abstractions;

/// <summary>
/// Provides methods for retrieving and managing NuGet package versions.
/// </summary>
public interface IPackageVersionService
{
    /// <summary>
    /// Retrieves a list of prerelease package versions from a NuGet repository.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API.</param>
    /// <param name="nugetApiKey">The API key for authentication against the NuGet API.</param>
    /// <param name="packageId">The identifier of the package to retrieve versions for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An asynchronous sequence of prerelease <see cref="NuGetVersion"/> objects matching the specified criteria.</returns>
    Task<NuGetVersion[]> GetPrereleasePackageVersionsAsync(string nugetApiUrl, string nugetApiKey, string packageId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all available versions of a NuGet package from the specified repository, optionally excluding unlisted versions.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API.</param>
    /// <param name="nugetApiKey">The API key for authentication against the NuGet API.</param>
    /// <param name="packageId">The identifier of the package to retrieve versions for.</param>
    /// <param name="excludeUnlistedVersions">Indicates whether to exclude unlisted versions from the result. If false, all available versions are returned.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An array of <see cref="NuGetVersion"/> objects matching the specified criteria.</returns>
    Task<PackageVersionListingInfo[]> GetAllPackageVersionsAsync(string nugetApiUrl, string nugetApiKey,
        string packageId,
        bool excludeUnlistedVersions,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a list of package versions that have been delisted from a NuGet repository.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API.</param>
    /// <param name="nugetApiKey">The API key for authentication against the NuGet API.</param>
    /// <param name="packageId">The identifier of the package to retrieve delisted versions for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An array of delisted <see cref="NuGetVersion"/> objects matching the specified criteria.</returns>
    Task<PackageVersionListingInfo[]> GetDelistedPackageVersionsAsync(string nugetApiUrl, string nugetApiKey,
        string packageId,
        CancellationToken cancellationToken);
}