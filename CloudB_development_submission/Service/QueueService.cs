using Azure.Storage.Queues;
using System.Text;

namespace CloudB_development_submission.Service
{
    public class QueueService
    {
        private readonly QueueClient _queueClient;

        public QueueService(QueueClient queue)
        {
            _queueClient = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        public async Task SendAsync(string text)
        {
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            await _queueClient.SendMessageAsync(base64);
        }
        public async Task<List<string>> PeekMessagesAsync(int maxMessages = 5)
        {
            var messages = new List<string>();
            var peeked = await _queueClient.PeekMessagesAsync(maxMessages);

            foreach (var msg in peeked.Value)
            {
                // Decode Base64 message
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(msg.MessageText));
                messages.Add(decoded);
            }

            return messages;
        }

    }
}
