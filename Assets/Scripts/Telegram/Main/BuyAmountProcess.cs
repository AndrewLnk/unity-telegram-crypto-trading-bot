using System;
using System.Threading;
using Assets.Scripts.CoreAnalyzer;
using Assets.Scripts.Main.Trading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Assets.Scripts.Telegram.Main
{
    public class BuyAmountProcess
    {
        private readonly ITelegramBotClient botClient;
        private readonly string command;
        private readonly AppState appState;
        private readonly ClientProcess clientProcess;
        private Func<long, bool> userLoggedFunc;
        private readonly ActionSetup actionSetup;
        
        public BuyAmountProcess(ITelegramBotClient botClient, ActionSetup actionSetup, AppState appState, ClientProcess clientProcess)
        {
            this.botClient = botClient;
            this.actionSetup = actionSetup;
            this.appState = appState;
            this.clientProcess = clientProcess;
        }

        public void AddLoginChecker(Func<long, bool> userLogged) => userLoggedFunc = userLogged;

        public async void TryInvoke(Update update)
        {
            if(update.Type == UpdateType.Message)
            {
                if (update.Message == null)
                    return;
                
                if (!userLoggedFunc.Invoke(update.Message.Chat.Id))
                    return;

                if (update.CallbackQuery?.Message?.Chat != null && !userLoggedFunc.Invoke(update.CallbackQuery.Message.Chat.Id))
                    return;
   
                if (update.Message?.Text != null && update.Message.Text.Equals("Buy"))
                {
                    if (appState.WaitingBuyChatId == 0)
                    {
                        appState.WaitingBuyChatId = update.Message.Chat.Id;
                        var maxAmount = Math.Clamp(clientProcess.GetDefaultBalanceAvailable() - CoreParams.BuyReserve, 0, clientProcess.GetDefaultBalanceAvailable());
                        if (maxAmount < 0.1M)
                        {
                            appState.WaitingBuyChatId = 0;
                            appState.BuyAmount = 0;
                            actionSetup.AddMenu(update.Message.Chat.Id, CancellationToken.None, "Small Balance");
                            return;
                        }
                        
                        await botClient.SendTextMessageAsync(update.Message.Chat, $"Enter amount in {clientProcess.GetDefaultAssetName()}. Max: {maxAmount:0.0000}", replyMarkup: new ReplyKeyboardRemove());
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(update.Message.Chat, "Process locked by another user");
                    }
                    return;
                }

                if (update.Message != null && appState.WaitingBuyChatId != 0)
                {
                    appState.WaitingBuyChatId = 0;
                    appState.BuyAmount = 0;
                    var parsedText = decimal.TryParse(update.Message.Text, out var amount);
                    if (!parsedText)
                    {
                        actionSetup.AddMenu(update.Message.Chat.Id, CancellationToken.None, "Wrong text format");
                    }
                    else
                    {
                        if (amount < 0.1M)
                        {
                            appState.BuyAmount = amount;
                            actionSetup.AddMenu(update.Message.Chat.Id, CancellationToken.None, "Amount is too small");
                            return;
                        }
                        
                        if (amount > clientProcess.GetDefaultBalanceAvailable() - CoreParams.BuyReserve)
                        {
                            appState.BuyAmount = amount;
                            actionSetup.AddMenu(update.Message.Chat.Id, CancellationToken.None, "Amount is too large");
                            return;
                        }
                        
                        InlineKeyboardMarkup inlineKeyboard = new(new[]
                        {
                            // first row
                            new []
                            {
                                InlineKeyboardButton.WithCallbackData("Cancel", "10"),
                                InlineKeyboardButton.WithCallbackData("Confirm", "11"),
                            },
                        });
                        appState.BuyAmount = amount;
                        await botClient.SendTextMessageAsync(update.Message.Chat, $"Do you confirm sell {appState.BuyAmount} {clientProcess.GetDefaultAssetName()}?", replyMarkup: inlineKeyboard);
                    }
                    return;
                }
            }

            if (update.Type == UpdateType.CallbackQuery)
            {
                if (update.CallbackQuery?.Message?.Chat == null)
                    return;
                
                if (update.CallbackQuery?.Data == null) 
                    return;

                if (!userLoggedFunc.Invoke(update.CallbackQuery.Message.Chat.Id))
                    return;
                
                if (appState.BuyAmount != 0)
                {
                    if (update.CallbackQuery.Data.Equals("10"))
                    {
                        actionSetup.AddMenu(update.CallbackQuery.Message.Chat.Id, CancellationToken.None, "Buy process canceled");
                        await botClient.DeleteMessageAsync(update.CallbackQuery.Message.Chat, update.CallbackQuery.Message.MessageId);
                    }
                    
                    if (update.CallbackQuery.Data.Equals("11"))
                    {
                        await botClient.DeleteMessageAsync(update.CallbackQuery.Message.Chat, update.CallbackQuery.Message.MessageId);
                        var currentPrice = clientProcess.GetLastPrice();
                        clientProcess.TransfersCenter.TransferBuyLite(appState.BuyAmount, currentPrice, null);
                        actionSetup.AddMenu(update.CallbackQuery.Message.Chat.Id, CancellationToken.None, "Buy process confirmed...");
                    }
                    
                    appState.WaitingBuyChatId = 0;
                    appState.BuyAmount = 0;
                }
            }
        }
    }
}
