public interface ITelegramCommandHandler
{
    bool IsCommand(string message);
    Task<string> HandleCommandAsync(long chatId, string message);
}