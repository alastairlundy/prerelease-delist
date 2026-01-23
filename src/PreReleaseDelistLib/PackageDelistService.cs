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
using System.Runtime.CompilerServices;
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

    public PackageDelistService(IHttpClientFactory clientFactory, IPackageVersionService packageVersionService)
    {
        _clientFactory =  clientFactory;
        _packageVersionService = packageVersionService;
    }

    /// <summary>
    /// Asynchronously requests the delisting of specified NuGet package versions from a package registry.
    /// </summary>
    /// <param name="nugetApiUrl">The URL of the NuGet API.</param>
    /// <param name="nugetApiKey">The API key for authenticating with the NuGet service.</param>
    /// <param name="packageId">The identifier of the NuGet package to delist versions for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An array of tuples containing the NuGet version, a boolean indicating the success of the delisting operation, and a response message from the service.</returns>
    public async Task<(NuGetVersion version, bool delistSuccess, string responseMessage)[]> RequestPackageDelistAsync(
        string nugetApiUrl, string nugetApiKey, string packageId, CancellationToken cancellationToken)
    {
        NuGetVersion[] versionToDelist = await _packageVersionService.GetAllPackageVersionsAsync
            (nugetApiUrl, nugetApiKey, packageId, true, cancellationToken);
        
        IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)> delistResults = 
            RequestPackageDelistAsync(nugetApiUrl, nugetApiKey, packageId, cancellationToken,  versionToDelist);

        return await delistResults.ToArrayAsync(cancellationToken: cancellationToken);
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
        RequestPackageDelistAsync(string nugetApiUrl,
            string nugetApiKey, string packageId, [EnumeratorCancellation] CancellationToken cancellationToken,
            params NuGetVersion[] versions)
    {
        ArgumentException.ThrowIfNullOrEmpty(nugetApiUrl);
        ArgumentException.ThrowIfNullOrEmpty(packageId);
        ArgumentNullException.ThrowIfNull(versions);
        
        bool doesPackageExists = await CheckIfPackageExists(nugetApiUrl, packageId, cancellationToken);

        if(!doesPackageExists)
            throw new ArgumentException($"Package '{packageId}' does not exist on Nuget Server '{nugetApiUrl}'.");
        
        (NuGetVersion version, bool isListed)[] versionListResults = await CheckIfPackageIsListed(nugetApiUrl, nugetApiKey, packageId, versions, cancellationToken);
        
        NuGetVersion[] versionsToDelist = versionListResults.Where(v => !v.isListed)
            .Select(x => x.version)
            .ToArray();

        foreach ((NuGetVersion version, bool isListed) alreadyDelistedVersion in versionListResults.Where(v => !v.isListed))
        {
            yield return (alreadyDelistedVersion.version, false, "Package already de-listed.");
        }

        if (versionsToDelist.Length == 0)
            yield break;
        
        HttpClient client = _clientFactory.CreateClient();
        
        client.DefaultRequestHeaders.Add(NugetApiKeyHeaderName, [nugetApiKey]);
        client.BaseAddress =  new Uri(nugetApiUrl);
        client.Timeout = TimeSpan.FromMinutes(2);
        
        Task<(NuGetVersion version, HttpResponseMessage responseMessage)>[] delistResponses = new Task<(NuGetVersion version, HttpResponseMessage responseMessage)>[versionsToDelist.Length];

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
            index++;
        }
        
        await foreach (Task<(NuGetVersion version, HttpResponseMessage responseMessage)> response in Task.WhenEach(delistResponses)
                           .WithCancellation(cancellationToken))
        {
            yield return (response.Result.version, response.Result.responseMessage.StatusCode == HttpStatusCode.Accepted,
                response.Result.responseMessage.ReasonPhrase ?? string.Empty);
        }
    }

    private async Task<(NuGetVersion version, bool isListed)[]> CheckIfPackageIsListed(string nugetApiUrl, string nugetApiKey, string packageId,
        NuGetVersion[] versions, CancellationToken cancellationToken)
    {
        List<(NuGetVersion versions, bool isListed)> output = new(capacity: versions.Length);
        
        NuGetVersion[] alreadyDelistedVersions = await _packageVersionService.GetDelistedPackageVersionsAsync(nugetApiUrl, nugetApiKey,
            packageId, cancellationToken);
        
        output.AddRange(alreadyDelistedVersions.Select(v => new ValueTuple<NuGetVersion, bool>(v, false)));
        
        IEnumerable<NuGetVersion> remainingVersions = versions.Exclude(alreadyDelistedVersions);
        
        foreach (NuGetVersion version in remainingVersions)
        {
            if (!output.Contains((version, true)))
            {
                output.Add((version, false));
            }
        }
        
        return output.ToArray();
    }

    private (SourceRepository repository, SourceCacheContext cacheContext) InitFeedInfo(string nugetApiUrl)
    {
        SourceRepository repository = Repository.Factory.GetCoreV3(nugetApiUrl);

        SourceCacheContext cacheContext = new();
        
        return (repository, cacheContext);
    }

    private async Task<bool> CheckIfPackageExists(string nugetApiUrl, string packageId, CancellationToken cancellationToken)
    {
        (SourceRepository repository, SourceCacheContext cacheContext) feedInfo = InitFeedInfo(nugetApiUrl);

        PackageSearchResource searchResource =
            await feedInfo.repository.GetResourceAsync<PackageSearchResource>(cancellationToken);

        SearchFilter searchFilter = new(true, SearchFilterType.IsAbsoluteLatestVersion);

        IEnumerable<IPackageSearchMetadata>? packages = await searchResource.SearchAsync(packageId, searchFilter, 0,
            1000, NullLogger.Instance, cancellationToken);

        if (packages is null)
            return false;

        return packages.Where(p => p.IsListed)
            .Any(p => p.Identity.Id == packageId);
    }
}