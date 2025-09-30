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
        _tgApiUrl = $"https://api.telegram.org/bot{_tgToken}/sendMessage";
    }

    public async Task HandleUpdateAsync(HttpRequest request, IAiAgentService aiAgentService)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        string response = "";
        if (root.TryGetProperty("message", out var message))
        {
            long chatId = message.GetProperty("chat").GetProperty("id").GetInt64();
            long fromId = message.GetProperty("from").GetProperty("id").GetInt64();
            string text = message.GetProperty("text").GetString() ?? "";

            if (fromId == _tgAdminId)
            {
                _logger.LogInformation("Message from {FromId}: {Text}", fromId, text);

                var aiAgentResponse = await aiAgentService.AskAsync(text);
                response = TelegramMarkdownConverter.Convert(aiAgentResponse);
            }
            else
            {
                _logger.LogWarning("Unauthorized access attempt from {FromId}: {Text}", fromId, text);
                response = "Access denied";
            }

            var payload = new
            {
                chat_id = chatId,
                text = response,
                parse_mode = "MarkdownV2"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var http = _httpClientFactory.CreateClient();
            await http.PostAsync(_tgApiUrl, content);
        }
    }
}