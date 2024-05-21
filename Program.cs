using Telegram.Bot;

namespace QRCodeTelegram
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TelegramClientHandler.TelegramClient =
                new TelegramBotClient("your bot id");
            TelegramClientHandler.Start();

            Console.WriteLine("SkiaQrBot run!");
            while (true) ;
        }
    }
}
