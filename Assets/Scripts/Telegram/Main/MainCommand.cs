using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UnityEngine;

namespace Assets.Scripts.Telegram.Main
{
    public class MainCommand
    {
        private readonly string command;
        private readonly string message;
        private Func<long, bool> userLoggedFunc;
        private readonly MenuSetup menuSetup;

        private readonly List<(long message, long chat)> messages = new List<(long, long)>();

        public MainCommand(string command, MenuSetup menuSetup)
        {
            this.command = command;
            this.menuSetup = menuSetup;
        }

        public void AddLoginChecker(Func<long, bool> userLogged) => userLoggedFunc = userLogged;

        public void TryInvoke(Update update, Action successAction, Func<string> getMessage)
        {
            if(update.Type == UpdateType.Message)
            {
                if (update.Message?.Text == null)
                    return;
               
                if (!userLoggedFunc.Invoke(update.Message.Chat.Id))
                    return;

                if (update.Message.Text.Equals(command))
                {
                    successAction?.Invoke();
                    var messageText = getMessage != null ? getMessage.Invoke() : "Command received";
                    menuSetup.AddMenu(update.Message.Chat.Id, CancellationToken.None, messageText);
                }
            }
        }
        
        public async void TryInvokeTask(Update update, Func<Task> successAction, Func<string> getMessage)
        {
            if(update.Type == UpdateType.Message)
            {
                if (update.Message?.Text == null)
                    return;
               
                if (!userLoggedFunc.Invoke(update.Message.Chat.Id))
                    return;
            
                if (update.Message.Text.Equals(command))
                {
                    await successAction.Invoke();
                    var messageText = getMessage != null ? getMessage.Invoke() : "Command received";
                    menuSetup.AddMenu(update.Message.Chat.Id, CancellationToken.None, messageText);
                }
            }
        }
    }
}
