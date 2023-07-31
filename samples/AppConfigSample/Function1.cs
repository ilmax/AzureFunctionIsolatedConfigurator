using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AppConfigSample
{
    public class Function1
    {
        private readonly ILogger _logger;
        private const string QueueName = "testqueue";

        public Function1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Function1>();
        }

        [Function("Function1")]
        public void Run([ServiceBusTrigger(QueueName, Connection = "ServiceBus")] string message)
        {
            _logger.LogInformation("Received message {m}", message);
        }
    }
}
