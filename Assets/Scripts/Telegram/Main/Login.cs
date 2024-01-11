using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.CoreAnalyzer.Sync.Telegram;
using Assets.Scripts.Main.Trading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UnityEngine;

namespace Assets.Scripts.Telegram.Main
{
    public class Login
    {
        private const string firebaseAllowedFileName = "LoginAllowed";
        private static readonly List<long> WaitingForLogin = new List<long>();
        private static readonly List<long> Logged = new List<long>(){};
        private readonly MenuSetup menuSetup;
        private readonly ClientProcess clientProcess;
        private readonly AppState appState;

        public Login(MenuSetup menuSetup, ClientProcess clientProcess, AppState appState)
        {
            this.menuSetup = menuSetup;
            this.clientProcess = clientProcess;
            this.appState = appState;
        }
        
        public async void TryLogin(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if(update.Type == UpdateType.Message)
            {
                if (update.Message?.Text == null)
                    return;

                // Login part
                if (update.Message.Text.Equals("/start"))
                {
                    WaitingForLogin.Add(update.Message.Chat.Id);
                    await botClient.SendTextMessageAsync(update.Message.Chat,"Enter Login Password", cancellationToken: cancellationToken);
                    return;
                }

                // Logout part
                if (update.Message.Text.Equals("/logout") && Logged.Contains(update.Message.Chat.Id))
                {
                    Logged.Remove(update.Message.Chat.Id);
                    TelegramNotifySync.Logged.Remove(update.Message.Chat.Id);
                    WaitingForLogin.Remove(update.Message.Chat.Id);
                    menuSetup.RemoveMenu(update.Message.Chat.Id, cancellationToken, "Logout Successful");
                    LogoutIds(update.Message.Chat.Id);
                    return;
                }
                
                if (update.Message.Text.Equals("/start") || update.Message.Text.Equals("/logout"))
                    return;
                
                // Check for password
                if (update.Message.Text.Equals(UserData.TelegramLoginPassword) && WaitingForLogin.Contains(update.Message.Chat.Id))
                {
                    if (!Logged.Contains(update.Message.Chat.Id)) Logged.Add(update.Message.Chat.Id);
                    if (!TelegramNotifySync.Logged.Contains(update.Message.Chat.Id)) TelegramNotifySync.Logged.Add(update.Message.Chat.Id);
                    WaitingForLogin.Remove(update.Message.Chat.Id);
                    menuSetup.AddMenu(update.Message.Chat.Id, cancellationToken, "Login Success");
                    await botClient.SendTextMessageAsync(update.Message.Chat,
                          $"Status: {appState.Active}" +
                          $"\n{clientProcess.GetWalletsInfo()}" +
                          "\n"+
                          $"\nPrice: {clientProcess.GetLastPrice():0.0000}" +
                          $"\nAnchor: {clientProcess.GetBuyAnchorPrice():0.0000}" +
                          $"\nExpire: {clientProcess.GetBuyAnchorExpiration()}" +
                          $"\nReady Buy: {clientProcess.GetBuyAbsolutePrice():0.0000}" +
                          $"\n{clientProcess.GetTransactionsLog()}", 
                        cancellationToken: cancellationToken);
                    return;
                }
                
                // Check for wrong password
                if (!update.Message.Text.Equals(UserData.TelegramLoginPassword) && WaitingForLogin.Contains(update.Message.Chat.Id))
                {
                    Logged.Remove(update.Message.Chat.Id);
                    TelegramNotifySync.Logged.Remove(update.Message.Chat.Id);
                    WaitingForLogin.Remove(update.Message.Chat.Id);
                    await botClient.SendTextMessageAsync(update.Message.Chat,"Wrong Password", cancellationToken: cancellationToken);
                }
                
                WaitingForLogin.Remove(update.Message.Chat.Id);
            }
        }

        public bool IsChatLogged(long chatId) => Logged.Contains(chatId);

        public void LogoutForEveryOne()
        {
            foreach (var l in Logged)
            {
                menuSetup.RemoveMenu(l, new CancellationToken(), "Bot Server Stopped");
            }
        }

        private void LogoutIds(long chatId)
        {
            if (appState.WaitingBuyChatId.Equals(chatId))
            {
                appState.WaitingBuyChatId = 0;
                appState.BuyAmount = 0;
            }
            
            if (appState.WaitingSellAmountChatId.Equals(chatId))
            {
                appState.WaitingSellAmountChatId = 0;
                appState.SellAmount = 0;
            }
            
            if (appState.WaitingSellTransactionChatId.Equals(chatId))
            {
                appState.WaitingSellTransactionChatId = 0;
            }
            
            if (appState.WaitingSettingsChatId.Equals(chatId))
            {
                appState.WaitingSettingsChatId = 0;
                appState.InSettings = 0;
            }
        }
    }
}
