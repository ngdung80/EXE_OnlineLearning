using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace POT_System_ASPNET.Services;

public interface IGeminiService
{
    Task<string> GenerateContentAsync(string prompt);
}

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public GeminiService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<string> GenerateContentAsync(string prompt)
    {
        var apiKey = _config["GeminiAPI:ApiKey"];
        var apiUrl = $"{_config["GeminiAPI:ApiUrl"]}?key={apiKey}";

        var escapedPrompt = prompt.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        var jsonPayload = $"{{\"contents\": [{{\"parts\": [{{\"text\": \"{escapedPrompt}\"}}]}}]}}";

        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"API request failed: {(int)response.StatusCode} - {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? throw new Exception("No content in API response");
    }
}
