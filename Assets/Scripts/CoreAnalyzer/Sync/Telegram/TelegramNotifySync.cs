using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Assets.Scripts.CoreAnalyzer.Sync.Telegram
{
    public static class TelegramNotifySync
    {
        public static readonly List<long> Logged = new List<long>();
        private static string UploadUrl => $"https://api.telegram.org/bot{UserData.TelegramToken}/sendMessage?chat_id=";

        public static void SendNotification(string message)
        {
            foreach (var l in Logged) UploadDataProcess(message, l);
        }

        private static async void UploadDataProcess(string message, long chatId)
        {
            var body = "{ \"text\": \"" + message + "\" }";
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var request = WebClient.Client.PostAsync($"{UploadUrl}{chatId}", content);
            await request;
        }
    }
}
