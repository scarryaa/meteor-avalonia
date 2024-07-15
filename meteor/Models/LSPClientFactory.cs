using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using meteor.Views.Interfaces;
using meteor.Views.Services;

namespace meteor.Models;

public class LspClientFactory(string configPath)
{
    private readonly LanguageServerManager _languageServerManager = new(configPath);
    private readonly Dictionary<string, ILspClient> _activeClients = new();

    public ILspClient GetOrCreateClient(string filePath)
    {
        var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

        if (_activeClients.TryGetValue(fileExtension, out var existingClient)) return existingClient;

        var config = _languageServerManager.GetLanguageServerConfiguration(fileExtension);
        Console.WriteLine($"Language server configuration for {fileExtension}: {config}");

        if (config.HasValue)
        {
            var (projectRootPath, projectFile) = FindProjectRootDirectory(filePath);
            if (string.IsNullOrEmpty(projectRootPath))
            {
                Console.WriteLine("Project root directory not found. Creating fallback client.");
                var fallbackClient = CreateFallbackClient(fileExtension);
                _activeClients[fileExtension] = fallbackClient;
                return fallbackClient;
            }

            Console.WriteLine($"Project root path: {projectRootPath}");
            Console.WriteLine($"Project file: {projectFile}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = config.Value.Command,
                    Arguments = BuildArguments(config.Value.Args, fileExtension, projectFile),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = projectRootPath
                }
            };

            Console.WriteLine(
                $"Starting language server with command: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
            process.Start();

            var client = new LspClient(process, projectRootPath);
            _activeClients[fileExtension] = client;
            return client;
        }

        throw new InvalidOperationException($"No language server configured for extension {fileExtension}");
    }

    private LspClient CreateFallbackClient(string fileExtension)
    {
        var config = _languageServerManager.GetLanguageServerConfiguration(fileExtension);
        if (!config.HasValue)
            throw new InvalidOperationException($"No language server configured for extension {fileExtension}");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = config.Value.Command,
                Arguments = BuildArguments(config.Value.Args, fileExtension, null),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetTempPath()
            }
        };

        Console.WriteLine(
            $"Starting fallback language server with command: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
        process.Start();

        return new LspClient(process, Path.GetTempPath());
    }

    private string BuildArguments(string[] configArgs, string fileExtension, string projectFile)
    {
        var args = new List<string>(configArgs);

        switch (fileExtension)
        {
            case ".cs":
                if (!string.IsNullOrEmpty(projectFile) && projectFile.EndsWith(".sln"))
                    args.Add($"--solution \"{projectFile}\"");
                break;
            case ".ts":
            case ".js":
                if (!string.IsNullOrEmpty(projectFile))
                    if (projectFile.EndsWith("tsconfig.json") || projectFile.EndsWith("package.json"))
                        args.Add($"--tsserver-path \"{Path.GetDirectoryName(projectFile)}\"");
                break;
        }

        return string.Join(" ", args);
    }

    private (string, string) FindProjectRootDirectory(string filePath)
    {
        var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
        var directory = new DirectoryInfo(Path.GetDirectoryName(filePath) ?? string.Empty);

        while (directory != null && directory.Exists)
        {
            var projectFile = FindProjectFile(directory.FullName, fileExtension);
            if (projectFile != null)
            {
                Console.WriteLine($"Found project file: {projectFile}");
                return (directory.FullName, projectFile);
            }

            directory = directory.Parent;
        }

        return (null, null);
    }

    private string FindProjectFile(string directoryPath, string fileExtension)
    {
        var projectExtensions = new List<string>();

        switch (fileExtension)
        {
            case ".ts":
            case ".js":
                projectExtensions.AddRange(new[] { "tsconfig.json", "package.json", "jsconfig.json" });
                break;
            case ".cs":
            case ".vb":
            case ".fs":
                projectExtensions.AddRange(new[] { "*.sln", "*.csproj", "*.vbproj", "*.fsproj" });
                break;
            default:
                projectExtensions.AddRange(
                    new[] { "*.csproj", "*.fsproj", "*.vbproj", "package.json", "tsconfig.json" });
                break;
        }

        foreach (var extension in projectExtensions)
        {
            var files = Directory.GetFiles(directoryPath, extension);
            if (files.Any())
                return files.First();
        }

        return null;
    }

    public void DisposeAllClients()
    {
        foreach (var client in _activeClients.Values) client.Dispose();
        _activeClients.Clear();
    }
}