
using Qdrant.Client;
using Qdrant.Client.Grpc;
using CodingAgent.Models;

namespace CodingAgent.Database;

public class QdrantService
{
    private readonly QdrantClient _client;
    private const string CollectionName = "codebase";

    public QdrantService()
    {
        _client = new QdrantClient("localhost", 6334);
    }

    // Check whether the collection already exists
    public async Task<bool> CollectionExistsAsync()
    {
        return await _client.CollectionExistsAsync(CollectionName);
    }

    // Create collection if it doesn't exist
    public async Task InitializeAsync()
    {
        bool exists = await CollectionExistsAsync();

        if (!exists)
        {
            await _client.CreateCollectionAsync(
                CollectionName,
                new VectorParams
                {
                    Size = 768,
                    Distance = Distance.Cosine
                }
            );

            Console.WriteLine("Qdrant collection created.");
        }
        else
        {
            Console.WriteLine("Qdrant collection already exists.");
        }
    }
    public async Task DeleteCollectionAsync()
    {
        if (await CollectionExistsAsync())
        {
            await _client.DeleteCollectionAsync(CollectionName);
            Console.WriteLine("Previous Qdrant collection deleted.");
        }
    }
    // Store code chunks and their embeddings
    public async Task StoreChunksAsync(List<CodeChunk> chunks)
    {
        var points = chunks
            .Where(c => c.Embedding != null)
            .Select(c => new PointStruct
            {
                Id = new PointId
                {
                    Uuid = c.Id.ToString()
                },
                Vectors = c.Embedding!,
                Payload =
                {
                    ["content"] = c.Content,
                    ["filePath"] = c.FilePath,
                    ["type"] = c.Type,
                    ["namespace"] = c.Namespace ?? "",
                    ["className"] = c.ClassName ?? "",
                    ["methodName"] = c.MethodName ?? "",
                    ["startLine"] = c.StartLine,
                    ["endLine"] = c.EndLine
                }
            })
            .ToList();

        if (points.Count == 0)
            return;

        await _client.UpsertAsync(
            CollectionName,
            points
        );
    }

    // Search for similar code chunks
    public async Task<IReadOnlyList<ScoredPoint>> SearchAsync(
        float[] queryVector,
        ulong limit = 5)
    {
        return await _client.SearchAsync(
            CollectionName,
            queryVector,
            limit: limit
        );
    }
}
