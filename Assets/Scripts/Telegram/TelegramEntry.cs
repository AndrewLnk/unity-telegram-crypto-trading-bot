using System;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Main;
using Assets.Scripts.Main.Trading;
using Assets.Scripts.Telegram.Main;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using UnityEngine;

namespace Assets.Scripts.Telegram
{
    public class TelegramEntry
    {
        private static readonly ITelegramBotClient Bot = new TelegramBotClient(UserData.TelegramToken);

        private readonly ClientProcess clientProcess;
        private readonly AppState appState;
        private readonly Login login;
        private readonly ParamsInput paramsInput;
        private readonly MainCommand refreshMenu;
        private readonly MainCommand startProcess;
        private readonly MainCommand stopProcess;
        private readonly MainCommand silentProcess;
        private readonly MainCommand watchProcess;
        private readonly MainCommand getWalletState;

        private readonly ActionsCommand setActionsMenu;
        private readonly MainCommand setMainMenu;
        private readonly BuyAmountProcess buyAmountProcess;
        private readonly SellAmountProcess sellAmountProcess;
        private readonly SellTransactionsProcess sellTransactionsProcess;
        private readonly UpdateAnchorProcess updateAnchorProcess;
        
        private readonly SettingsProcess settingsProcess;

        public TelegramEntry(AppState appState, ClientProcess clientProcess, ParamsInput paramsInput)
        {
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            Bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cancellationToken);

            this.appState = appState;
            this.clientProcess = clientProcess;
            var menuSetup = new MenuSetup(appState, Bot);
            this.paramsInput = paramsInput;
            login = new Login(menuSetup, clientProcess, appState);
            refreshMenu = new MainCommand("Check if initialized", menuSetup);
            refreshMenu.AddLoginChecker(login.IsChatLogged);
            startProcess = new MainCommand("Start", menuSetup);
            startProcess.AddLoginChecker(login.IsChatLogged);
            stopProcess = new MainCommand("Stop", menuSetup);
            stopProcess.AddLoginChecker(login.IsChatLogged);
            silentProcess = new MainCommand("Silent", menuSetup);
            silentProcess.AddLoginChecker(login.IsChatLogged);
            watchProcess = new MainCommand("Watch", menuSetup);
            watchProcess.AddLoginChecker(login.IsChatLogged);
            getWalletState = new MainCommand("App Log", menuSetup);
            getWalletState.AddLoginChecker(login.IsChatLogged);

            var actionSetup = new ActionSetup(appState, Bot);
            setActionsMenu = new ActionsCommand("Actions", actionSetup);
            setActionsMenu.AddLoginChecker(login.IsChatLogged);
            setMainMenu = new MainCommand("Back", menuSetup);
            setMainMenu.AddLoginChecker(login.IsChatLogged);
            buyAmountProcess = new BuyAmountProcess(Bot, actionSetup, appState, clientProcess);
            buyAmountProcess.AddLoginChecker(login.IsChatLogged);
            sellAmountProcess = new SellAmountProcess(Bot, actionSetup, appState, clientProcess);
            sellAmountProcess.AddLoginChecker(login.IsChatLogged);
            sellTransactionsProcess = new SellTransactionsProcess(Bot, actionSetup, appState, clientProcess);
            sellTransactionsProcess.AddLoginChecker(login.IsChatLogged);
            updateAnchorProcess = new UpdateAnchorProcess(Bot, clientProcess);
            updateAnchorProcess.AddLoginChecker(login.IsChatLogged);
            settingsProcess = new SettingsProcess(Bot, menuSetup, appState, clientProcess, paramsInput);
            settingsProcess.AddLoginChecker(login.IsChatLogged);
        }
        
        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                login.TryLogin(botClient, update, cancellationToken);
                refreshMenu.TryInvoke(update, null, () => "Refreshed");
                startProcess.TryInvoke(update, () => { appState.Active = true; clientProcess.Analyze?.SetActive(true); }, () => "Started");
                stopProcess.TryInvoke(update, () => { appState.Active = false; clientProcess.Analyze?.SetActive(false); }, () => "Stopped");
                silentProcess.TryInvoke(update, () => { appState.WatchLog = false; }, () => "Silent process mode...");
                watchProcess.TryInvoke(update, () => { appState.WatchLog = true; }, () => "Log process mode...");
                setActionsMenu.TryInvoke(update, null, () => "Choose action");
                setMainMenu.TryInvoke(update, null, () => "Main menu");
                buyAmountProcess.TryInvoke(update);
                sellAmountProcess.TryInvoke(update);
                sellTransactionsProcess.TryInvoke(update);
                updateAnchorProcess.TryInvoke(update);
                settingsProcess.TryInvoke(update);
                
                getWalletState.TryInvokeTask(update, clientProcess.RefreshWallets, () =>
                                                                                         $"Status: {appState.Active}" +
                                                                                         $"\n{clientProcess.GetWalletsInfo()}" +
                                                                                         "\n"+
                                                                                         $"\nPrice: {clientProcess.GetLastPrice():0.0000}" +
                                                                                         $"\nAnchor: {clientProcess.GetBuyAnchorPrice():0.0000}" +
                                                                                         $"\nExpire: {clientProcess.GetBuyAnchorExpiration()}" +
                                                                                         $"\nReady Buy: {clientProcess.GetBuyAbsolutePrice():0.0000}" +
                                                                                         $"\n{clientProcess.GetTransactionsLog()}");
            }
            catch (Exception e)
            {
                if (update.Message?.Chat != null)
                {
                    await botClient.SendTextMessageAsync(update.Message?.Chat,$"Exception! {e.Message}", cancellationToken: cancellationToken);
                }
               
                if (update.CallbackQuery?.Message?.Chat != null)
                {
                    await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat,$"Exception! {e.Message}", cancellationToken: cancellationToken);
                }
                
                throw;
            }
        }

        private static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Debug.Log(exception.Message);
        }

        public void StopBot()
        {
            login.LogoutForEveryOne();
            appState.Active = false;
            appState.BuyAmount = 0;
            appState.WaitingBuyChatId = 0;
            appState.WaitingSellAmountChatId = 0;
            appState.SellAmount = 0;
            appState.WaitingSellTransactionChatId = 0;
            appState.WaitingSettingsChatId = 0;
            appState.InSettings = 0;
        }
    }
}
