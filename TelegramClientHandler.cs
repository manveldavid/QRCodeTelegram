using QRCoder;
using SkiaSharp;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace QRCodeTelegram
{
    public static class TelegramClientHandler
    {
        public static TelegramBotClient TelegramClient { get; set; } = null!;

        public static void Start()
        {
            var resiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new UpdateType[]
                {
                    UpdateType.Message,
                    UpdateType.EditedMessage
                }
            };

            if (TelegramClient == null) { throw new ArgumentNullException("TelegramClient is null"); }

            TelegramClient.StartReceiving(UpdateHandler, ErrorHandler, resiverOptions);
        }
        private static Task ErrorHandler(
            ITelegramBotClient telegramClient, Exception ex, CancellationToken cancellationToken)
        {
            return Task.Run(
                () => Console.WriteLine(ex),
                cancellationToken);
        }

        private static async Task UpdateHandler(
            ITelegramBotClient telegramClient, Update update, CancellationToken cancellationToken)
        {
            var message = update.Message;
            if (message == null) return;
            if (message.Type == MessageType.Text)
            {
                await TextHandle(message.Text, message.Chat, cancellationToken);
            }
            if(message.Type == MessageType.Photo)
            {
                await PhotoHandle(message.Photo, message.Chat, cancellationToken);
            }
        }

        public static async Task TextHandle(string? text, Chat chat, CancellationToken cancellationToken)
        {
            if(string.IsNullOrEmpty(text)) return;
            var generator = new QRCodeGenerator();
            var encodedData = Encoding.UTF8.GetBytes(text);
            var qrData = generator.CreateQrCode(encodedData, QRCodeGenerator.ECCLevel.L);
            var qrCode = new PngByteQRCode(qrData).GetGraphic(5);
            var inputFile = InputFile.FromStream(new MemoryStream(qrCode), "qrCode.png");

            await TelegramClient.SendPhotoAsync(chat, inputFile, cancellationToken:cancellationToken);
        }

        public static async Task PhotoHandle(PhotoSize[]? photos, Chat chat, CancellationToken cancellationToken)
        {
            if (photos == null) return;

            var photo = photos.Last();

            await using (var ms = new MemoryStream())
            {
                var file = await TelegramClient.GetInfoAndDownloadFileAsync(photo.FileId, ms, cancellationToken);
                var btm = SKBitmap.Decode(ms.ToArray());
                var reader = new ZXing.SkiaSharp.BarcodeReader();
                var result = reader.Decode(btm);

                await TelegramClient.SendTextMessageAsync(chat, result?.Text ?? "QR code is not identified", cancellationToken: cancellationToken);
            }
        }
    }
}