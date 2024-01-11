using System.Net.Http;

namespace Assets.Scripts.CoreAnalyzer.Sync
{
    public static class WebClient
    {
        public readonly static HttpClient Client;
            
        static WebClient()
        {
            Client = new HttpClient();
        }
    }
}
