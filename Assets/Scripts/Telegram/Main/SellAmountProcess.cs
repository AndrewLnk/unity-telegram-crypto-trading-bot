using System;
using System.Threading;
using Assets.Scripts.Main.Trading;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UnityEngine;

namespace Assets.Scripts.Telegram.Main
{
    public class SellAmountProcess
    {
        private readonly ITelegramBotClient botClient;
        private readonly string command;
        private readonly AppState appState;
        private readonly ClientProcess clientProcess;
        private Func<long, bool> userLoggedFunc;
        private readonly ActionSetup actionSetup;
        
        public SellAmountProcess(ITelegramBotClient botClient, ActionSetup actionSetup, AppState appState, ClientProcess clientProcess)
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
   
                if (update.Message?.Text != null && update.Message.Text.Equals("Sell Other"))
                {
                    if (appState.WaitingSellAmountChatId == 0)
                    {
                        appState.WaitingSellAmountChatId = update.Message.Chat.Id;
                        var balance = clientProcess.GetSecondaryBalanceAvailable();
                        var amount = Math.Clamp(balance, 0, decimal.MaxValue);
                        if (amount < 0.1M)
                        {
                            appState.WaitingSellAmountChatId = 0;
                            appState.SellAmount = 0;
                            actionSetup.AddMenu(update.Message.Chat.Id, CancellationToken.None, "Small Balance");
                            return;
                        }
                        await botClient.SendTextMessageAsync(update.Message.Chat, $"Enter amount in {clientProcess.GetSecondaryAssetName()}. Max: {amount:0.0000}", replyMarkup: new ReplyKeyboardRemove());
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(update.Message.Chat, "Process locked by another user");
                    }
                    return;
                }

                if (update.Message != null && appState.WaitingSellAmountChatId != 0)
                {
                    appState.WaitingSellAmountChatId = 0;
                    appState.SellAmount = 0;
                    var parsedText = decimal.TryParse(update.Message.Text, out var amount);
                    if (!parsedText)
                    {
                        actionSetup.AddMenu(update.Message.Chat.Id, CancellationToken.None, "Wrong text format");
                    }
                    else
                    {
                        if (amount < 0.1M)
                        {
                            appState.SellAmount = amount;
                            actionSetup.AddMenu(update.Message.Chat.Id, CancellationToken.None, "Amount is too small");
                            return;
                        }
                        
                        if (amount > clientProcess.GetSecondaryBalanceAvailable())
                        {
                            appState.SellAmount = amount;
                            actionSetup.AddMenu(update.Message.Chat.Id, CancellationToken.None, "Amount is too large");
                            return;
                        }
                        
                        InlineKeyboardMarkup inlineKeyboard = new(new[]
                        {
                            // first row
                            new []
                            {
                                InlineKeyboardButton.WithCallbackData("Cancel", "30"),
                                InlineKeyboardButton.WithCallbackData("Confirm", "31"),
                            },
                        });
                        appState.SellAmount = amount;
                        await botClient.SendTextMessageAsync(update.Message.Chat, $"Do you confirm sell {appState.SellAmount} {clientProcess.GetSecondaryAssetName()} by {clientProcess.GetLastPrice():0.0000}?", replyMarkup: inlineKeyboard);
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
                
                if (appState.SellAmount != 0)
                {
                    if (update.CallbackQuery.Data.Equals("30"))
                    {
                        actionSetup.AddMenu(update.CallbackQuery.Message.Chat.Id, CancellationToken.None, "Buy process canceled");
                        await botClient.DeleteMessageAsync(update.CallbackQuery.Message.Chat, update.CallbackQuery.Message.MessageId);
                    }
                    
                    if (update.CallbackQuery.Data.Equals("31"))
                    {
                        await botClient.DeleteMessageAsync(update.CallbackQuery.Message.Chat, update.CallbackQuery.Message.MessageId);
                        clientProcess.TransfersCenter.TransferSellLite(appState.SellAmount, clientProcess.GetLastPrice(), null);
                        actionSetup.AddMenu(update.CallbackQuery.Message.Chat.Id, CancellationToken.None, "Sell process confirmed...");
                    }
                    
                    appState.WaitingSellAmountChatId = 0;
                    appState.SellAmount = 0;
                }
            }
        }
    }
}
