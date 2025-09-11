public class GptAgentService : IAiAgentService
{
    public async Task<string> GetResponseAsync(string prompt)
    {
        await Task.CompletedTask;
        return $"(ChatGPT response: {prompt})";
    }
}