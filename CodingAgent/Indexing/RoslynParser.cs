
using CodingAgent.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodingAgent.Indexing;

public class RoslynParser
{
    private const int MaxChunkSize = 3000;
    private const int Overlap = 200;

    public List<CodeChunk> Parse(string filePath)
    {
        string sourceCode = File.ReadAllText(filePath);

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = syntaxTree.GetRoot();

        var chunks = new List<CodeChunk>();

        // Add a syntax element as one or more chunks.
        void AddChunk(SyntaxNode node, string type, string? name = null)
        {
            var lineSpan = node.GetLocation().GetLineSpan();

            var className = node.Ancestors()
                .OfType<TypeDeclarationSyntax>()
                .FirstOrDefault()?.Identifier.Text;

            var namespaceName = node.Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault()?.Name.ToString();

            string content = node.ToFullString();

            var parts = SplitContent(content);

            foreach (var part in parts)
            {
                chunks.Add(new CodeChunk
                {
                    FilePath = filePath,
                    Content = part,
                    Type = type,
                    Namespace = namespaceName,
                    ClassName = className,
                    MethodName = name,
                    StartLine = lineSpan.StartLinePosition.Line + 1,
                    EndLine = lineSpan.EndLinePosition.Line + 1
                });
            }
        }

        // 1. Using directives (including global usings)
        foreach (var node in root.DescendantNodes()
                     .OfType<UsingDirectiveSyntax>())
        {
            string type = node.GlobalKeyword != default
                ? "GlobalUsing"
                : "Using";

            AddChunk(node, type);
        }

        // 2. Namespaces
        foreach (var node in root.DescendantNodes()
                     .OfType<BaseNamespaceDeclarationSyntax>())
        {
            AddChunk(node, "Namespace", node.Name.ToString());
        }

        // 3. Classes, interfaces, structs and records
        foreach (var node in root.DescendantNodes()
                     .OfType<TypeDeclarationSyntax>())
        {
            AddChunk(node, node.Kind().ToString(), node.Identifier.Text);
        }

        // 4. Enums
        foreach (var node in root.DescendantNodes()
                     .OfType<EnumDeclarationSyntax>())
        {
            AddChunk(node, "Enum", node.Identifier.Text);
        }

        // 5. Methods
        foreach (var node in root.DescendantNodes()
                     .OfType<MethodDeclarationSyntax>())
        {
            AddChunk(node, "Method", node.Identifier.Text);
        }

        // 6. Constructors
        foreach (var node in root.DescendantNodes()
                     .OfType<ConstructorDeclarationSyntax>())
        {
            AddChunk(node, "Constructor", node.Identifier.Text);
        }

        // 7. Properties
        foreach (var node in root.DescendantNodes()
                     .OfType<PropertyDeclarationSyntax>())
        {
            AddChunk(node, "Property", node.Identifier.Text);
        }

        // 8. Fields
        foreach (var node in root.DescendantNodes()
                     .OfType<FieldDeclarationSyntax>())
        {
            foreach (var variable in node.Declaration.Variables)
            {
                AddChunk(node, "Field", variable.Identifier.Text);
            }
        }

        // 9. Enum members
        foreach (var node in root.DescendantNodes()
                     .OfType<EnumMemberDeclarationSyntax>())
        {
            AddChunk(node, "EnumMember", node.Identifier.Text);
        }

        return chunks;
    }

    // Split large chunks into smaller pieces.
    private List<string> SplitContent(string content)
    {
        var chunks = new List<string>();

        if (content.Length <= MaxChunkSize)
            return new List<string> { content };

        int start = 0;

        while (start < content.Length)
        {
            int length = Math.Min(
                MaxChunkSize,
                content.Length - start
            );

            chunks.Add(content.Substring(start, length));

            start += MaxChunkSize - Overlap;
        }

        return chunks;
    }
}
