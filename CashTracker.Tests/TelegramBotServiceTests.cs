using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class TelegramBotServiceTests
    {
        [Fact]
        public async Task GetUpdatesAsync_ParsesPhotoCaptionAndMessageId()
        {
            var handler = new RecordingHttpMessageHandler((request, _) =>
            {
                var url = request.RequestUri?.ToString() ?? string.Empty;
                if (url.Contains("/getUpdates", StringComparison.OrdinalIgnoreCase))
                {
                    return RecordingHttpMessageHandler.OkJson(
                        "{\"ok\":true,\"result\":[{\"update_id\":77,\"message\":{\"message_id\":55,\"chat\":{\"id\":123},\"from\":{\"id\":42},\"caption\":\"market fis\",\"photo\":[{\"file_id\":\"small\",\"width\":90,\"height\":90,\"file_size\":200},{\"file_id\":\"large\",\"width\":800,\"height\":1200,\"file_size\":4000}]}}]}");
                }

                return RecordingHttpMessageHandler.OkJson("{\"ok\":true,\"result\":[]}");
            });

            var bot = new TelegramBotService(new HttpClient(handler), "test-token");
            var updates = await bot.GetUpdatesAsync();

            var update = Assert.Single(updates);
            Assert.Equal(77, update.UpdateId);
            Assert.Equal(55, update.MessageId);
            Assert.Equal(123, update.ChatId);
            Assert.Equal(42, update.UserId);
            Assert.Equal("market fis", update.Caption);
            Assert.True(update.HasPhoto);
            Assert.Equal("large", update.PhotoFileId);
        }

        [Fact]
        public async Task GetFilePathAndDownloadFileAsync_UseTelegramFileEndpoints()
        {
            var handler = new RecordingHttpMessageHandler((request, _) =>
            {
                var url = request.RequestUri?.ToString() ?? string.Empty;
                if (url.Contains("/getFile", StringComparison.OrdinalIgnoreCase))
                {
                    return RecordingHttpMessageHandler.OkJson("{\"ok\":true,\"result\":{\"file_path\":\"photos/receipt.jpg\"}}");
                }

                if (url.Contains("/file/bottest-token/photos/receipt.jpg", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(new byte[] { 9, 8, 7 })
                    };
                }

                return RecordingHttpMessageHandler.OkJson("{\"ok\":true,\"result\":{}}");
            });

            var bot = new TelegramBotService(new HttpClient(handler), "test-token");
            var filePath = await bot.GetFilePathAsync("photo-1");
            Assert.Equal("photos/receipt.jpg", filePath);

            var tempPath = Path.Combine(Path.GetTempPath(), $"telegram_test_{Guid.NewGuid():N}.jpg");
            try
            {
                await bot.DownloadFileAsync(filePath, tempPath);
                Assert.True(File.Exists(tempPath));
                Assert.Equal(new byte[] { 9, 8, 7 }, await File.ReadAllBytesAsync(tempPath));
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }

        [Fact]
        public async Task SendTextAsync_WithSettings_UsesLatestBotToken()
        {
            var handler = new RecordingHttpMessageHandler();
            var settings = new TelegramSettings { BotToken = "111:first-token" };
            var bot = new TelegramBotService(new HttpClient(handler), settings);

            await bot.SendTextAsync("123", "ilk");
            settings.BotToken = "222:second-token";
            await bot.SendTextAsync("123", "ikinci");

            Assert.Contains("/bot111:first-token/sendMessage", handler.Requests[0].Url);
            Assert.Contains("/bot222:second-token/sendMessage", handler.Requests[1].Url);
        }

        [Fact]
        public async Task GetMeAsync_ParsesBotIdentity()
        {
            var handler = new RecordingHttpMessageHandler((request, _) =>
            {
                var url = request.RequestUri?.ToString() ?? string.Empty;
                if (url.Contains("/getMe", StringComparison.OrdinalIgnoreCase))
                {
                    return RecordingHttpMessageHandler.OkJson(
                        "{\"ok\":true,\"result\":{\"id\":987,\"is_bot\":true,\"first_name\":\"Systemcel\",\"username\":\"SystemcelBot\"}}");
                }

                return RecordingHttpMessageHandler.OkJson("{\"ok\":true,\"result\":{}}");
            });

            var bot = new TelegramBotService(new HttpClient(handler), "test-token");
            var identity = await bot.GetMeAsync();

            Assert.Equal(987, identity.Id);
            Assert.Equal("SystemcelBot", identity.Username);
            Assert.Equal("Systemcel", identity.FirstName);
        }

        [Fact]
        public async Task SetCommandsAsync_PostsTelegramCommandMenu()
        {
            var handler = new RecordingHttpMessageHandler();
            var bot = new TelegramBotService(new HttpClient(handler), "test-token");

            await bot.SetCommandsAsync(TelegramCommandCatalog.BotMenu);

            var request = Assert.Single(handler.Requests);
            Assert.Contains("/setMyCommands", request.Url);
            Assert.Contains("\"command\":\"yardim\"", request.Body);
            Assert.Contains("\"command\":\"bugun\"", request.Body);
        }
    }
}
