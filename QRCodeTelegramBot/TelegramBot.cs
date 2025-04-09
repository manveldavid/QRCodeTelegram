using QRCoder;
using SkiaSharp;
using System.Text;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using ZXing;
using static System.Net.Mime.MediaTypeNames;

namespace QRCodeTelegramBot
{
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
                        try
                        {
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

                                    SKBitmap image = default!;
                                    await using (var ms = new MemoryStream())
                                    {
                                        var file = await telegramBot.GetInfoAndDownloadFile(update.Message.Photo.Last().FileId, ms);
                                        ms.Position = 0;
                                        image = SKBitmap.Decode(ms);
                                    }

                                    var decoder = new ZXing.SkiaSharp.BarcodeReader();
                                    var result = decoder.Decode(image);

                                    if (result?.Text is null)
                                    {
                                        decoder.AutoRotate = true;
                                        decoder.Options = new ZXing.Common.DecodingOptions()
                                        {
                                            TryHarder = true,
                                            TryInverted = true
                                        };

                                        var contrast = 2.5f;
                                        var blurSigma = 2.5f;
                                        int width = image.Width;
                                        int height = image.Height;

                                        for (int y = 0; y < height; y++)
                                        {
                                            for (int x = 0; x < width; x++)
                                            {
                                                SKColor color = image.GetPixel(x, y);
                                                byte gray = (byte)(0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue);
                                                image.SetPixel(x, y, new SKColor(gray, gray, gray));
                                            }
                                        }

                                        SKBitmap processed = new SKBitmap(width, height);

                                        using (var canvas = new SKCanvas(processed))
                                        using (var paint = new SKPaint() 
                                        { 
                                            ColorFilter = SKColorFilter.CreateHighContrast(true, SKHighContrastConfigInvertStyle.NoInvert, contrast),
                                            ImageFilter = SKImageFilter.CreateBlur(blurSigma, blurSigma),
                                        })
                                            canvas.DrawBitmap(image, new SKRect(0, 0, processed.Width, processed.Height), paint);

                                        result = decoder.Decode(processed);
                                    }

                                    await telegramBot.SendMessage(update.Message.Chat, result?.Text ?? "QR code is not identified", replyParameters: new ReplyParameters { MessageId = update.Message.Id });

                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
            }
        }
    }
}
