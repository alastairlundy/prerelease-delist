/*
    prerelease-delist - Delist pre-release library versions from a Nuget Server
    Copyright (C) 2026 Alastair Lundy

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
     any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using EnhancedLinq.Immediate;
using NuGet.Versioning;

namespace PreReleaseDelistCli;

[CliCommand(Name = "")]
public class DelistCommand
{
    private readonly IConfiguration _configuration;
    private readonly IPackageDelistService _packageDelistService;

    public DelistCommand(IConfiguration configuration,
        IPackageDelistService packageDelistService)
    {
        _configuration = configuration;
        _packageDelistService = packageDelistService;
    }
    
    [CliOption(Name = "--package-id", Required = true,
        Arity = CliArgumentArity.ExactlyOne)]
    public string PackageId { get; set; }

    [CliOption(Name = "--delist-all-versions")]
    public bool DelistAllVersions { get; set; } = false;

    [CliOption(Name = "--use-strict-parsing")]
    public bool UseStrictParsing { get; set; } = true;
    
    [CliArgument(Name = "versions")]
    public string[] Versions { get; set; }
    
    [CliOption(Name = "--api-key", Required = true)]
    [DefaultValue(null)]
    public string? ApiKey { get; set; }

    [CliOption(Name = "--server-url", Required = false)]
    [DefaultValue("https://api.nuget.org/v3/index.json")]
    public string ServerUrl { get; set; } = "https://api.nuget.org/v3/index.json";
    
    public async Task<int> RunAsync()
    {
        Versions = Versions.Exclude(Versions, s => string.IsNullOrEmpty(s) || !char.IsDigit(s.First()));

        if (Versions.Length == 0)
        {
            await Console.Error.WriteLineAsync(Resources.Errors_Input_NoVersionStrings);
            return -1;
        }

        ArgumentException.ThrowIfNullOrEmpty(PackageId);
        
        string? nugetApiKey = !string.IsNullOrEmpty(ApiKey) ? ApiKey : _configuration["NuGetApiKey"];

        ArgumentException.ThrowIfNullOrEmpty(nugetApiKey);

        if (string.IsNullOrEmpty(nugetApiKey))
        {
            Console.WriteLine(Resources.Exceptions_Configuration_NugetApiKey);
            return -1;
        }

        (NuGetVersion version, bool isDelisted, string responseMessage)[] results; 
        
        if (DelistAllVersions)
        {
            results = await _packageDelistService.RequestPackageDelistAsync(ServerUrl, nugetApiKey,
                PackageId, CancellationToken.None);
        }
        else
        {
            NuGetVersion[] parsedVersions = ParseVersions(Versions, UseStrictParsing);
            
            results = await _packageDelistService.RequestPackageDelistAsync(ServerUrl, nugetApiKey,
                    PackageId, CancellationToken.None,
                    parsedVersions)
                .ToArrayAsync(CancellationToken.None);    
        }

        await Console.Out.WriteLineAsync($"Versions Delisted for Package: {PackageId}");

        IEnumerable<(NuGetVersion version, bool isDelisted, string responseMessage)> delistedVersions = results
            .Where(x => x.isDelisted);

        int delistedVersionsCount = 0;
        foreach ((NuGetVersion version, bool isDelisted, string responseMessage) result in delistedVersions)
        {
            await Console.Out.WriteLineAsync($"{result.version.ToFullString()}");
            delistedVersionsCount++;
        }

        await Console.Out.WriteLineAsync();

        int exitCode = 0;
        
        if (delistedVersionsCount < results.Length)
        {
            await Console.Out.WriteLineAsync($"The following versions of {PackageId} could not be delisted:");

            foreach ((NuGetVersion version, bool isDelisted, string responseMessage) result in 
                     results.Where(x => !x.isDelisted))
            {
                await Console.Out.WriteLineAsync($"{result.version.ToFullString()} - With Reason: {result.responseMessage}");
            }

            exitCode = 1;
        }
        
        return exitCode;
    }
    
    private NuGetVersion[] ParseVersions(string[] versions, bool throwOnError)
    {
        List<NuGetVersion> output = new(capacity: versions.Length);

        foreach (string versionString in versions)
        {
            bool success = NuGetVersion.TryParse(versionString, out NuGetVersion? version);

            if (success && version is not null)
            {
                output.Add(version);
            }
            else
            {
                if(throwOnError)
                    throw new ArgumentException("Invalid version string: " + versionString);
            }
        }
        
        return output.ToArray();
    }
}