
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CodingAgent.Embeddings;

public class EmbeddingGenerator
{
    private readonly HttpClient _httpClient = new();

    public async Task<float[]> GenerateAsync(
        string text,
        bool isQuery = false)
    {
        string prefix = isQuery
            ? "search_query: "
            : "search_document: ";

        var request = new
        {
            model = "nomic-embed-text",
            prompt = prefix + text
        };

        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:11434/api/embeddings",
            request
        );

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<EmbeddingResponse>();

        return result?.Embedding
            ?? throw new Exception("Embedding generation failed.");
    }

    private class EmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = [];
    }
}
