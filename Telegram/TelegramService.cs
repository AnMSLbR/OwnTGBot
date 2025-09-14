using System.Text;
using System.Text.Json;

public class TelegramService : ITelegramService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _tgToken;
    private readonly string _tgApiUrl;

    public TelegramService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _tgToken = _config["TgBotToken"] ?? "";
        _tgApiUrl = $"https://api.telegram.org/bot{_tgToken}/sendMessage";
    }

    public async Task HandleUpdateAsync(HttpRequest request, IAiAgentService aiAgentService)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("message", out var message))
        {
            long chatId = message.GetProperty("chat").GetProperty("id").GetInt64();
            string text = message.GetProperty("text").GetString() ?? "";

            var aiAgentResponse = await aiAgentService.AskAsync(text);

            string formattedResponse = TelegramMarkdownConverter.Convert(aiAgentResponse);

            var payload = new
            {
                chat_id = chatId,
                text = formattedResponse,
                parse_mode = "MarkdownV2"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var http = _httpClientFactory.CreateClient();
            await http.PostAsync(_tgApiUrl, content);
        }
    }
}