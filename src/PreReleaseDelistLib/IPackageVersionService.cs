namespace PreReleaseDelistLib;

public interface IPackageVersionService
{
    Task<NuGetVersion[]> GetPrereleasePackageVersionsAsync(string nugetApiUrl, string nugetApiKey, string packageId,
        CancellationToken cancellationToken);
    
    Task<NuGetVersion[]> GetAllPackageVersionsAsync(string nugetApiUrl, string nugetApiKey, string packageId,
        CancellationToken cancellationToken);
}