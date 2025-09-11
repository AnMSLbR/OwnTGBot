public interface IAiAgentService
{
    Task<string> GetResponseAsync(string prompt);
}