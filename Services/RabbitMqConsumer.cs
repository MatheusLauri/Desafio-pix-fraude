using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Hosting;

namespace fraude_pix.Services
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMqConsumer()
        {
            _factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                UserName = "guest",
                Password = "guest"
            };
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();

            string queueName = "fraude_pix_queue";

            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            Console.WriteLine($"[Consumer] Iniciado. Aguardando mensagens na fila '{queueName}'...");

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null) throw new InvalidOperationException("Canal RabbitMQ não inicializado.");

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                try
                {
                    var message = JsonSerializer.Deserialize<JsonElement>(json);
                    Console.WriteLine($"[Consumer] Mensagem recebida: {json}");

                    bool isFraud = message.TryGetProperty("IsFraud", out var prop) && prop.GetBoolean();

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

            _channel.BasicConsume(
                queue: "fraude_pix_queue",
                autoAck: true,
                consumer: consumer
            );

            return Task.CompletedTask; // consumer roda em background
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
