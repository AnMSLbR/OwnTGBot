public class TelegramCommandHandler : ITelegramCommandHandler
{
    private readonly ILogger<TelegramCommandHandler> _logger;
    private readonly string _adminUsername = "admin";

    public TelegramCommandHandler(ILogger<TelegramCommandHandler> logger)
    {
        _logger = logger;
    }

    public bool IsCommand(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        return message.StartsWith('/');
    }

    public async Task<string> HandleCommandAsync(long chatId, string message)
    {
        try
        {
            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLowerInvariant();
            var args = parts.Skip(1).ToArray();

            _logger.LogInformation("Executing command {Command} from chat {ChatId}", command, chatId);

            return command switch
            {
                "/start" => await HandleStartCommand(chatId, args),
                _ => $"Unknown command"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while executing command {Message} from chat {ChatId}", message, chatId);
            return String.Empty;
        }
    }

    private Task<string> HandleStartCommand(long chatId, string[] args)
    {
        return Task.FromResult($"Please contact @{_adminUsername.TrimStart('@')} for access");
    }
}
