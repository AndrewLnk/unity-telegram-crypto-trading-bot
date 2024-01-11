using System;
using Assets.Scripts.Main.Trading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Assets.Scripts.Telegram.Main
{
    public class UpdateAnchorProcess
    {
        private readonly ITelegramBotClient botClient;
        private readonly string command;
        private readonly ClientProcess clientProcess;
        private Func<long, bool> userLoggedFunc;

        public UpdateAnchorProcess(ITelegramBotClient botClient, ClientProcess clientProcess)
        {
            this.botClient = botClient;
            this.clientProcess = clientProcess;
        }

        public void AddLoginChecker(Func<long, bool> userLogged) => userLoggedFunc = userLogged;

        public async void TryInvoke(Update update)
        {
            if (update.Type == UpdateType.Message)
            {
                if (update.Message == null)
                    return;

                if (!userLoggedFunc.Invoke(update.Message.Chat.Id))
                    return;

                if (update.CallbackQuery?.Message?.Chat != null && !userLoggedFunc.Invoke(update.CallbackQuery.Message.Chat.Id))
                    return;

                if (update.Message?.Text != null && update.Message.Text.Equals("Update Anchor By Current Price"))
                {
                    clientProcess.UpdateAnchor(true);
                    await botClient.SendTextMessageAsync(update.Message.Chat, "Update anchor planned...");
                }
            }
        }
    }
}
