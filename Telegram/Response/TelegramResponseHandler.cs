using System.Net;
using System.Text.Json;

public class TelegramResponseHandler : ITelegramResponseHandler
{
    private readonly ILogger<TelegramResponseHandler> _logger;

    public TelegramResponseHandler(ILogger<TelegramResponseHandler> logger)
    {
        _logger = logger;
    }

    public async Task<TelegramResponse> Parse(HttpResponseMessage httpResponseMessage)
    {
        var responseBody = await httpResponseMessage.Content.ReadAsStringAsync();
        TelegramResponse tgResponse;

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            using var doc = JsonDocument.Parse(responseBody);

            var messageId = doc.RootElement
                      .GetProperty("result")
                      .GetProperty("message_id")
                      .GetInt32();

            tgResponse = new() { MessageId = messageId, IsSuccess = true };
        }
        else
        {
            if (httpResponseMessage.StatusCode == HttpStatusCode.BadRequest)
                tgResponse = new() { IsRequireResend = RequiresResend(responseBody) };
            else
                tgResponse = new();

            _logger.LogError("Telegram API error: {StatusCode} - {Response}", httpResponseMessage.StatusCode, responseBody);
        }
        return tgResponse;
    }

    private bool RequiresResend(string responseBody)
    {
        try
        {
            var tgBadRequestError = JsonSerializer.Deserialize<TelegramBadRequestError?>(responseBody);

            return tgBadRequestError is not null
                    & tgBadRequestError?.Ok == false
                    & tgBadRequestError?.ErrorCode == 400
                    & tgBadRequestError?.Description?.Contains("can't parse entities") == true;
        }
        catch (Exception) { return false; }
    }
}