using System.Collections.Generic;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Assets.Scripts.Telegram.Main
{
    public class ActionSetup
    {
        private readonly AppState appState;
        private readonly ITelegramBotClient botClient;
        
        public ActionSetup(AppState appState, ITelegramBotClient botClient)
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
            buttonsList.Add(new KeyboardButton[] { "Buy"});
            buttonsList.Add(new KeyboardButton[]{ "Sell Transfers", "Sell Other" });
            buttonsList.Add(new KeyboardButton[]{ "Update Anchor By Current Price" });
            buttonsList.Add(new KeyboardButton[]{ "Back" });
            return new ReplyKeyboardMarkup(buttonsList.ToArray()) { ResizeKeyboard = true };
        }
    }
}
