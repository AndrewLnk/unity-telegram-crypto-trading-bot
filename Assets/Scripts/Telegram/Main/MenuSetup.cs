using System.Collections.Generic;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Assets.Scripts.Telegram.Main
{
    public class MenuSetup
    {
        private readonly AppState appState;
        private readonly ITelegramBotClient botClient;
        public MenuSetup(AppState appState, ITelegramBotClient botClient)
        {
            this.appState = appState;
            this.botClient = botClient;
        }
        
        public async void AddMenu(long chatId, CancellationToken cancellationToken, string message)
        {
            await botClient.SendTextMessageAsync(chatId, message,
                replyMarkup: GetMenu(),
                cancellationToken: cancellationToken);
        }
        
        public async void RemoveMenu(long chatId, CancellationToken cancellationToken, string message)
        {
            await botClient.SendTextMessageAsync(chatId, message,
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }

        private ReplyKeyboardMarkup GetMenu()
        {
            var buttonsList = new List<KeyboardButton[]>();
            if (!appState.Initialized)
            {
                buttonsList.Add(new KeyboardButton[]{"Check if initialized"});
            }
            else
            {
                buttonsList.Add(new KeyboardButton[]
                {
                    appState.Active ? "Stop" : "Start",
                    appState.WatchLog ? "Silent" : "Watch"
                });
            }
            
            buttonsList.Add(new KeyboardButton[] { "App Log" });
            buttonsList.Add(new KeyboardButton[]{ "Actions" });
            buttonsList.Add(new KeyboardButton[]{ "Settings" });
            return new ReplyKeyboardMarkup(buttonsList.ToArray()) { ResizeKeyboard = true };
        }
    }
}
