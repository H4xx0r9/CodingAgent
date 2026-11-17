

using CodingAgent.Indexing;
using CodingAgent.Models;
using CodingAgent.Embeddings;
using CodingAgent.Database;
using CodingAgent.Retrieval;

Console.WriteLine("================================");
Console.WriteLine("       Codebase RAG");
Console.WriteLine("================================\n");

var scanner = new RepositoryScanner();
var chunkGenerator = new ChunkGenerator();
var embeddingGenerator = new EmbeddingGenerator();
var qdrant = new QdrantService();

try
{
    // 1. Check whether the vector database already exists
    Console.WriteLine("Checking Qdrant...");

    bool databaseExists = await qdrant.CollectionExistsAsync();

    if (databaseExists)
    {
        Console.WriteLine("\nVector database already exists!");
        Console.WriteLine("Skipping repository indexing and embeddings.");

        // Initialize retrieval service
        var retrieval = new RetrievalService(
            embeddingGenerator,
            qdrant
        );

        // Interactive search
        Console.WriteLine("\n================================");
        Console.WriteLine("       CODEBASE SEARCH");
        Console.WriteLine("================================");

        Console.WriteLine("Ask questions about your repository.");
        Console.WriteLine("Type 'exit' to quit.");

        while (true)
        {
            Console.Write("\nYour question: ");

            string? query = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(query))
                continue;

            if (query.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            Console.WriteLine("\nSearching...\n");

            var results = await retrieval.SearchAsync(query, topK: 5);

            Console.WriteLine("========== RESULTS ==========\n");

            if (results.Count == 0)
            {
                Console.WriteLine("No results found.");
                continue;
            }

            int rank = 1;

            foreach (var result in results)
            {
                var payload = result.Payload;

                string content = payload["content"].StringValue;
                string filePath = payload["filePath"].StringValue;
                string type = payload["type"].StringValue;

                Console.WriteLine($"Result #{rank++}");
                Console.WriteLine($"Similarity score: {result.Score:F4}");
                Console.WriteLine($"File: {filePath}");
                Console.WriteLine($"Type: {type}");

                Console.WriteLine("--------------------------------");
                Console.WriteLine(content);
                Console.WriteLine("--------------------------------\n");
            }
        }

        return;
    }

    Console.WriteLine("\nNo existing database found. Starting indexing...\n");

    // 2. Scan repository
    Console.Write("Enter repository path: ");
    string? path = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(path))
    {
        Console.WriteLine("Repository path cannot be empty.");
        return;
    }

    Console.WriteLine("\nScanning repository...\n");

    var files = scanner.Scan(path);

    Console.WriteLine($"Files found: {files.Count}");

    // 3. Generate chunks
    Console.WriteLine("\nGenerating chunks...\n");

    var allChunks = new List<CodeChunk>();

    foreach (var file in files)
    {
        try
        {
            var chunks = chunkGenerator.Generate(file);
            allChunks.AddRange(chunks);

            Console.WriteLine(
                $"{Path.GetFileName(file)} -> {chunks.Count} chunks");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error processing {file}: {ex.Message}");
        }
    }

    // 4. Display results
    Console.WriteLine("\n================================");
    Console.WriteLine("          RESULTS");
    Console.WriteLine("================================");

    Console.WriteLine($"Files scanned: {files.Count}");
    Console.WriteLine($"Chunks generated: {allChunks.Count}");

    Console.WriteLine("\nChunk type breakdown:");

    foreach (var group in allChunks.GroupBy(c => c.Type))
    {
        Console.WriteLine($"{group.Key}: {group.Count()}");
    }

    // 5. Generate embeddings
    Console.WriteLine("\n================================");
    Console.WriteLine("      GENERATING EMBEDDINGS");
    Console.WriteLine("================================\n");

    int embeddedCount = 0;

    foreach (var chunk in allChunks)
    {
        try
        {
            chunk.Embedding = await embeddingGenerator.GenerateAsync(
                chunk.Content
            );

            embeddedCount++;

            Console.WriteLine(
                $"[{embeddedCount}/{allChunks.Count}] " +
                $"{chunk.Type}: {chunk.Embedding.Length} dimensions");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Embedding error ({chunk.FilePath}): {ex.Message}");
        }
    }

    Console.WriteLine(
        $"\nSuccessfully generated: {embeddedCount}/{allChunks.Count}");

    // 6. Initialize Qdrant
    Console.WriteLine("\n================================");
    Console.WriteLine("       CONNECTING QDRANT");
    Console.WriteLine("================================\n");

    await qdrant.InitializeAsync();

    Console.WriteLine("Qdrant connected successfully!");

    // 7. Store chunks
    Console.WriteLine("\nStoring embeddings in Qdrant...\n");

    var chunksWithEmbeddings = allChunks
        .Where(c => c.Embedding != null)
        .ToList();

    await qdrant.StoreChunksAsync(chunksWithEmbeddings);

    Console.WriteLine(
        $"Successfully stored {chunksWithEmbeddings.Count} chunks!");

    // 8. Final summary
    Console.WriteLine("\n================================");
    Console.WriteLine("         INDEXING COMPLETE");
    Console.WriteLine("================================");

    Console.WriteLine($"Files scanned: {files.Count}");
    Console.WriteLine($"Chunks generated: {allChunks.Count}");
    Console.WriteLine($"Embeddings generated: {embeddedCount}");
    Console.WriteLine($"Chunks stored: {chunksWithEmbeddings.Count}");

    Console.WriteLine("\nYou can now run the application again to search!");
}
catch (Exception ex)
{
    Console.WriteLine($"\nError: {ex.Message}");
}
