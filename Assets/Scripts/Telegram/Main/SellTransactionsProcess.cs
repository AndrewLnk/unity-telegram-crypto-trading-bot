using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Assets.Scripts.CoreAnalyzer.Sync.Transactions;
using Assets.Scripts.Main.Trading;
using Kucoin.Net.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UnityEngine;

namespace Assets.Scripts.Telegram.Main
{
    public class SellTransactionsProcess
    {
        private readonly ITelegramBotClient botClient;
        private readonly string command;
        private readonly AppState appState;
        private readonly ClientProcess clientProcess;
        private Func<long, bool> userLoggedFunc;
        private readonly ActionSetup actionSetup;
        
        public SellTransactionsProcess(ITelegramBotClient botClient, ActionSetup actionSetup, AppState appState, ClientProcess clientProcess)
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
   
                if (update.Message?.Text != null && update.Message.Text.Equals("Sell Transfers"))
                {
                    if (appState.WaitingSellTransactionChatId == 0)
                    {
                        appState.WaitingSellTransactionChatId = update.Message.Chat.Id;
                        await botClient.SendTextMessageAsync(update.Message.Chat, $"Select transaction", replyMarkup: GetMarkups());
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(update.Message.Chat, "Process locked by another user");
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
                
                if (!update.CallbackQuery.Message.Chat.Id.Equals(appState.WaitingSellTransactionChatId))
                    return;

                if (appState.WaitingSellTransactionChatId != 0)
                {
                    appState.WaitingSellTransactionChatId = 0;
                    
                    if (update.CallbackQuery.Data.Equals("20"))
                    {
                        await botClient.DeleteMessageAsync(update.CallbackQuery.Message.Chat, update.CallbackQuery.Message.MessageId);
                        actionSetup.AddMenu(update.CallbackQuery.Message.Chat.Id, CancellationToken.None, "Sell process canceled");
                        return;
                    }
                    
                    if (update.CallbackQuery.Data.StartsWith("2"))
                    {
                        await botClient.DeleteMessageAsync(update.CallbackQuery.Message.Chat, update.CallbackQuery.Message.MessageId);
                        var transactionIdRaw = update.CallbackQuery.Data.Substring(1);
                        var tryGetId = int.TryParse(transactionIdRaw, out var id);
                        if (tryGetId)
                        {
                            id--;
                            var transaction = TransactionsKeeper.GetTransactions().FirstOrDefault(e => e.Id.Equals(id));
                            if (transaction == null)
                            {
                                actionSetup.AddMenu(update.CallbackQuery.Message.Chat.Id, CancellationToken.None, $"Transaction not found");
                                return;
                            }
                            actionSetup.AddMenu(update.CallbackQuery.Message.Chat.Id, CancellationToken.None, $"Sell process confirmed for [{id}]...");
                            transaction.OrderSide = OrderSide.Sell;
                            clientProcess.TransfersCenter.TransferBetweenAccounts(transaction, transaction.Amount,  e =>
                            {
                                if (e.Equals(0)) TransactionsKeeper.RemoveTransaction(transaction);
                            });
                        }
                        else
                        {
                            actionSetup.AddMenu(update.CallbackQuery.Message.Chat.Id, CancellationToken.None, $"Transaction not found");
                        }
                    }
                }
            }
        }

        private static InlineKeyboardMarkup GetMarkups()
        {
            var transactions = TransactionsKeeper.GetTransactions().OrderBy(e=>e.Price);
            var list = new List<InlineKeyboardButton[]>();
            foreach (var transaction in transactions)
            {
                var item = new[]
                {
                    InlineKeyboardButton.WithCallbackData($"[{transaction.Id}] Amount: {transaction.Amount:0.0000}, Price {transaction.Price:0.0000}", callbackData: $"2{transaction.Id + 1}"),
                };
                list.Add(item);
            }
            
            list.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"Cancel", callbackData: "20"),
            });
            
            return new InlineKeyboardMarkup(list.ToArray());
        }
    }
}
