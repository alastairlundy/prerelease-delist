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

using Microsoft.Extensions.DependencyInjection;

Cli.Ext.ConfigureServices(services =>
{
    services.AddHttpClient()
        .AddSingleton<IPackageVersionService, PackageVersionService>()
        .AddSingleton<IPackageDelistService, PackageDelistService>();

    IConfigurationBuilder configurationBuilder;

    // Fallback to avoid using AppSettings.Json if it is not present.
    try
    {
        configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");
    }
    catch
    {
        configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory());
    }
        
    IConfiguration configuration = configurationBuilder.Build();
        
    services.AddSingleton(configuration);
});

await Cli.RunAsync<DelistCommand>(args, new CliSettings
{
    EnableDefaultExceptionHandler = true,
    EnableSuggestDirective = true,
    EnableEnvironmentVariablesDirective = true
});