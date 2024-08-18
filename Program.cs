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
        var offset = 0;
        var apiKey = Environment.GetEnvironmentVariable("API_KEY")!;
        var telegramBot = new TelegramBotClient(apiKey);
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Console.WriteLine("bot run!");
        await telegramBot.DeleteWebhookAsync();

        while (true)
        {
            var updates = await telegramBot.GetUpdatesAsync(offset);
            await Task.Delay(TimeSpan.FromSeconds(5));

            foreach (var update in updates)
            {
                if (update is null || update.Message is null)
                    continue;

                if (update.Message.Type == MessageType.Text)
                {
                    if (string.IsNullOrEmpty(update.Message.Text))
                        return;

                    var generator = new QRCodeGenerator();
                    var encodedData = Encoding.UTF8.GetBytes(update.Message.Text);
                    var qrData = generator.CreateQrCode(encodedData, QRCodeGenerator.ECCLevel.L);
                    var qrCode = new PngByteQRCode(qrData).GetGraphic(5);
                    var inputFile = InputFile.FromStream(new MemoryStream(qrCode), "qrCode.png");

                    await telegramBot.SendPhotoAsync(update.Message.Chat, inputFile);
                }

                if (update.Message.Type == MessageType.Photo)
                {
                    if (update.Message.Photo == null)
                        return;

                    var photo = update.Message.Photo.Last();

                    await using (var ms = new MemoryStream())
                    {
                        var file = await telegramBot.GetInfoAndDownloadFileAsync(photo.FileId, ms);
                        var btm = SKBitmap.Decode(ms.ToArray());
                        var reader = new ZXing.SkiaSharp.BarcodeReader();
                        var result = reader.Decode(btm);

                        await telegramBot.SendTextMessageAsync(update.Message.Chat, result?.Text ?? "QR code is not identified");
                    }
                }

                offset = update.Id + 1;
            }
        }
    }
}
