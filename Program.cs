using System.Globalization;
using Telegram.Bot;

namespace QRCodeTelegram
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var apiKey = string.Empty;

            if (string.IsNullOrEmpty(args.FirstOrDefault()))
                apiKey = Environment.GetEnvironmentVariable("API_KEY")!;
            else
                apiKey = args.First();

            TelegramClientHandler.TelegramClient =
                new TelegramBotClient(apiKey);

            TelegramClientHandler.Start();

            Console.WriteLine("SkiaQrBot run!");
            while (true) ;
        }
    }
}
