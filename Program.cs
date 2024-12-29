using QRCoder;
using SkiaSharp;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text;
using System.Globalization;

namespace QRCodeTelegram;

public class Program
{
    public static async Task Main(string[] args)
    {
        var tgBotPollPeriodInSeconds = TimeSpan.FromSeconds(double.TryParse(Environment.GetEnvironmentVariable("TG_BOT_POLL_PERIOD_SECONDS"), out var _tgBotPollPeriodInSeconds) ? _tgBotPollPeriodInSeconds : 10d);
        var apiKey = Environment.GetEnvironmentVariable("API_KEY")!;

        Console.WriteLine("bot run!");

        await Task.WhenAll([
                new TelegramBot().RunAsync(
                    apiKey,
                    tgBotPollPeriodInSeconds,
                    CancellationToken.None)
            ]);
    }
}
