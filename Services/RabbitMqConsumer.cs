using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace fraude_pix.Services
{
    public class RabbitMqConsumer
    {
        private readonly ConnectionFactory _factory;

        public RabbitMqConsumer()
        {
            _factory = new ConnectionFactory
            {
                HostName = "rabbitmq", 
                UserName = "guest",
                Password = "guest"
            };
        }

        public void StartConsuming()
        {
            var connection = _factory.CreateConnection();
            var channel = connection.CreateModel();

            string queueName = "fraude_pix_queue";

            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                try
                {
                    var message = JsonSerializer.Deserialize<dynamic>(json);
                    Console.WriteLine($"[Consumer] Mensagem recebida: {json}");

                    bool isFraud = message?.GetProperty("IsFraud")?.GetBoolean() ?? false;

                    if (isFraud)
                        Console.WriteLine("[Consumer] 🚨 Fraude detectada!");
                    else
                        Console.WriteLine("[Consumer] ✅ Transação válida.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Consumer] Erro ao processar mensagem: {ex.Message}");
                }
            };

            channel.BasicConsume(
                queue: queueName,
                autoAck: true,
                consumer: consumer
            );

            Console.WriteLine($"[Consumer] Aguardando mensagens na fila '{queueName}'...");
        }
    }
}
