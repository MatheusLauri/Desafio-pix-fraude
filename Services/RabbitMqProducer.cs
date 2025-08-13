using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace fraude_pix.Services
{
    public class RabbitMqProducer : IRabbitMqProducer
    {
        private readonly ConnectionFactory _factory;

        public RabbitMqProducer()
        {
            _factory = new ConnectionFactory
            {
                HostName = "rabbitmq", // ou "rabbitmq" se estiver rodando no docker-compose
                UserName = "guest",
                Password = "guest"
            };
        }

        public Task PublishAsync(object message)
        {
            using var connection = _factory.CreateConnection();
            using var channel = connection.CreateModel();

            string queueName = "fraude_pix_queue";

            // Garantir que a fila exista
            channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            // Serializar a mensagem para JSON
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            // Publicar a mensagem na fila
            channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: null,
                body: body
            );

            Console.WriteLine($"[Producer] Mensagem publicada na fila '{queueName}': {json}");

            return Task.CompletedTask;
        }
    }
}
