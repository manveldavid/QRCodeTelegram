using System.Globalization;
using Telegram.Bot;

namespace QRCodeTelegram
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var apiKey = Environment.GetEnvironmentVariable("API_KEY");

            TelegramClientHandler.TelegramClient =
                new TelegramBotClient(apiKey ?? throw new Exception(nameof(apiKey)));

            TelegramClientHandler.Start();

            Console.WriteLine("SkiaQrBot run!");
            while (true) ;
        }
    }
}
