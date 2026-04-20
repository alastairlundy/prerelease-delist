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

    /// <summary>
    /// Asynchronously delists all versions of a NuGet package to be delisted based on the provided API credentials and package ID.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API endpoint.</param>
    /// <param name="nugetApiKey">The API key for authentication with the NuGet service.</param>
    /// <param name="packageId">The identifier of the package(s) to be delisted.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A sequence of tuples containing the version, whether the delisting was successful, and any response message from the API.</returns>
    public async IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)>
        RequestPackageDelistingAsync(string nugetApiUrl, string nugetApiKey,
            string packageId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        NuGetVersion[] versionToDelist = await _packageVersionService.GetAllPackageVersionsAsync
            (nugetApiUrl, nugetApiKey, packageId, cancellationToken);
        
        IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)> delistResults =
            RequestPackageDelistingAsync(nugetApiUrl, nugetApiKey, packageId, versionToDelist, cancellationToken);

        await foreach ((NuGetVersion version, bool delistSuccess, string responseMessage) result in delistResults)
        {
            yield return (result.version, result.delistSuccess, result.responseMessage);
        }
    }

    /// <summary>
    /// Asynchronously delists a list of versions of a NuGet package to be delisted based on the provided API credentials and package ID.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API endpoint.</param>
    /// <param name="nugetApiKey">The API key for authentication with the NuGet service.</param>
    /// <param name="packageId">The identifier of the package(s) to be delisted.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <param name="versions">The package versions to delist.</param>
    /// <returns>A sequence of tuples containing the version, whether the delisting was successful, and any response message from the API.</returns>
    public async IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)>
        RequestPackageDelistingAsync(string nugetApiUrl, string nugetApiKey, string packageId, IList<NuGetVersion> versions,
            [EnumeratorCancellation] CancellationToken cancellationToken)
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