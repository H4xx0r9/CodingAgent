using CodingAgent.Models;

namespace CodingAgent.Indexing;

public class ChunkGenerator
{
    private readonly RoslynParser _roslynParser = new();

    public List<CodeChunk> Generate(string filePath)
    {
        string extension = Path.GetExtension(filePath)
            .ToLowerInvariant();

        return extension switch
        {
            ".cs" => _roslynParser.Parse(filePath),

            ".json" => GenerateTextChunks(filePath, "JSON"),

            ".md" => GenerateMarkdownChunks(filePath),

            ".csproj" => GenerateTextChunks(filePath, "Project"),

            _ => new List<CodeChunk>()
        };
    }

    private List<CodeChunk> GenerateTextChunks(
        string filePath, string type)
    {
        string content = File.ReadAllText(filePath);

        return new List<CodeChunk>
        {
            new CodeChunk
            {
                FilePath = filePath,
                Content = content,
                Type = type,
                StartLine = 1,
                EndLine = File.ReadAllLines(filePath).Length
            }
        };
    }

    private List<CodeChunk> GenerateMarkdownChunks(string filePath)
    {
        // We'll implement heading-based chunking next.
        return GenerateTextChunks(filePath, "Markdown");
    }
}