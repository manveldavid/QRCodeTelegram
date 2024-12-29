using QRCoder;
using SkiaSharp;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text;

namespace QRCodeTelegram;

public class TelegramBot
{
    public async Task RunAsync(string apiKey, TimeSpan pollPeriod, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(apiKey))
            return;

        var offset = 0;
        var telegramBot = new TelegramBotClient(apiKey);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(pollPeriod, cancellationToken);

            Update[] updates = Array.Empty<Update>();
            try
            {
                updates = await telegramBot.GetUpdates(offset, timeout: (int)pollPeriod.TotalSeconds, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                foreach (var update in updates)
                {
                    offset = update.Id + 1;

                    if (update is null || update.Message is null)
                        continue;

                    switch (update.Message.Type)
                    {
                        case MessageType.Text:
                                if (string.IsNullOrEmpty(update.Message.Text))
                                    return;

                                var qrCode = new PngByteQRCode(new QRCodeGenerator().CreateQrCode(Encoding.UTF8.GetBytes(update.Message.Text), QRCodeGenerator.ECCLevel.L)).GetGraphic(5);
                                var inputFile = InputFile.FromStream(new MemoryStream(qrCode), "qrCode.png");

                                await telegramBot.SendPhoto(update.Message.Chat, inputFile, replyParameters: new ReplyParameters { MessageId = update.Message.Id });
                            break;
                        case MessageType.Photo:
                                if (update.Message.Photo == null)
                                    return;

                                await using (var ms = new MemoryStream())
                                {
                                    var file = await telegramBot.GetInfoAndDownloadFile(update.Message.Photo.First().FileId, ms);
                                    var decoder = new ZXing.SkiaSharp.BarcodeReader()
                                    {
                                        AutoRotate = true,
                                        Options = new ZXing.Common.DecodingOptions()
                                        { 
                                            TryHarder = true,
                                            TryInverted = true
                                        }
                                    };
                                    var result = decoder.Decode(SKBitmap.Decode(ms.ToArray()));

                                    await telegramBot.SendMessage(update.Message.Chat, result?.Text ?? "QR code is not identified", replyParameters: new ReplyParameters { MessageId = update.Message.Id });
                                }
                            break;
                    }
                }
            }
        }
    }
}
