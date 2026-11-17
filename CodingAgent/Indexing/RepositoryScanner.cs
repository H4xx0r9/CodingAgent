using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodingAgent.Indexing;

public class RepositoryScanner
{
    private readonly string[] _supportedExtensions =
    {
        ".cs",
        ".json",
        ".md",
        ".csproj"
    };

    private readonly HashSet<string> _ignoredDirectories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".git",
            "bin",
            "obj",
            "node_modules",
            ".vs",
            "packages"
        };

    public List<string> Scan(string repositoryPath)
    {
        if (!Directory.Exists(repositoryPath))
        {
            throw new DirectoryNotFoundException(
                $"Repository not found: {repositoryPath}");
        }

        var files = new List<string>();

        ScanDirectory(repositoryPath, files);

        return files;
    }

    private void ScanDirectory(string directory, List<string> files)
    {
        foreach (var file in Directory.GetFiles(directory))
        {
            var extension = Path.GetExtension(file);

            if (_supportedExtensions.Contains(
                    extension,
                    StringComparer.OrdinalIgnoreCase))
            {
                files.Add(file);
            }
        }

        foreach (var subdirectory in Directory.GetDirectories(directory))
        {
            var directoryName = Path.GetFileName(subdirectory);

            if (_ignoredDirectories.Contains(directoryName))
                continue;

            ScanDirectory(subdirectory, files);
        }
    }
}