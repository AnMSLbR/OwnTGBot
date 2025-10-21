using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class TelegramResponse
{
    public int? MessageId { get; init; }
    public bool IsSuccess { get; init; } = false;
    public bool IsRequireResend { get; init; } = false;
}