using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.CoreAnalyzer.Sync.FirebaseSync
{
    public static class FirebaseFields
    {
        private static readonly Dictionary<string, string> StoredInfo = new Dictionary<string, string>();

        private static string GetFilePath(string fileName)
        {
            return $"https://firebasestorage.googleapis.com/v0/b/{UserData.FirebaseStorageUrl}/o/TradingBotData%2F{fileName}?alt=media&token=0000000000";
        }
        
        public static async Task<string> GetString(string key, string defaultValue, bool fresh = false)
        {
            if (fresh) DeleteLocalKey(key);
            
            if (StoredInfo.ContainsKey(key))
                return StoredInfo[key] != null ? StoredInfo[key] : defaultValue;

            await LoadDataProcess(key);

            if (StoredInfo.ContainsKey(key) && StoredInfo[key] == null)
                return defaultValue;

            if (StoredInfo.ContainsKey(key))
                return StoredInfo[key];

            return defaultValue;
        }
        
        public static async void SetString(string key, string value)
        {
            if (StoredInfo.ContainsKey(key))
            {
                StoredInfo[key] = value;
                await UploadDataProcess(key, StoredInfo[key]);
                return;
            }

            StoredInfo.Add(key, value);
            await UploadDataProcess(key, StoredInfo[key]);
        }
        
        public static async Task<decimal> GetDecimal(string key, decimal defaultValue, bool fresh = false)
        {
            if (fresh) DeleteLocalKey(key);
            
            if (StoredInfo.ContainsKey(key))
            {
                var value = StoredInfo[key];
                
                if (value == null)
                    return defaultValue;
                
                var parsed = decimal.TryParse(value, out var result);
                if (parsed)
                    return result;

                await UploadDataProcess(key, string.Empty);
                StoredInfo[key] = null;
                return defaultValue;
            }

            await LoadDataProcess(key);
            
            if (!StoredInfo.ContainsKey(key))
                return defaultValue;
            
            if (StoredInfo[key] == null)
                return defaultValue;
            
            var postParsed = decimal.TryParse(StoredInfo[key], out var postResult);
            if (postParsed)
                return postResult;
            
            return defaultValue;
        }
        
        public static async void SetDecimal(string key, decimal value)
        {
            if (StoredInfo.ContainsKey(key))
            {
                StoredInfo[key] = value.ToString(CultureInfo.InvariantCulture);
                await UploadDataProcess(key, StoredInfo[key]);
                return;
            }

            StoredInfo.Add(key, value.ToString(CultureInfo.InvariantCulture));
            await UploadDataProcess(key, StoredInfo[key]);
        }

        public static void DeleteLocalKey(string key)
        {
            if (StoredInfo.ContainsKey(key))
            {
                StoredInfo.Remove(key);
            }
        }

        private static async Task LoadDataProcess(string key)
        {
            var request = WebClient.Client.GetAsync(GetFilePath(key));
            var response = await request;
            var returnValue = response.Content.ReadAsStringAsync().Result;

            if (returnValue.Contains("error"))
            {
                StoredInfo.Add(key, null);
                return;
            }

            if (string.IsNullOrEmpty(returnValue))
                returnValue = null;

            if (StoredInfo.ContainsKey(key)) StoredInfo[key] = returnValue;
            else StoredInfo.Add(key, returnValue);
        }
        
        private static async Task UploadDataProcess(string key, string value)
        {
            var content = new StringContent(value, Encoding.UTF8, "application/json");
            var request = WebClient.Client.PostAsync(GetFilePath(key), content);
            await request;
        }
    }
}
