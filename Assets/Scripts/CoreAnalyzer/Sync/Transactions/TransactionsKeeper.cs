using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.CoreAnalyzer.Sync.FirebaseSync;
using Assets.Scripts.CoreAnalyzer.Sync.Telegram;
using UnityEngine;

namespace Assets.Scripts.CoreAnalyzer.Sync.Transactions
{
    public static class TransactionsKeeper
    {
        private static readonly List<Transaction> CurrentTransactions = new List<Transaction>();
        public static bool Initialized;

        public static Transaction[] GetTransactions() => CurrentTransactions.ToArray();

        public static decimal GetMinPrice()
        {
            if (CurrentTransactions.Count == 0)
                return -1;

            return CurrentTransactions.Min(e => e.Price);
        }
        
        public static Transaction CreateFreshTransaction()
        {
            var newId = 0;
            while (CurrentTransactions.Exists(e=>e.Id.Equals(newId)))
                newId++;

            return new Transaction { Id = newId };
        }

        public static void AddTransaction(Transaction transaction)
        {
            CurrentTransactions.Add(transaction);
            UploadTransactionsToServer();
        }
        
        public static void RemoveTransaction(Transaction transaction)
        {
            if (!CurrentTransactions.Contains(transaction))
                return;

            CurrentTransactions.Remove(transaction);
            UploadTransactionsToServer();
        }

        public static async void LoadTransactionsFromServer()
        {
            var returnValue = await FirebaseFields.GetString("Transactions", string.Empty);

            if (returnValue.Contains("error"))
            {
                TelegramNotifySync.SendNotification("Failed to load transactions...");
                return;
            }

            var (data, success) = TryGetData(returnValue);
            if (success)
            {
                CurrentTransactions.Clear();
                CurrentTransactions.AddRange(data);
                Initialized = true;
            }
            else
            {
                TelegramNotifySync.SendNotification("Failed read transactions...");
            }
        }
        
        private static (Transaction[] data, bool success) TryGetData(string response)
        {
            if (response != null && response.Length == 0)
                return (Array.Empty<Transaction>(), true);

            TransactionsPackage serverDto;
            try
            {
                serverDto = JsonUtility.FromJson<TransactionsPackage>(response);
            }
            catch (Exception e)
            {
                return (null, false);
            }

            if (serverDto == null)
                return (null, false);
            
            if (serverDto.Data == null)
                return (null, false);

            var transactions = new List<Transaction>();
            foreach (var serverTransaction in serverDto.Data)
            {
                var transaction = new Transaction()
                {
                    Id = serverTransaction.Id,
                    Amount = (decimal) serverTransaction.Amount,
                    Price = (decimal) serverTransaction.Price,
                };
                transactions.Add(transaction);
            }
            
            return (transactions.ToArray(), true);
        }

        private static void UploadTransactionsToServer()
        {
            var value = FormData(CurrentTransactions.ToArray());
            FirebaseFields.SetString("Transactions", value);
        }
        
        private static string FormData(Transaction[] items)
        {
            if (items == null || items.Length == 0)
                return string.Empty;
            
            var serverTransactions = new List<ServerTransaction>();
            foreach (var transaction in items)
            {
                var serverTransaction = new ServerTransaction()
                {
                    Id = transaction.Id,
                    Amount = (float) transaction.Amount,
                    Price = (float) transaction.Price,
                };
                serverTransactions.Add(serverTransaction);
            }

            var package = new TransactionsPackage() { Data = serverTransactions.ToArray() };
            return JsonUtility.ToJson(package, true);
        }
        
        [Serializable]
        public class TransactionsPackage
        {
            public ServerTransaction[] Data;
        }
    
        [Serializable]
        public class ServerTransaction
        {
            public int Id;
            public float Price;
            public float Amount;
        }
    }
}
