using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

public class WebhookInitializer
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly string _tgToken;

    public WebhookInitializer(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _tgToken = _config["TgBotToken"] ?? throw new InvalidOperationException("No TgBotToken in config");
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
            Console.WriteLine($"Webhook was set: {webhookUrl}");
        else
            Console.WriteLine($"Webhook setting error: {response}");
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

        using var process = Process.Start(psi);
        if (process == null) throw new Exception("Failed to start LocalTunnel");

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
