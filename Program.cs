using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddScoped<ITelegramService, TelegramService>();
builder.Services.AddScoped<IAiAgentService, GptAgentService>();

var autoSetWebhook = builder.Configuration.GetValue<bool>("AutoSetWebhook");
if (autoSetWebhook)
{
    builder.Services.AddSingleton<WebhookInitializer>();
    builder.Services.AddHostedService<WebhookHostedService>();
}

var app = builder.Build();

app.MapPost("/webhook", async (
    [FromServices] ITelegramService telegramService,
    [FromServices] IAiAgentService chatGptService,
    HttpRequest request) =>
{
    await telegramService.HandleUpdateAsync(request, chatGptService);
    return Results.Ok();
});

app.Run();
