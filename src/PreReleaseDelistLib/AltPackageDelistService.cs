namespace PreReleaseDelistLib;

public class AltPackageDelistService : IPackageDelistService
{
    public async Task<(NuGetVersion version, bool delistSuccess, string responseMessage)[]> RequestPackageDelistAsync(string nugetApiUrl, string nugetApiKey, string packageName,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(NuGetVersion version, bool delistSuccess, string responseMessage)> RequestPackageDelistAsync(string nugetApiUrl, string nugetApiKey, string packageName,
        CancellationToken cancellationToken, params NuGetVersion[] version)
    {
        throw new NotImplementedException();
    }
}