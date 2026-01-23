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

using CliInvoke.Core;
using CliInvoke.Core.Factories;

namespace PreReleaseDelistCli;

public class AuthCommand
{
    public async Task<int> Init(
        [FromServices] IShellDetector shellDetector,
        [FromServices] IProcessConfigurationFactory processConfigurationFactory,
        [FromServices] IProcessInvoker processInvoker,
        CancellationToken cancellationToken,
        string apiKey = "", string serverUrl = "")
    {
        bool success = false;
        
        ShellInformation shellInfo = await shellDetector.
            ResolveDefaultShellAsync(cancellationToken);
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            if (OperatingSystem.IsWindows())
            {
                Environment.SetEnvironmentVariable("PreReleaseDelistCLI_NuGetApiKey", apiKey, EnvironmentVariableTarget.User);
                success = true;
            }
            else
            {
                using ProcessConfiguration shellConfig = processConfigurationFactory
                    .Create(shellInfo.TargetFilePath.FullName, 
                        $"export PreReleaseDelistCLI_NuGetApiKey='{apiKey}'");

                ProcessResult result = await processInvoker.ExecuteAsync(shellConfig,
                    cancellationToken: cancellationToken);
                
                success = result.ExitCode == 0;
            }
        }

        if (!string.IsNullOrEmpty(serverUrl))
        {
            if (OperatingSystem.IsWindows())
            {
                Environment.SetEnvironmentVariable("PreReleaseDelistCLI_NugetServerUrl", apiKey, EnvironmentVariableTarget.User);
                success = true;
            }
            else
            {
                using ProcessConfiguration shellConfig = processConfigurationFactory
                    .Create(shellInfo.TargetFilePath.FullName, 
                        $"export PreReleaseDelistCLI_NuGetServerUrl='{serverUrl}'");

                ProcessResult result = await processInvoker.ExecuteAsync(shellConfig,
                    cancellationToken: cancellationToken);
                
                success = result.ExitCode == 0;
            }
        }
        
        return success ? 0 : 1;
    }
    
    public async Task<int> Clear([FromServices] IShellDetector shellDetector,
        [FromServices] IProcessConfigurationFactory processConfigurationFactory,
        [FromServices] IProcessInvoker processInvoker,
        CancellationToken cancellationToken)
    {
        bool success;

        if (OperatingSystem.IsWindows())
        {
            Environment.SetEnvironmentVariable("PreReleaseDelistCLI_NuGetApiKey", "", EnvironmentVariableTarget.User);
        
            Environment.SetEnvironmentVariable("PreReleaseDelistCLI_NuGetServerUrl", "", EnvironmentVariableTarget.User);
            success = true;
        }
        else
        {
            ShellInformation shellInformation = await shellDetector.ResolveDefaultShellAsync(cancellationToken);

            using ProcessConfiguration shellConfig = processConfigurationFactory
                .Create(shellInformation.TargetFilePath.FullName,
                    "unset PreReleaseDelistCLI_NuGetApiKey PreReleaseDelistCLI_NuGetServerUrl");
            
            ProcessResult result = await processInvoker.ExecuteAsync(shellConfig,
                cancellationToken: cancellationToken);
            
            success = result.ExitCode == 0;
        }
        
        return success ? 0 : 1;
    }
}