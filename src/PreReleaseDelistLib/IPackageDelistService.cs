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
/// Interface for requesting package delisting from a NuGet Server.
/// </summary>
public interface IPackageDelistService
{
    /// <summary>
    /// Requests the delisting of all versions of a NuGet package.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API endpoint to which the request will be sent.</param>
    /// <param name="nugetApiKey">The API key required for authentication with the NuGet service.</param>
    /// <param name="packageName">The name of the package from which pre-release versions are to be delisted.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to request cancellation of the operation.</param>
    /// <returns>An array of tuples containing the version being processed, a boolean indicating whether
    /// the delisting was successful, and a response message from the API.</returns>
    Task<(NuGetVersion version, bool delistSuccess, string responseMessage)[]> RequestPackageDelistAsync(
        string nugetApiUrl, string nugetApiKey, string packageName, CancellationToken cancellationToken);

    /// <summary>
    /// Requests the delisting of specified versions from a NuGet package.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API endpoint to which the request will be sent.</param>
    /// <param name="nugetApiKey">The API key required for authentication with the NuGet service.</param>
    /// <param name="packageName">The name of the package from which pre-release versions are to be delisted.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to request cancellation of the operation.</param>
    /// <param name="version">The versions of the package to delist.</param>
    /// <returns>An array of tuples containing the version being processed, a boolean indicating whether
    /// the delisting was successful, and a response message from the API.</returns>
    IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)> RequestPackageDelistAsync(
        string nugetApiUrl, string nugetApiKey, string packageName, CancellationToken cancellationToken,
        params NuGetVersion[] version);
}