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
                Environment.SetEnvironmentVariable("PreReleaseDelistCLI_NuGetServer:ApiKey", apiKey, EnvironmentVariableTarget.User);
                success = true;
            }
            else
            {
                using ProcessConfiguration shellConfig = processConfigurationFactory
                    .Create(shellInfo.TargetFilePath.FullName, 
                        $"export PreReleaseDelistCLI_NuGetServer:ApiKey='{apiKey}'");

                ProcessResult result = await processInvoker.ExecuteAsync(shellConfig,
                    cancellationToken: cancellationToken);
                
                success = result.ExitCode == 0;
            }
        }

        if (!string.IsNullOrEmpty(serverUrl))
        {
            if (OperatingSystem.IsWindows())
            {
                Environment.SetEnvironmentVariable("PreReleaseDelistCLI_ApiBaseUrl", apiKey, EnvironmentVariableTarget.User);
                success = true;
            }
            else
            {
                using ProcessConfiguration shellConfig = processConfigurationFactory
                    .Create(shellInfo.TargetFilePath.FullName, 
                        $"export PreReleaseDelistCLI_NuGetServer:ApiBaseUrl='{serverUrl}'");

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
            Environment.SetEnvironmentVariable("PreReleaseDelistCLI_NuGetServer:ApiKey", "", EnvironmentVariableTarget.User);
        
            Environment.SetEnvironmentVariable("PreReleaseDelistCLI_NuGetServer:ApiBaseUrl", "", EnvironmentVariableTarget.User);
            success = true;
        }
        else
        {
            ShellInformation shellInformation = await shellDetector.ResolveDefaultShellAsync(cancellationToken);

            using ProcessConfiguration shellConfig = processConfigurationFactory
                .Create(shellInformation.TargetFilePath.FullName,
                    "unset PreReleaseDelistCLI_NuGetServer:ApiKey PreReleaseDelistCLI_NuGetServer:ApiBaseUrl");
            
            ProcessResult result = await processInvoker.ExecuteAsync(shellConfig,
                cancellationToken: cancellationToken);
            
            success = result.ExitCode == 0;
        }
        
        return success ? 0 : 1;
    }
}