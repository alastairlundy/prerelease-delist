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

[CliCommand(Name = "auth")]
public class AuthCommand
{
    private readonly IShellDetector _shellDetector;
    private readonly IProcessConfigurationFactory _processConfigurationFactory;
    private readonly IProcessInvoker _processInvoker;

    public AuthCommand(IShellDetector shellDetector,
        IProcessConfigurationFactory processConfigurationFactory,
        IProcessInvoker processInvoker)
    {
        _shellDetector = shellDetector;
        _processConfigurationFactory = processConfigurationFactory;
        _processInvoker = processInvoker;
    }

    [CliArgument(Order = 0)]
    public string ApiKey { get; set; }
    
    [CliArgument(Order = 1)]
    public string ServerUrl { get; set; }
    
    public async Task<int> Init(
        CancellationToken cancellationToken)
    {
        bool success;
        
        ArgumentException.ThrowIfNullOrEmpty(ServerUrl);
        ArgumentException.ThrowIfNullOrEmpty(ApiKey);
        
        if (OperatingSystem.IsWindows())
        {
            Environment.SetEnvironmentVariable("PreReleaseDelistCLI_NuGetApiKey", ApiKey, EnvironmentVariableTarget.User);
                
            Environment.SetEnvironmentVariable("PreReleaseDelistCLI_NuGetServerUrl", ServerUrl, EnvironmentVariableTarget.User);
            success = true;
        }
        else
        {
            ShellInformation shellInfo = await _shellDetector.
                ResolveDefaultShellAsync(cancellationToken);

            using ProcessConfiguration shellConfig = _processConfigurationFactory
                .Create(shellInfo.TargetFilePath.FullName, 
                    $"export PreReleaseDelistCLI_NuGetApiKey='{ApiKey}'");

            ProcessResult result = await _processInvoker.ExecuteAsync(shellConfig,
                cancellationToken: cancellationToken);
                
            using ProcessConfiguration shellConfig2 = _processConfigurationFactory
                .Create(shellInfo.TargetFilePath.FullName, 
                    $"export PreReleaseDelistCLI_NuGetServerUrl='{ServerUrl}'");

            ProcessResult result2 = await _processInvoker.ExecuteAsync(shellConfig2,
                cancellationToken: cancellationToken);
                
            success = result.ExitCode == 0 && result2.ExitCode == 0;
        }
        
        return success ? 0 : 1;
    }
    
    public async Task<int> Clear(CancellationToken cancellationToken)
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
            ShellInformation shellInformation = await _shellDetector.ResolveDefaultShellAsync(cancellationToken);

            using ProcessConfiguration shellConfig = _processConfigurationFactory
                .Create(shellInformation.TargetFilePath.FullName,
                    "unset PreReleaseDelistCLI_NuGetApiKey PreReleaseDelistCLI_NuGetServerUrl");
            
            ProcessResult result = await _processInvoker.ExecuteAsync(shellConfig,
                cancellationToken: cancellationToken);
            
            success = result.ExitCode == 0;
        }
        
        return success ? 0 : 1;
    }
}