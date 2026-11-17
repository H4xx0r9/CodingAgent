
namespace CodingAgent.Models;

public class CodeChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FilePath { get; set; } = "";
    public string Content { get; set; } = "";
    public string Type { get; set; } = "";

    public string? Namespace { get; set; }
    public string? ClassName { get; set; }
    public string? MethodName { get; set; }

    public int StartLine { get; set; }
    public int EndLine { get; set; }

    public float[]? Embedding { get; set; }
}
