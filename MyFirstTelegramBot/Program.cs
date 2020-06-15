using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json;
using System.Collections.Generic;
using static MyFirstTelegramBot.WeatherConfiguration;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using File = System.IO.File;
using Telegram.Bot.Requests;

namespace MyFirstTelegramBot
{
    class Program
    {
        private static TelegramBotClient Bot;

        public bool a = true;
        public static async Task Main()
        {

            //var Proxy = new WebProxy(Configuration.Proxy.Host, Configuration.Proxy.Port) { UseDefaultCredentials = true };
            //Bot = new TelegramBotClient(Configuration.BotToken, webProxy: Proxy);
            Bot = new TelegramBotClient(Configuration.BotToken);

            var me = await Bot.GetMeAsync();
            Console.Title = me.Username;
            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            Bot.OnReceiveError += BotOnReceiveError;
            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
            Bot.StopReceiving();
        }


        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message == null || message.Type != MessageType.Text)
                return;
            else
            {
                switch (message.Text.Split(' ').First())
                {
                    // Send inline keyboard
                    case "/inline@Shinji_bot":
                        await SendInlineKeyboard(message);
                        break;
                    case "/EditSendImage@Shinji_bot":
                        await editUserPhoto(message);
                        break;
                    // send a photo
                    case "/photo@Shinji_bot":
                        await SendDocument(message);
                        break;
                    // request location or contact
                    case "/request@Shinji_bot":
                        await RequestContactAndLocation(message);
                        break;
                    case "/weather@Shinji_bot":
                        await SendWeatherMoscow(message);
                        break;
                    case "/help@Shinji_bot":
                        await Usage(message);
                        break;
                }

            }


            // Send inline keyboard
            // You can process responses in BotOnCallbackQueryReceived handler
            static async Task SendInlineKeyboard(Message message)
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
                // Simulate longer running task
                await Task.Delay(500);

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Site AzurLane", "https://azurlane.koumakan.jp/Azur_Lane_Wiki"),
                        InlineKeyboardButton.WithCallbackData("Weather in Moscow", "https://yandex.ru/pogoda/moscow"),
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("Twitter", "https://twitter.com/home"),

                    }
                });
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Choose",
                    replyMarkup: inlineKeyboard
                );
            }


            static async Task editUserPhoto(Message message)
            {

                await Bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Send some image.");
                Bot.OnUpdate += BotOnMessageUpdate;
                static async void BotOnMessageUpdate(object sender, UpdateEventArgs updateEventArgs)
                {

                    var message = updateEventArgs.Update.Message;
                    if (message == null || message.Type != MessageType.Photo)
                        return;
                    else
                        await SendEditImage(message);
                    static async Task SendEditImage(Message message)
                    {
                        try
                        {
                            await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);
                            if (message == null || message.Type != MessageType.Text)
                            {
                                var file_info = await Bot.GetFileAsync(message.Photo.LastOrDefault().FileId);
                                string fileName = file_info.FileId + "." + file_info.FilePath.Split('.').Last();

                                using (var saveImageStream = File.Open(@"C:\ImageTelegram\" + fileName, FileMode.Create))
                                {
                                    await Bot.DownloadFileAsync(file_info.FilePath, saveImageStream);
                                }
                            }
                            else
                            {
                                await Bot.SendTextMessageAsync(chatId: message.Chat.Id, text: "Error! Send some image.");
                            }

                            await Bot.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: "Downloaded"
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error downloading: " + ex.Message);
                        }
                    }
                }
            }

            //weather sender
            static async Task SendWeatherMoscow(Message message)
            {
                WebRequest request = HttpWebRequest.Create("https://gridforecast.com/api/v1/forecast/55.7558;37.6173/202005221200?api_token=MAk14KAmyFF9X6SG");
                WebResponse response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string content = reader.ReadToEnd();
                DataWeather dataWeather = Newtonsoft.Json.JsonConvert.DeserializeObject<DataWeather>(content);
                await Bot.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Temperature in Moscow: {Convert.ToString(dataWeather.t)} Apparent temperature: {Convert.ToString(dataWeather.aptmp)}");
            }

            static async Task SendDocument(Message message)
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

                const string filePath = @"C:\Users\camic\Desktop\Study\C#\MyFirstTelegramBot\MyFirstTelegramBot\Shinji Zelenski.png";
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();
                await Bot.SendPhotoAsync(
                    chatId: message.Chat.Id,
                    photo: new InputOnlineFile(fileStream, fileName),
                    caption: "Nice Picture"
                );
            }

            static async Task RequestContactAndLocation(Message message)
            {
                var RequestReplyKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    KeyboardButton.WithRequestLocation("Location"),
                    KeyboardButton.WithRequestContact("Contact"),
                });
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Who or Where are you?",
                    replyMarkup: RequestReplyKeyboard
                );
            }
            //help
            static async Task Usage(Message message)
            {
                const string usage = "Usage:\n" +
                                        "/inline   - send inline keyboard\n" +
                                        "/photo    - send a photo\n" +
                                        "/request  - request location or contact";
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: usage,
                    replyMarkup: new ReplyKeyboardRemove()
                );
            }
        }

        // Process Inline Keyboard callback data
        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;

            await Bot.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Received {callbackQuery.Data}"
            );

            await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: $"Received {callbackQuery.Data}"
            );
        }

        #region Inline Mode

        private static async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            Console.WriteLine($"Received inline query from: {inlineQueryEventArgs.InlineQuery.From.Id}");

            InlineQueryResultBase[] results = {
                // displayed result
                new InlineQueryResultArticle(
                    id: "3",
                    title: "TgBots",
                    inputMessageContent: new InputTextMessageContent(
                        "hello"
                    )
                )
            };
            await Bot.AnswerInlineQueryAsync(
                inlineQueryId: inlineQueryEventArgs.InlineQuery.Id,
                results: results,
                isPersonal: true,
                cacheTime: 0
            );
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        #endregion

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message
            );
        }
    }
}
