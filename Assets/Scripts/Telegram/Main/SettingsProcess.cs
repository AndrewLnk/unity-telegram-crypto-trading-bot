using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Main;
using Assets.Scripts.Main.Trading;
using Assets.Scripts.Tools;
using Kucoin.Net.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using UnityEngine;

namespace Assets.Scripts.Telegram.Main
{
    public class SettingsProcess
    {
        private readonly ITelegramBotClient botClient;
        private readonly string command;
        private readonly ClientProcess clientProcess;
        private readonly ParamsInput paramsInput;
        private readonly AppState appState;
        private Func<long, bool> userLoggedFunc;
        private readonly MenuSetup actionSetup;

        public SettingsProcess(ITelegramBotClient botClient, MenuSetup actionSetup, AppState appState, ClientProcess clientProcess, ParamsInput paramsInput)
        {
            this.botClient = botClient;
            this.actionSetup = actionSetup;
            this.clientProcess = clientProcess;
            this.paramsInput = paramsInput;
            this.appState = appState;
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
   
                if (update.Message?.Text != null && update.Message.Text.Equals("Settings"))
                {
                    appState.WaitingSettingsChatId = update.Message.Chat.Id;
                    appState.InSettings = 0;
                    await botClient.SendTextMessageAsync(update.Message.Chat, "Choose parameter", replyMarkup: GetMarkups());
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

                if (update.CallbackQuery.Message.Chat.Id.Equals(appState.WaitingSettingsChatId) && appState.InSettings == 0)
                {
                    if (update.CallbackQuery.Data.Equals("50"))
                    {
                        await botClient.DeleteMessageAsync(update.CallbackQuery.Message.Chat, update.CallbackQuery.Message.MessageId);
                        actionSetup.AddMenu(update.CallbackQuery.Message.Chat.Id, CancellationToken.None, "Menu");
                        appState.WaitingSettingsChatId = 0;
                        return;
                    }
                    
                    await CheckCode5(update);
                }
                
                if (update.CallbackQuery.Message.Chat.Id.Equals(appState.WaitingSettingsChatId) && appState.InSettings != 0)
                {
                    await CheckCode6(update);
                    await CheckCode7(update);
                    await CheckCode8(update);
                }
            }
        }

        private async Task CheckCode5(Update update)
        {
            if (update.CallbackQuery?.Message?.Chat == null)
                return;
                
            if (update.CallbackQuery?.Data == null) 
                return;
            
            if (update.CallbackQuery.Data.StartsWith("5"))
            {
                await botClient.DeleteMessageAsync(update.CallbackQuery.Message.Chat, update.CallbackQuery.Message.MessageId);
                var transactionIdRaw = update.CallbackQuery.Data.Substring(1);
                var tryGetId = int.TryParse(transactionIdRaw, out var id);
                if (tryGetId)
                {
                    appState.InSettings = id;
                    id--;
                    var field = fields.ElementAt(id);
                    var value = typeof(ParamsInput).GetField(field.Key, BindingFlags.Instance | BindingFlags.Public)?.GetValue(paramsInput);

                    // If Kline
                    if (value is KlineInterval)
                    {
                        var buttons = new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Save", "6="),
                            InlineKeyboardButton.WithCallbackData("Previous", "6-"),
                            InlineKeyboardButton.WithCallbackData("Next", "6+"),
                        });

                        var k = (KlineInterval) value;
                        await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"{field.Value} [{ConvertName(k)}]", replyMarkup: buttons);
                        return;
                    }

                    // If Local Kline
                    if (value is LocalKlineInterval)
                    {
                        var buttons = new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Save", "7="),
                            InlineKeyboardButton.WithCallbackData("Previous", "7-"),
                            InlineKeyboardButton.WithCallbackData("Next", "7+"),
                        });

                        var k = (LocalKlineInterval) value;
                        await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"{field.Value} [{ConvertName(k)}]", replyMarkup: buttons);
                        return;
                    }

                    if (value is float)
                    {
                        var buttons = new InlineKeyboardMarkup(new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Save", "8="),
                            InlineKeyboardButton.WithCallbackData("Previous", "8-"),
                            InlineKeyboardButton.WithCallbackData("Next", "8+"),
                        });

                        var amount = (float) value;
                        var prefix = fieldsPrefixes.ContainsKey(field.Key) ? fieldsPrefixes[field.Key] : string.Empty;
                        await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"{field.Value} [{amount}{prefix}]", replyMarkup: buttons);
                    }
                }
                else
                {
                    actionSetup.AddMenu(update.CallbackQuery.Message.Chat.Id, CancellationToken.None, $"Failed button");
                }
            }
        }
        
        private async Task CheckCode6(Update update)
        {
            if (update.CallbackQuery?.Message?.Chat == null)
                return;
                
            if (update.CallbackQuery?.Data == null) 
                return;
            
            if (update.CallbackQuery.Data.StartsWith("6"))
            {
                await botClient.DeleteMessageAsync(update.CallbackQuery.Message.Chat, update.CallbackQuery.Message.MessageId);

                var field = fields.ElementAt(appState.InSettings - 1);
                if (update.CallbackQuery.Data.EndsWith("="))
                {
                    appState.InSettings = 0;
                    appState.WaitingSettingsChatId = 0;
                    paramsInput.SaveField(field.Key);
                    actionSetup.AddMenu(update.CallbackQuery.Message.Chat.Id, CancellationToken.None, "Settings closed");
                    return;
                }

                var value = typeof(ParamsInput).GetField(field.Key, BindingFlags.Instance | BindingFlags.Public)?.GetValue(paramsInput);
                var kline = (KlineInterval) value;
                
                if (update.CallbackQuery.Data.EndsWith("+"))
                    kline = NextInterval(kline);
                
                if (update.CallbackQuery.Data.EndsWith("-"))
                    kline = PreviousInterval(kline);
                
                typeof(ParamsInput).GetField(field.Key, BindingFlags.Instance | BindingFlags.Public)?.SetValue(paramsInput, kline);
                
                var buttons = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Save", "6="),
                    InlineKeyboardButton.WithCallbackData("Previous", "6-"),
                    InlineKeyboardButton.WithCallbackData("Next", "6+"),
                });
                
                await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"{field.Value} [{ConvertName(kline)}]", replyMarkup: buttons);
            }
        }
        
        private async Task CheckCode7(Update update)
        {
            if (update.CallbackQuery?.Message?.Chat == null)
                return;
                
            if (update.CallbackQuery?.Data == null) 
                return;
            
            if (update.CallbackQuery.Data.StartsWith("7"))
            {
                await botClient.DeleteMessageAsync(update.CallbackQuery.Message.Chat, update.CallbackQuery.Message.MessageId);

                var field = fields.ElementAt(appState.InSettings - 1);
                if (update.CallbackQuery.Data.EndsWith("="))
                {
                    appState.InSettings = 0;
                    appState.WaitingSettingsChatId = 0;
                    paramsInput.SaveField(field.Key);
                    actionSetup.AddMenu(update.CallbackQuery.Message.Chat.Id, CancellationToken.None, "Settings closed");
                    return;
                }
                
                var value = typeof(ParamsInput).GetField(field.Key, BindingFlags.Instance | BindingFlags.Public)?.GetValue(paramsInput);
                var kline = (LocalKlineInterval) value;
                
                if (update.CallbackQuery.Data.EndsWith("+"))
                    kline = NextInterval(kline);
                
                if (update.CallbackQuery.Data.EndsWith("-"))
                    kline = PreviousInterval(kline);
                
                typeof(ParamsInput).GetField(field.Key, BindingFlags.Instance | BindingFlags.Public)?.SetValue(paramsInput, kline);
                
                var buttons = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Save", "7="),
                    InlineKeyboardButton.WithCallbackData("Previous", "7-"),
                    InlineKeyboardButton.WithCallbackData("Next", "7+"),
                });
                
                await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"{field.Value} [{ConvertName(kline)}]", replyMarkup: buttons);
            }
        }
        
        private async Task CheckCode8(Update update)
        {
            if (update.CallbackQuery?.Message?.Chat == null)
                return;
                
            if (update.CallbackQuery?.Data == null) 
                return;
            
            if (update.CallbackQuery.Data.StartsWith("8"))
            {
                await botClient.DeleteMessageAsync(update.CallbackQuery.Message.Chat, update.CallbackQuery.Message.MessageId);

                var field = fields.ElementAt(appState.InSettings - 1);
                
                if (update.CallbackQuery.Data.EndsWith("="))
                {
                    appState.InSettings = 0;
                    appState.WaitingSettingsChatId = 0;
                    paramsInput.SaveField(field.Key);
                    actionSetup.AddMenu(update.CallbackQuery.Message.Chat.Id, CancellationToken.None, "Settings closed");
                    return;
                }

                var value = typeof(ParamsInput).GetField(field.Key, BindingFlags.Instance | BindingFlags.Public)?.GetValue(paramsInput);
                var amount = (float) value;
                
                if (update.CallbackQuery.Data.EndsWith("+"))
                    NextValue(ref amount, fieldsSteps[field.Key]);
                
                if (update.CallbackQuery.Data.EndsWith("-"))
                    PreviousValue(ref amount,fieldsSteps[field.Key]);
                
                typeof(ParamsInput).GetField(field.Key, BindingFlags.Instance | BindingFlags.Public)?.SetValue(paramsInput, amount);
                
                var buttons = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("Save", "8="),
                    InlineKeyboardButton.WithCallbackData("Previous", "8-"),
                    InlineKeyboardButton.WithCallbackData("Next", "8+"),
                });
                
                var prefix = fieldsPrefixes.ContainsKey(field.Key) ? fieldsPrefixes[field.Key] : string.Empty;
                await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, $"{field.Value} [{ConvertName(amount)}{prefix}]", replyMarkup: buttons);
            }
        }

        private InlineKeyboardMarkup GetMarkups()
        {
            var list = new List<InlineKeyboardButton[]>();
            
            foreach (var item in fields)
            {
                var value = typeof(ParamsInput).GetField(item.Key, BindingFlags.Instance | BindingFlags.Public)?.GetValue(paramsInput);
                var prefix = fieldsPrefixes.ContainsKey(item.Key) ? fieldsPrefixes[item.Key] : string.Empty;
                var button = new[]
                {
                    InlineKeyboardButton.WithCallbackData($"{item.Value} - {ConvertName(value)}{prefix}",  $"5{fields.Keys.ToList().IndexOf(item.Key) + 1}"),
                };
                list.Add(button);
            }

            list.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"Cancel", callbackData: "50"),
            });
            
            return new InlineKeyboardMarkup(list.ToArray());
        }

        private readonly Dictionary<string, string> fields = new Dictionary<string, string>()
        {
            {"MinBuyAverageTime",    "Analyze Min Buy" },
            {"MinSellAverageTime",    "Analyze Min Sell" },
            {"MaxAverageTime",    "Analyze Max" },
            
            {"AnchorPercentage",   "Anchor" },
            {"BuyAnchorWaitingTime", "Anchor Duration" },
            {"AbsoluteBuyPercentage",   "Absolute Buy" },
            {"BuyStopTimeout",           "Lock When Bought" },
            
            {"SellPercentage", "Sell" },
            {"AbsoluteSellPercentage",   "Absolute Sell" },
 
            {"BuyStopPrice",      "Stop Price" },
            {"BuyLimitPrice",      "Limit Price" },
            {"BuyExchangeAmount", "Exchange Transfer" },
            {"BuyReserve",        "Reserve" },
        };
        
        private readonly Dictionary<string, float> fieldsSteps = new Dictionary<string, float>()
        {
            {"AnchorPercentage", 0.1f },
            {"AbsoluteBuyPercentage", 0.1f },
            
            {"BuyStopPrice", 0.01f },
            {"BuyLimitPrice", 0.01f },
            {"BuyExchangeAmount", 1 },
            {"BuyReserve", 5 },
            
            {"SellPercentage", 0.1f },
            {"AbsoluteSellPercentage", 0.1f },
        };
        
        private readonly Dictionary<string, string> fieldsPrefixes = new Dictionary<string, string>()
        {
            {"AnchorPercentage", "%" },
            {"AbsoluteBuyPercentage", "%" },
            
            {"BuyStopPrice", "$" },
            {"BuyLimitPrice", "$" },
            {"BuyExchangeAmount", "$" },
            {"BuyReserve", "$" },
            
            {"SellPercentage", "%" },
            {"AbsoluteSellPercentage", "%" },
        };
        
        private string ConvertName(object obj)
        {
            if (obj is LocalKlineInterval)
            {
                var interval = (LocalKlineInterval) obj;
                switch (interval)
                {
                    case LocalKlineInterval.Minute:
                        return "1 Minute";
                    case LocalKlineInterval.Minutes2:
                        return "2 Minutes";
                    case LocalKlineInterval.Minutes3:
                        return "3 Minutes";
                    case LocalKlineInterval.Minutes4:
                        return "4 Minutes";
                    case LocalKlineInterval.Minutes5:
                        return "5 Minutes";
                    case LocalKlineInterval.Minutes6:
                        return "6 Minutes";
                    case LocalKlineInterval.Minutes7:
                        return "7 Minutes";
                    case LocalKlineInterval.Minutes8:
                        return "8 Minutes";
                    case LocalKlineInterval.Minutes9:
                        return "9 Minutes";
                    case LocalKlineInterval.Minutes10:
                        return "10 Minutes";
                    case LocalKlineInterval.Minutes15:
                        return "15 Minutes";
                    case LocalKlineInterval.Minutes20:
                        return "20 Minutes";
                    case LocalKlineInterval.Minutes25:
                        return "25 Minutes";
                    case LocalKlineInterval.Minutes30:
                        return "30 Minutes";
                    case LocalKlineInterval.Minutes35:
                        return "35 Minutes";
                    case LocalKlineInterval.Minutes40:
                        return "40 Minutes";
                    case LocalKlineInterval.Minutes45:
                        return "45 Minutes";
                    case LocalKlineInterval.Minutes50:
                        return "50 Minutes";
                    case LocalKlineInterval.Minutes55:
                        return "55 Minutes";
                    case LocalKlineInterval.Hour:
                        return "1 Hour";
                    default:
                        return "X Minutes";
                }
            }
            
            if (obj is KlineInterval)
            {
                var interval = (KlineInterval) obj;
                switch (interval)
                {
                    case KlineInterval.OneMinute:
                        return "1 Minute";
                    case KlineInterval.ThreeMinutes:
                        return "3 Minutes";
                    case KlineInterval.FiveMinutes:
                        return "5 Minutes";
                    case KlineInterval.FifteenMinutes:
                        return "15 Minutes";
                    case KlineInterval.ThirtyMinutes:
                        return "30 Minutes";
                    case KlineInterval.OneHour:
                        return "1 Hour";
                    case KlineInterval.TwoHours:
                        return "2 Hours";
                    case KlineInterval.FourHours:
                        return "4 Hours";
                    case KlineInterval.SixHours:
                        return "6 Hours";
                    case KlineInterval.EightHours:
                        return "8 Hours";
                    case KlineInterval.TwelveHours:
                        return "12 Hours";
                    case KlineInterval.OneDay:
                        return "1 Day";
                    case KlineInterval.OneWeek:
                        return "1 Week";
                    default:
                        return "X Minutes";
                }
            }
            
            return obj.ToString();
        }
        
        private static KlineInterval NextInterval(KlineInterval src)
        {
            var values = (KlineInterval[])Enum.GetValues(src.GetType());
            var j = Array.IndexOf(values, src) + 1;
            return (values.Length == j) ? values[0] : values[j];            
        }
        
        private static LocalKlineInterval NextInterval(LocalKlineInterval src)
        {
            var values = (LocalKlineInterval[])Enum.GetValues(src.GetType());
            var j = Array.IndexOf(values, src) + 1;
            return (values.Length == j) ? values[0] : values[j];            
        }
        
        private static KlineInterval PreviousInterval(KlineInterval src)
        {
            var values = (KlineInterval[])Enum.GetValues(src.GetType());
            var j = Array.IndexOf(values, src) - 1;
            return (j < 0) ? values[values.Length - 1] : values[j];            
        }
        
        private static LocalKlineInterval PreviousInterval(LocalKlineInterval src)
        {
            var values = (LocalKlineInterval[])Enum.GetValues(src.GetType());
            var j = Array.IndexOf(values, src) - 1;
            return (j < 0) ? values[values.Length - 1] : values[j];            
        }

        private static void NextValue(ref float value, float step)
        {
            value += step;

            if (step > 1)
                return;
            
            var round = Mathf.RoundToInt(1 / step);
            value = Mathf.RoundToInt(value * round);
            value /= round;
        }

        private static void PreviousValue(ref float value, float step)
        {
            value -= step;
            
            if (step > 1)
                return;
            
            var round = Mathf.RoundToInt(1 / step);
            value = Mathf.RoundToInt(value * round);
            value /= round;
        }
    }
}
