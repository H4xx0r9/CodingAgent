
using System.Text.Json;
using CodingAgent.Indexing;
using CodingAgent.Models;
using CodingAgent.Embeddings;
using CodingAgent.Database;

namespace CodingAgent.Context;

public class RepositoryContextManager
{
    private readonly RepositoryScanner _scanner;
    private readonly ChunkGenerator _chunkGenerator;
    private readonly EmbeddingGenerator _embeddingGenerator;
    private readonly QdrantService _qdrant;

    private const string MetadataFile = "repository_context.json";

    public RepositoryContextManager()
    {
        _scanner = new RepositoryScanner();
        _chunkGenerator = new ChunkGenerator();
        _embeddingGenerator = new EmbeddingGenerator();
        _qdrant = new QdrantService();
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("\n================================");
        Console.WriteLine("     REPOSITORY CONTEXT");
        Console.WriteLine("================================\n");

        bool databaseExists = await _qdrant.CollectionExistsAsync();

        if (databaseExists)
        {
            var metadata = await LoadMetadataAsync();

            if (metadata != null)
            {
                Console.WriteLine($"Agent context: {metadata.RepositoryPath}");
            }
            else
            {
                Console.WriteLine("An existing vector database was found.");
                Console.WriteLine("Repository information is unavailable.");
            }

            Console.Write("\nAdd new repository as context? (Y/N): ");

            string? answer = Console.ReadLine();

            if (!string.Equals(answer, "Y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("\nKeeping existing repository context.");
                return;
            }
        }
        else
        {
            Console.WriteLine("Agent has no context of a repository.");
        }

        Console.Write("\nEnter repository path: ");
        string? repositoryPath = Console.ReadLine()?.Trim().Trim('"');

        if (string.IsNullOrWhiteSpace(repositoryPath) ||
            !Directory.Exists(repositoryPath))
        {
            Console.WriteLine("Invalid repository path.");
            return;
        }

        await IndexRepositoryAsync(repositoryPath);
    }

    private async Task IndexRepositoryAsync(string repositoryPath)
    {
        string repositoryName =
            Path.GetFileName(Path.TrimEndingDirectorySeparator(repositoryPath));

        Console.WriteLine($"\nIndexing repository: {repositoryName}");

        // 1. Scan repository
        Console.WriteLine("\nScanning repository...");

        var files = _scanner.Scan(repositoryPath);

        Console.WriteLine($"Files found: {files.Count}");

        // 2. Generate chunks
        Console.WriteLine("\nGenerating chunks...");

        var allChunks = new List<CodeChunk>();

        foreach (var file in files)
        {
            try
            {
                var chunks = _chunkGenerator.Generate(file);
                allChunks.AddRange(chunks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {file}: {ex.Message}");
            }
        }

        Console.WriteLine($"Chunks generated: {allChunks.Count}");

        if (allChunks.Count == 0)
        {
            Console.WriteLine("No chunks generated. Existing context was not replaced.");
            return;
        }

        // 3. Generate embeddings
        Console.WriteLine("\nGenerating embeddings...");

        int embeddedCount = 0;

        foreach (var chunk in allChunks)
        {
            try
            {
                chunk.Embedding =
                    await _embeddingGenerator.GenerateAsync(chunk.Content);

                embeddedCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Embedding error ({chunk.FilePath}): {ex.Message}");
            }
        }

        var chunksWithEmbeddings = allChunks
            .Where(c => c.Embedding != null)
            .ToList();

        Console.WriteLine(
            $"Embeddings generated: {embeddedCount}/{allChunks.Count}");

        if (chunksWithEmbeddings.Count == 0)
        {
            Console.WriteLine("No embeddings generated. Existing context was not replaced.");
            return;
        }

        // 4. Replace existing Qdrant collection
        Console.WriteLine("\nInitializing Qdrant...");

        await _qdrant.DeleteCollectionAsync();
        await _qdrant.InitializeAsync();

        // 5. Store chunks
        Console.WriteLine("\nStoring chunks in Qdrant...");

        await _qdrant.StoreChunksAsync(chunksWithEmbeddings);

        // 6. Save repository metadata
        var metadata = new RepositoryMetadata
        {
            RepositoryName = repositoryName,
            RepositoryPath = repositoryPath,
            IndexedAt = DateTime.UtcNow
        };

        await SaveMetadataAsync(metadata);

        Console.WriteLine("\n================================");
        Console.WriteLine("       INDEXING COMPLETE");
        Console.WriteLine("================================");

        Console.WriteLine($"Repository: {repositoryName}");
        Console.WriteLine($"Files scanned: {files.Count}");
        Console.WriteLine($"Chunks generated: {allChunks.Count}");
        Console.WriteLine($"Embeddings generated: {embeddedCount}");
        Console.WriteLine($"Chunks stored: {chunksWithEmbeddings.Count}");
    }

    private async Task<RepositoryMetadata?> LoadMetadataAsync()
    {
        if (!File.Exists(MetadataFile))
            return null;

        string json = await File.ReadAllTextAsync(MetadataFile);

        return JsonSerializer.Deserialize<RepositoryMetadata>(json);
    }

    private async Task SaveMetadataAsync(RepositoryMetadata metadata)
    {
        string json = JsonSerializer.Serialize(
            metadata,
            new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(MetadataFile, json);
    }
    public async Task AddRepositoryAsync(string repositoryPath)
{
    if (string.IsNullOrWhiteSpace(repositoryPath) ||
        !Directory.Exists(repositoryPath))
    {
        throw new DirectoryNotFoundException(
            $"Repository directory not found: {repositoryPath}");
    }

    await IndexRepositoryAsync(repositoryPath);
}
}

public class RepositoryMetadata
{
    public string RepositoryName { get; set; } = "";
    public string RepositoryPath { get; set; } = "";
    public DateTime IndexedAt { get; set; }
}
