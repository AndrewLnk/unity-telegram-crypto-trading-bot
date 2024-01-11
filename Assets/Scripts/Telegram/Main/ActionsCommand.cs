using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using UnityEngine;

namespace Assets.Scripts.Telegram.Main
{
    public class ActionsCommand
    {
        private readonly string command;
        private readonly string message;
        private Func<long, bool> userLoggedFunc;
        private readonly ActionSetup actionSetup;
        
        public ActionsCommand(string command, ActionSetup actionSetup)
        {
            this.command = command;
            this.actionSetup = actionSetup;
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
                    actionSetup.AddMenu(update.Message.Chat.Id, CancellationToken.None, messageText);
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
                    actionSetup.AddMenu(update.Message.Chat.Id, CancellationToken.None, messageText);
                }
            }
        }
    }
}
