public interface IAiAgentService
{
    Task<string> AskAsync(string userMessage);
}