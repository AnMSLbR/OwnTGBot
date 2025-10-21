public interface ITelegramResponseHandler
{
    public Task<TelegramResponse> Parse(HttpResponseMessage httpResponseMessage);
}