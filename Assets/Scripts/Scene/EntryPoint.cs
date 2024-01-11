using Assets.Scripts.CoreAnalyzer.Sync.Transactions;
using Assets.Scripts.Main;
using Assets.Scripts.Main.Trading;
using Assets.Scripts.Telegram;
using Assets.Scripts.Tools;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts.Scene
{
    public class EntryPoint : MonoBehaviour
    {
        private ClientProcess clientProcess;
        private ParamsInput paramsInput;
        private AppState appState;
        private TelegramEntry telegramEntry;

        private void Awake()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 5;
            OnDemandRendering.renderFrameInterval = 15;

            gameObject.AddComponent<MainSync>();
        }
        
        private void Start()
        {
            var accountData = new TradingTargetData()
            {
                ApiKey = UserData.ApiKey,
                ApiSecret = UserData.ApiSecret,
                ApiPassword = UserData.ApiPassword,
                DefaultCoin = UserData.DefaultCoin,
                SecondaryCoin = UserData.SecondaryCoin,
            };

            clientProcess = new ClientProcess(accountData);
            clientProcess.Initialize();
            
            paramsInput = new ParamsInput();
            appState = new AppState(); 
            LogView.SetAppState(appState);
            telegramEntry = new TelegramEntry(appState, clientProcess, paramsInput);
            TransactionsKeeper.LoadTransactionsFromServer();
        }

        private void Update()
        {
            OtherSetup();
            paramsInput?.Update();
        }

        private void OtherSetup()
        {
            if (appState == null || appState.Initialized)
                return;
            
            if (clientProcess?.Analyze == null || !clientProcess.Analyze.Initialized())
                return;

            paramsInput.UpdatedBuyDelta += (sender, args) => clientProcess.Analyze.ResetAnchor(false);
            appState.Initialized = true;
        }

        private void OnApplicationQuit()
        {
            telegramEntry?.StopBot();
        }
    }
}
