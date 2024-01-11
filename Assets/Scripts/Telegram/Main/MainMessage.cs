using System.Threading;
using Telegram.Bot;

namespace Assets.Scripts.Telegram.Main
{
    public class MainMessage
    {
        public async void SendMessage(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken, string message)
        {
            await botClient.SendTextMessageAsync(chatId, message, cancellationToken: cancellationToken);
        }
    }
}
