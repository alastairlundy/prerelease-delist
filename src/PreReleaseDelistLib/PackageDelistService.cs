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

using System.Net;
using EnhancedLinq.Deferred;

namespace PreReleaseDelistLib;

/// <summary>
/// Service for requesting package delisting from a NuGet server.
/// </summary>
public class PackageDelistService : IPackageDelistService
{
    private const string NugetApiKeyHeaderName = "X-NuGet-ApiKey";
    
    private readonly IHttpClientFactory _clientFactory;
    private readonly IPackageVersionService _packageVersionService;
    private readonly IPackageAvailabilityDetector _packageAvailabilityDetector;

    public PackageDelistService(IHttpClientFactory clientFactory, IPackageVersionService packageVersionService,
        IPackageAvailabilityDetector packageAvailabilityDetector)
    {
        _clientFactory =  clientFactory;
        _packageVersionService = packageVersionService;
        _packageAvailabilityDetector = packageAvailabilityDetector;
    }

    /// <summary>
    /// Asynchronously requests the delisting of specified NuGet package versions from a package registry.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API.</param>
    /// <param name="nugetApiKey">The API key for authenticating with the NuGet service.</param>
    /// <param name="packageId">The identifier of the NuGet package to delist versions for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An array of tuples containing the NuGet version, a boolean indicating the success of the delisting operation, and a response message from the service.</returns>
    public async IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)>
        RequestPackageDelistingAsync(string nugetApiUrl, string nugetApiKey, string packageId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        NuGetVersion[] versionToDelist = await _packageVersionService.GetAllPackageVersionsAsync
            (nugetApiUrl, nugetApiKey, packageId, cancellationToken);
        
        IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)> result = RequestPackageDelistingAsync(nugetApiUrl,
            nugetApiKey, packageId, cancellationToken,versionToDelist.ToArray());

        await foreach ((NuGetVersion version, bool delistSuccess, string responseMessage) in result)
        {
            yield return (version, delistSuccess, responseMessage);
        }
    }

    /// <summary>
    /// Asynchronously requests the delisting of specified NuGet package versions from a package registry.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API.</param>
    /// <param name="nugetApiKey">The API key for authenticating with the NuGet service.</param>
    /// <param name="packageId">The identifier of the NuGet package to delist versions for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <param name="versions">The versions of the package to delist.</param>
    /// <returns>An array of tuples containing the NuGet version, a boolean indicating the success of the delisting operation, and a response message from the service.</returns>
    public async IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)>
        RequestPackageDelistingAsync(string nugetApiUrl,
            string nugetApiKey, string packageId, [EnumeratorCancellation] CancellationToken cancellationToken,
            params NuGetVersion[] versions)
    {
        ArgumentException.ThrowIfNullOrEmpty(nugetApiUrl);
        ArgumentException.ThrowIfNullOrEmpty(packageId);
        ArgumentNullException.ThrowIfNull(versions);
        
        bool doesPackageExist = await _packageAvailabilityDetector.CheckPackageExistsAsync(nugetApiUrl, packageId, cancellationToken);

        if(!doesPackageExist)
            throw new ArgumentException(string.Format(Resources.Exceptions_Package_NotFoundOnServer, packageId, nugetApiUrl), nameof(packageId));
        
        IDictionary<NuGetVersion, bool> checkVersionsForDelist = await _packageVersionService.CheckPackageVersionsListedAsync(nugetApiUrl, nugetApiKey, packageId,
            true, versions, cancellationToken);

        NuGetVersion[] alreadyDelistedVersions = checkVersionsForDelist.Where(kvp => !kvp.Value).Select(kvp => kvp.Key)
            .ToArray();
        
        NuGetVersion[] versionsToDelist = versions.Exclude(alreadyDelistedVersions)
            .ToArray();
        
        foreach (NuGetVersion alreadyDelistedVersion in alreadyDelistedVersions)
        {
            yield return (alreadyDelistedVersion, false, 
                Resources.Info_Package_AlreadyDelisted);
        }

        if (versionsToDelist.Length == 0)
            yield break;
        
        HttpClient client = _clientFactory.CreateClient();
        
        client.DefaultRequestHeaders.Add(NugetApiKeyHeaderName, [nugetApiKey]);
        client.BaseAddress =  new Uri(nugetApiUrl);
        client.Timeout = TimeSpan.FromMinutes(2);
        
        Task<(NuGetVersion version, HttpResponseMessage responseMessage)>[] delistResponses = new Task<(NuGetVersion version,
            HttpResponseMessage responseMessage)>[versionsToDelist.Length];

        int index = 0;
        foreach(NuGetVersion version in versionsToDelist)
        {
            delistResponses[index] = new Task<(NuGetVersion version, HttpResponseMessage responseMessage)>(() =>
            { 
                Task<HttpResponseMessage> response = client.DeleteAsync(
                    $"{packageId}{version.ToNormalizedString()}", cancellationToken);
                
                response.Wait(cancellationToken);
                
                return (version, response.Result);
            });

            delistResponses[index].Start();
            
            index++;
        }
        
        await foreach (Task<(NuGetVersion version, HttpResponseMessage responseMessage)> response in Task.WhenEach(delistResponses)
                           .WithCancellation(cancellationToken))
        {
            yield return (response.Result.version, response.Result.responseMessage.StatusCode == HttpStatusCode.Accepted,
                response.Result.responseMessage.ReasonPhrase ?? string.Empty);
        }
    }
}