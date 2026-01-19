namespace PreReleaseDelistLib;

public interface IPackageDelistService
{
    Task<(NuGetVersion version, bool delistSuccess, string responseMessage)[]> RequestPackageDelistAsync(
        string nugetApiUrl, string nugetApiKey, string packageName, CancellationToken cancellationToken);
    
    IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)> RequestPackageDelistAsync(
        string nugetApiUrl, string nugetApiKey, string packageName, CancellationToken cancellationToken,
        params NuGetVersion[] version);
}