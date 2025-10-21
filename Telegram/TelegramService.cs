using System.Dynamic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class TelegramService : ITelegramService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramService> _logger;
    private readonly ITelegramCommandHandler _commandHandler;
    private readonly ITelegramResponseHandler _tgResponseHandler;
    private readonly string _tgToken;
    private readonly string _tgApiUrl;
    private readonly long _tgAdminId;

    public TelegramService(IConfiguration config,
                            IHttpClientFactory httpClientFactory,
                            ILogger<TelegramService> logger,
                            ITelegramCommandHandler commandHandler,
                            ITelegramResponseHandler tgResponseHandler)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _commandHandler = commandHandler;
        _tgResponseHandler = tgResponseHandler;
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
            int unixTime = message.GetProperty("date").GetInt32();
            var messageTime = DateTimeOffset.FromUnixTimeSeconds(unixTime).ToLocalTime();

            var now = DateTimeOffset.Now;

            if (now - messageTime > TimeSpan.FromMinutes(3))
            {
                _logger.LogInformation(
                    "Ignoring outdated message from {FromId} sent at {MessageTime:yyyy-MM-dd HH:mm:ss}: {Text}",
                    fromId, messageTime, text);
                return;
            }

            if (_commandHandler.IsCommand(text))
            {
                var response = await _commandHandler.HandleCommandAsync(chatId, text);
                if (String.IsNullOrEmpty(response)) return;
                await SendMessageAsync(chatId, response, ParseMode.None);
            }
            else
            {
                if (fromId != _tgAdminId)
                {
                    _logger.LogWarning("Unauthorized access attempt from {FromId}: {Text}", fromId, text);
                    await SendMessageAsync(chatId, "Access denied");
                }
                else
                {
                    _logger.LogInformation("Message from {FromId}: {Text}", fromId, text);

                    var pendingResponse = await SendMessageAsync(chatId, "Processing your request...", ParseMode.None);

                    if (pendingResponse.IsSuccess)
                    {
                        var aiAgentResponse = await aiAgentService.AskAsync(text);

                        var response = await EditMessageAsync(chatId, (int)pendingResponse.MessageId!, aiAgentResponse);

                        if (response.IsRequireResend)
                            await EditMessageAsync(chatId, (int)pendingResponse.MessageId!, aiAgentResponse, ParseMode.None);
                    }
                }
            }
        }
    }

    private async Task<TelegramResponse> SendMessageAsync(long chatId, string text, ParseMode parseMode = ParseMode.MarkdownV2)
    {
        dynamic payload = new ExpandoObject();
        payload.chat_id = chatId;
        payload.text = text;

        if (parseMode is not ParseMode.None)
            payload.parse_mode = parseMode;

        return await PostToTelegramAsync("sendMessage", payload);
    }

    private async Task<TelegramResponse> EditMessageAsync(long chatId, int messageId, string newText, ParseMode parseMode = ParseMode.MarkdownV2)
    {
        dynamic payload = new ExpandoObject();
        payload.chat_id = chatId;
        payload.message_id = messageId;
        payload.text = newText;

        if (parseMode is not ParseMode.None)
            payload.parse_mode = parseMode;

        return await PostToTelegramAsync("editMessageText", payload);
    }

    private async Task<TelegramResponse> PostToTelegramAsync(string method, object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var http = _httpClientFactory.CreateClient();
        var response = await http.PostAsync($"{_tgApiUrl}/{method}", content);

        return await _tgResponseHandler.Parse(response);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
enum ParseMode
{
    [EnumMember(Value = "Markdown")]
    Markdown,

    [EnumMember(Value = "HTML")]
    Html,

    [EnumMember(Value = "MarkdownV2")]
    MarkdownV2,
    None
}