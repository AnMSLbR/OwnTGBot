public class WebhookHostedService : IHostedService
{
    private readonly WebhookInitializer _initializer;

    public WebhookHostedService(WebhookInitializer initializer)
    {
        _initializer = initializer;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _initializer.InitWebhookAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
