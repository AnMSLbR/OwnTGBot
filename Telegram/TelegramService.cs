using System.Text;
using System.Text.Json;

public class TelegramService : ITelegramService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramService> _logger;
    private readonly string _tgToken;
    private readonly string _tgApiUrl;
    private readonly long _tgAdminId;

    public TelegramService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<TelegramService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _tgToken = _config["TgBotToken"] ?? "";
        long.TryParse(_config["TgUserId"], out _tgAdminId);
        _tgApiUrl = $"https://api.telegram.org/bot{_tgToken}";
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
            long fromId = message.GetProperty("from").GetProperty("id").GetInt64();
            string text = message.GetProperty("text").GetString() ?? "";

            if (fromId != _tgAdminId)
            {
                _logger.LogWarning("Unauthorized access attempt from {FromId}: {Text}", fromId, text);
                await SendMessageAsync(chatId, "Access denied");
            }
            else
            {
                _logger.LogInformation("Message from {FromId}: {Text}", fromId, text);

                var pendingMessageId = await SendMessageAsync(chatId, "Processing your request...");

                var aiAgentResponse = await aiAgentService.AskAsync(text);
                // string formattedResponse = TelegramMarkdownConverter.Convert(aiAgentResponse);

                await EditMessageAsync(chatId, pendingMessageId, aiAgentResponse);
            }
        }
    }

    private async Task<int> SendMessageAsync(long chatId, string text)
    {
        var payload = new
        {
            chat_id = chatId,
            text,
            // parse_mode = "MarkdownV2"
        };

        return await PostToTelegramAsync("sendMessage", payload);
    }

    private async Task<int> EditMessageAsync(long chatId, int messageId, string newText)
    {
        var payload = new
        {
            chat_id = chatId,
            message_id = messageId,
            text = newText,
            // parse_mode = "MarkdownV2"
        };

        return await PostToTelegramAsync("editMessageText", payload);
    }

    private async Task<int> PostToTelegramAsync(string method, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var http = _httpClientFactory.CreateClient();
        var response = await http.PostAsync($"{_tgApiUrl}/{method}", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        return doc.RootElement
                  .GetProperty("result")
                  .GetProperty("message_id")
                  .GetInt32();
    }
}