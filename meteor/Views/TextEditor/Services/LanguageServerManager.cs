using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using meteor.Models;
using File = System.IO.File;

namespace meteor.Views.Services;

public class LanguageServerManager
{
    private readonly Dictionary<string, LanguageServerConfig>? _languageServers;
    private readonly HttpClient _httpClient;

    public LanguageServerManager(string configPath)
    {
        _httpClient = new HttpClient();
        try
        {
            if (!File.Exists(configPath))
                throw new FileNotFoundException("The configuration file was not found.", configPath);

            var configJson = File.ReadAllText(configPath);
            Console.WriteLine(configJson);
            _languageServers = JsonSerializer.Deserialize<Dictionary<string, LanguageServerConfig>>(configJson);

            Console.WriteLine($"Successfully loaded configuration: {_languageServers != null}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Access to the path '{configPath}' is denied. Exception: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while reading the configuration file: {ex.Message}");
            throw;
        }
    }

    private bool IsLanguageServerInstalled(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            Console.WriteLine("Cannot check if language server is installed: Command is empty");
            return false;
        }

        try
        {
            Console.WriteLine($"Checking if language server is installed: {command}");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"command -v {command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking if language server is installed: {ex.Message}");
            return false;
        }
    }

    private async Task InstallLanguageServerAsync(string installCommand)
    {
        if (string.IsNullOrWhiteSpace(installCommand))
        {
            Console.WriteLine("Cannot install language server: Install command is empty");
            return;
        }

        try
        {
            Console.WriteLine($"Installing language server with command: {installCommand}");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{installCommand}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            Console.WriteLine($"Installation process exited with code: {process.ExitCode}");

            if (process.ExitCode == 0)
            {
                Console.WriteLine("C# Language Server (csharp-ls) installed successfully.");
                // Update the configuration with the installed path
                UpdateCSharpLanguageServerPath();
            }
            else
            {
                Console.WriteLine($"Failed to install C# Language Server. Error: {process.StandardError.ReadToEnd()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error installing language server: {ex.Message}");
        }
    }

    private void UpdateCSharpLanguageServerPath()
    {
        var csharpLsPath = GetCSharpLsPath();
        if (!string.IsNullOrEmpty(csharpLsPath))
        {
            if (_languageServers != null && _languageServers.TryGetValue(".cs", out var config))
            {
                config.Args[0] = csharpLsPath;
                Console.WriteLine($"Updated C# Language Server path: {csharpLsPath}");
            }
        }
        else
        {
            Console.WriteLine("Failed to find installed C# Language Server path");
        }
    }

    private string GetCSharpLsPath()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = "csharp-ls",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        return output;
    }

    private async Task InstallCSharpLanguageServerAsync()
    {
        Console.WriteLine("Installing C# Language Server (Omnisharp Roslyn)");

        var downloadUrl =
            "https://github.com/OmniSharp/omnisharp-roslyn/releases/download/v1.39.6/omnisharp-linux-x64-net6.0.zip";
        var zipFilePath = Path.Combine(Path.GetTempPath(), "omnisharp-linux-x64-net6.0.zip");
        var extractPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".omnisharp");

        Directory.CreateDirectory(extractPath);

        // Use shell command to download the zip file
        var downloadCommand = $"curl -L -o {zipFilePath} {downloadUrl}";
        Console.WriteLine($"Downloading Omnisharp Roslyn with command: {downloadCommand}");
        var downloadProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                Arguments = $"-c \"{downloadCommand}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        downloadProcess.Start();
        downloadProcess.WaitForExit();
        Console.WriteLine($"Download process exited with code: {downloadProcess.ExitCode}");

        if (downloadProcess.ExitCode != 0)
        {
            Console.WriteLine(
                $"Failed to download Omnisharp Roslyn. Standard output: {downloadProcess.StandardOutput.ReadToEnd()}");
            Console.WriteLine($"Standard error: {downloadProcess.StandardError.ReadToEnd()}");
            return;
        }

        // Use shell command to extract the zip file
        var extractCommand = $"unzip -o {zipFilePath} -d {extractPath}";
        Console.WriteLine($"Extracting Omnisharp Roslyn with command: {extractCommand}");
        var extractProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                Arguments = $"-c \"{extractCommand}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        extractProcess.Start();
        extractProcess.WaitForExit();
        Console.WriteLine($"Extraction process exited with code: {extractProcess.ExitCode}");

        if (extractProcess.ExitCode != 0)
        {
            Console.WriteLine(
                $"Failed to extract Omnisharp Roslyn. Standard output: {extractProcess.StandardOutput.ReadToEnd()}");
            Console.WriteLine($"Standard error: {extractProcess.StandardError.ReadToEnd()}");
            return;
        }

        var toolPath = Path.Combine(extractPath, "OmniSharp.dll");
        UpdateCSharpLanguageServerPath(toolPath);
    }

    private void UpdateCSharpLanguageServerPath(string toolPath)
    {
        if (!string.IsNullOrEmpty(toolPath))
        {
            if (_languageServers != null && _languageServers.TryGetValue(".cs", out var config))
            {
                config.Args[1] = toolPath;
                Console.WriteLine($"Updated C# Language Server path: {toolPath}");
            }
        }
        else
        {
            Console.WriteLine("Failed to find installed C# Language Server path");
        }
    }

    public async Task<(string Command, string[] Args)?> GetLanguageServerConfigurationAsync(string fileExtension)
    {
        if (_languageServers != null && _languageServers.TryGetValue(fileExtension, out var config))
        {
            Console.WriteLine(
                $"Found configuration for extension {fileExtension}: Command={config.Command}, Args={string.Join(" ", config.Args)}, InstallCommand={config.InstallCommand}");

            if (string.IsNullOrWhiteSpace(config.Command))
            {
                if (string.IsNullOrWhiteSpace(config.InstallCommand))
                {
                    Console.WriteLine(
                        $"Both Command and InstallCommand are empty for extension {fileExtension}. Cannot proceed.");
                    return null;
                }

                Console.WriteLine($"Command is empty for extension {fileExtension}. Attempting to install.");
                await InstallLanguageServerAsync(config.InstallCommand);

                if (fileExtension == ".cs")
                {
                    var toolPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".omnisharp", "OmniSharp", "OmniSharp.dll");
                    UpdateCSharpLanguageServerPath(toolPath);
                }
            }

            if (!IsLanguageServerInstalled(config.Command))
            {
                Console.WriteLine($"Language server not installed, attempting to install: {config.Command}");
                await InstallLanguageServerAsync(config.InstallCommand);
            }

            return (config.Command, config.Args);
        }

        Console.WriteLine($"No configuration found for extension {fileExtension}");
        return null;
    }

    public (string Command, string[] Args)? GetLanguageServerConfiguration(string fileExtension)
    {
        if (_languageServers != null && _languageServers.TryGetValue(fileExtension, out var config))
        {
            Console.WriteLine(
                $"Found configuration for extension {fileExtension}: Command={config.Command}, Args={string.Join(" ", config.Args)}, InstallCommand={config.InstallCommand}");

            if (string.IsNullOrWhiteSpace(config.Command))
            {
                Console.WriteLine($"Command is empty for extension {fileExtension}. Cannot proceed.");
                return null;
            }

            return (config.Command, config.Args);
        }

        Console.WriteLine($"No configuration found for extension {fileExtension}");
        return null;
    }

    public Process StartLanguageServer(string command, string[] args, string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new InvalidOperationException("Cannot start process because a file name has not been provided.");

        var allArgs = args != null ? args.ToList() : new List<string>();
        allArgs.Add($"--solution {projectRoot}");

        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = string.Join(" ", allArgs),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = projectRoot
        };

        Console.WriteLine($"Starting language server with command: {command} {startInfo.Arguments}");
        Console.WriteLine($"Working directory: {startInfo.WorkingDirectory}");

        var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine($"LSP Server Error: {e.Data}");
            };
            process.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting language server process: {ex.Message}");
            throw;
        }

        return process;
    }
}