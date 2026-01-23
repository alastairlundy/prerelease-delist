# prerelease-delist

A CLI to delist pre-release versions of your Nuget package(s).

## Features

- Delist specific pre-release versions of a NuGet package.
- Delist all pre-release versions of a NuGet package.
- Configure NuGet API Key and Server URL via environment variables or configuration files.

## Installation

### Prerequisites
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build

```bash
dotnet build -c Release
```

## Usage

### Authentication

You can set your NuGet API Key and Server URL using the `auth` command:

```bash
prerelease-delist auth init --api-key "YOUR_API_KEY" --server-url "https://api.nuget.org/v3/index.json"
```

To clear the stored authentication:

```bash
prerelease-delist auth clear
```

### Delisting Packages

To delist specific pre-release versions of a package:

```bash
prerelease-delist "MyPackage" --versions "1.0.0-alpha.1" "1.0.0-alpha.2"
```

To delist all pre-release versions of a package:

```bash
prerelease-delist "MyPackage" --delist-all-versions true
```

## Rate Limits
NuGet.org's NuGet server implementation has an API rate limit for delisting packages of [**250 package versions per hour**](https://learn.microsoft.com/en-gb/nuget/api/rate-limits) per API Key.

Third party NuGet servers may have their own rate limits. Please check your NuGet server's documentation for more information.

This CLI tries to gracefully fail (and inform you) if you exceed the API rate limit.

## Configuration

The tool searches for configuration in the following order:
1. `appsettings.json`
2. Environment variables prefixed with `PreReleaseDelistCLI_`

### Environment Variables

- `PreReleaseDelistCLI_NuGetServer:ApiKey`
- `PreReleaseDelistCLI_NuGetServer:ApiBaseUrl`

## License

The CLI project is licensed under the GNU GPL v3.0 or later – see the [LICENCE](LICENSE) file for details.

The library powering the CLI's NuGet-related functionality is licensed under the GNU LGPL v3.0 or later.
