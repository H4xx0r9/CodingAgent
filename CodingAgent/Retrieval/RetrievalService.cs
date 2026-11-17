
using CodingAgent.Embeddings;
using CodingAgent.Database;
using Qdrant.Client.Grpc;

namespace CodingAgent.Retrieval;

public class RetrievalService
{
    private readonly EmbeddingGenerator _embeddingGenerator;
    private readonly QdrantService _qdrant;

    public RetrievalService(
        EmbeddingGenerator embeddingGenerator,
        QdrantService qdrant)
    {
        _embeddingGenerator = embeddingGenerator;
        _qdrant = qdrant;
    }

    public async Task<IReadOnlyList<ScoredPoint>> SearchAsync(
        string query,
        int topK = 5)
    {
        // Convert question into a query embedding
        float[] queryVector =
            await _embeddingGenerator.GenerateAsync(
                query,
                isQuery: true
            );

        // Find the most similar code chunks
        return await _qdrant.SearchAsync(
            queryVector,
            (ulong)topK
        );
    }
}
