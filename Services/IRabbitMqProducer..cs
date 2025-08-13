using System.Threading.Tasks;


namespace fraude_pix.Services
{
    public interface IRabbitMqProducer
    {
        Task PublishAsync(object message);
    }
}
