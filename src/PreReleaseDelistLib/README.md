# PreReleaseDelistLib

`PreReleaseDelistLib` is a .NET library designed to help manage and delist prerelease versions of NuGet packages from a NuGet repository.

It supports .NET 9 and newer.

## Features

- **Retrieve Package Versions**: Get all versions, only prerelease versions, or only delisted versions of a package.
- **Delist Packages**: Request the delisting of specific package versions from a NuGet server using the NuGet API.
- **Support for NuGet V3**: Built on top of `NuGet.Protocol` for reliable interaction with NuGet repositories.

## Installation

To use `PreReleaseDelistLib` in your project, you can add it as a project reference or include the source files.

### Dependencies

The library depends on the following NuGet packages:
- `EnhancedLinq` (>= 1.0.0-alpha.5)
- `Microsoft.Extensions.Http` (>= 10.0.2)
- `NuGet.Protocol` (>= 7.0.1)

## Usage

### PackageVersionService

Used to query package versions from a NuGet feed.

```csharp
using PreReleaseDelistLib;
using NuGet.Versioning;

var versionService = new PackageVersionService();
string nugetApiUrl = "https://api.nuget.org/v3/index.json";
string packageId = "Your.Package.Id";

// Get all prerelease versions
NuGetVersion[] versions = await versionService.GetPrereleasePackageVersionsAsync(
    nugetApiUrl, 
    string.Empty, // API key not required for public GET requests
    packageId, 
    CancellationToken.None);

foreach (var version in versions)
{
    Console.WriteLine(version.ToNormalizedString());
}
```

### PackageDelistService

Used to delist package versions. It requires an `IHttpClientFactory` and an `IPackageVersionService` instance.

```csharp
using PreReleaseDelistLib;
using Microsoft.Extensions.DependencyInjection;

// Setup Dependency Injection for IHttpClientFactory
var services = new ServiceCollection();
services.AddHttpClient();
services.AddSingleton<IPackageVersionService, PackageVersionService>();
services.AddSingleton<IPackageDelistService, PackageDelistService>();
var serviceProvider = services.BuildServiceProvider();

var delistService = serviceProvider.GetRequiredService<IPackageDelistService>();

string nugetApiUrl = "https://www.nuget.org/api/v2/package"; // Use the appropriate delist endpoint
string nugetApiKey = "your-api-key";
string packageId = "Your.Package.Id";

var results = await delistService.RequestPackageDelistAsync(
    nugetApiUrl, 
    nugetApiKey, 
    packageId, 
    CancellationToken.None);

foreach (var result in results)
{
    Console.WriteLine($"Version: {result.version}, Success: {result.delistSuccess}, Message: {result.responseMessage}");
}
```

## License

This project is licensed under the GNU LGPL v3.0 or later. See the `COPYING` and `COPYING.LESSER` files for details.
