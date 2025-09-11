public interface ITelegramService
{
    Task HandleUpdateAsync(HttpRequest request, IAiAgentService chatGptService);
}