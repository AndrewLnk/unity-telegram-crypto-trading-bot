using System;
using System.Collections;
using Assets.Scripts.CoreAnalyzer;
using UnityEngine;

namespace Assets.Scripts.Main
{
    public class AnalyzeTimer : MonoBehaviour
    {
        private DateTime? expirationLoopTime;
        private Action loopedAction;

        private DateTime? invokeTime;
        private Action onceAction;

        public static AnalyzeTimer CreateInstance(string timerName)
        {
            var go = new GameObject(timerName);
            return go.AddComponent<AnalyzeTimer>();
        }
        
        public void StartLoopTimer(Action action)
        {
            loopedAction = action;
            expirationLoopTime = DateTime.Now.AddSeconds(CoreParams.TickerOfAveragePriceFetching);
        }

        private void Update()
        {
            if (expirationLoopTime.HasValue && DateTime.Now > expirationLoopTime.Value)
            {
                loopedAction?.Invoke();
                expirationLoopTime = DateTime.Now.AddSeconds(CoreParams.TickerOfAveragePriceFetching);
            }
            
            if (invokeTime.HasValue && DateTime.Now > invokeTime.Value)
            {
                onceAction?.Invoke();
                onceAction = null;
                invokeTime = null;
            }
        }
    }
}
