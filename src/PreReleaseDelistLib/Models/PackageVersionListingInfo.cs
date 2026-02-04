namespace PreReleaseDelistLib.Models;

public class PackageVersionListingInfo
{
    public NuGetVersion PackageVersion { get; set; }
    
    public bool IsListed { get; set; }
    
    public bool PackageVersionExists { get; set; }
}