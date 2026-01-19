using System.Net;
using System.Runtime.CompilerServices;
using EnhancedLinq.Deferred;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace PreReleaseDelistLib;

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
    /// 
    /// </summary>
    /// <param name="nugetApiUrl"></param>
    /// <param name="nugetApiKey"></param>
    /// <param name="packageId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(NuGetVersion version, bool delistSuccess, string responseMessage)[]> RequestPackageDelistAsync(
        string nugetApiUrl, string nugetApiKey, string packageId, CancellationToken cancellationToken)
    {
        NuGetVersion[] versionToDelist = await _packageVersionService.GetAllPackageVersionsAsync
            (nugetApiUrl, nugetApiKey, packageId, cancellationToken);
        
        IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)> delistResults = 
            RequestPackageDelistAsync(nugetApiUrl, nugetApiKey, packageId, cancellationToken,  versionToDelist);

        return await delistResults.ToArrayAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nugetApiUrl"></param>
    /// <param name="nugetApiKey"></param>
    /// <param name="packageId"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="versions"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)> RequestPackageDelistAsync(string nugetApiUrl,
        string nugetApiKey, string packageId, [EnumeratorCancellation] CancellationToken cancellationToken, params NuGetVersion[] versions)
    {
        ArgumentException.ThrowIfNullOrEmpty(nugetApiUrl);
        ArgumentException.ThrowIfNullOrEmpty(packageId);
        ArgumentNullException.ThrowIfNull(versions);
        
        bool doesPackageExists = await CheckIfPackageExists(nugetApiUrl, packageId, cancellationToken);

        if(!doesPackageExists)
            throw new ArgumentException($"Package '{packageId}' does not exist on Nuget Server '{nugetApiUrl}'.");
        
        (NuGetVersion version, bool isListed)[] versionListResults = await CheckIfPackageIsListed(nugetApiUrl, packageId, versions, cancellationToken);
        
        NuGetVersion[] versionsToDelist = versionListResults.Select(x => x.version)
            .ToArray();

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
        
        await foreach (Task<(NuGetVersion version, HttpResponseMessage responseMessage)> response in Task.WhenEach(delistResponses).WithCancellation(cancellationToken))
        {
            yield return (response.Result.version, response.Result.responseMessage.StatusCode == HttpStatusCode.Accepted,
                response.Result.responseMessage.ReasonPhrase ?? string.Empty);
        }
    }

    private async Task<(NuGetVersion version, bool isListed)[]> CheckIfPackageIsListed(string nugetApiUrl, string packageId,
        NuGetVersion[] versions, CancellationToken cancellationToken)
    {
        var feedInfo = InitFeedInfo(nugetApiUrl);
        
        PackageSearchResource searchResource =
            await feedInfo.repository.GetResourceAsync<PackageSearchResource>(cancellationToken);

        SearchFilter searchFilter = new(true, SearchFilterType.IsAbsoluteLatestVersion)
        {
            PackageTypes = ["Dependency", "DotnetCliTool"]
        };

        IEnumerable<IPackageSearchMetadata> packages = await searchResource.SearchAsync("", searchFilter, 0, 10000,
            NullLogger.Instance, cancellationToken);

        IPackageSearchMetadata? package = packages.FirstOrDefault(p => p.IsListed && p.Identity.Id == packageId);

        if (package is null)
            return [];
        
        IEnumerable<VersionInfo> actualVersions =  await package.GetVersionsAsync();

        List<(NuGetVersion version, bool isListed)> output = new();

        VersionInfo[] packageVersions = actualVersions.ToArray();
        
        foreach (VersionInfo packageVersion in packageVersions)
        {
            NuGetVersion? version = versions.FirstOrDefault(v => v == packageVersion.Version);
            
            if (version is not null)
            {
                output.Add((version, packageVersion.PackageSearchMetadata.IsListed));
            }
        }

        NuGetVersion[] totalVersions = packageVersions.Select(v => v.Version).ToArray();

        IEnumerable<NuGetVersion> remainingVersions = totalVersions.Exclude(versions);
        
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