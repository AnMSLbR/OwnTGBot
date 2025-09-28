using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

public class WebhookInitializer
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<TelegramService> _logger;
    private readonly string _tgToken;

    public WebhookInitializer(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<TelegramService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
        _tgToken = _config["TgBotToken"] ?? "";
    }

    public async Task InitWebhookAsync()
    {
        var urls = _config["ASPNETCORE_URLS"]?.Split(';', StringSplitOptions.RemoveEmptyEntries)
                           ?? new[] { "http://localhost:5000" };

        var uri = new Uri(urls.First());
        var port = uri.Port;

        string ltUrl = await GetLocalTunnelUrl(port);
        string webhookUrl = $"{ltUrl}/webhook";

        string apiUrl = $"https://api.telegram.org/bot{_tgToken}/setWebhook?url={webhookUrl}";

        var http = _httpClientFactory.CreateClient();
        var response = await http.GetStringAsync(apiUrl);

        var json = JsonDocument.Parse(response);
        if (json.RootElement.GetProperty("ok").GetBoolean())
            _logger.LogInformation("Webhook was set: {WebhookUrl}", webhookUrl);
        else
            _logger.LogInformation("Webhook setting error: {Response}", response);
    }

    private async Task<string> GetLocalTunnelUrl(int port)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c npx localtunnel --port {port}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new Exception("Failed to start LocalTunnel");

        _logger.LogInformation("Process {FileName} with arguments \"{Arguments}\" was started successfully.", psi.FileName,psi.Arguments);

        string? url = null;
        while (!process.StandardOutput.EndOfStream)
        {
            var line = await process.StandardOutput.ReadLineAsync();
            if (line != null && line.Contains("your url is:"))
            {
                url = line.Split("your url is:")[1].Trim();
                break;
            }
        }

        if (url == null)
            throw new Exception("Failed to get URL");

        return url;
    }
}
