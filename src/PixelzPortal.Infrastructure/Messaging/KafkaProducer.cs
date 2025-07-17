using Confluent.Kafka;
using Microsoft.Extensions.Options;
using PixelzPortal.Application.Interfaces;
using System.Text.Json;

namespace PixelzPortal.Infrastructure.Messaging
{
    
    public class KafkaProducer : IKafkaProducer
    {
        private readonly IProducer<Null, string> _producer;

        public KafkaProducer(IOptions<KafkaSettings> settings)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = settings.Value.BootstrapServers
            };

            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task ProduceAsync<T>(string topic, T message)
        {
            var json = JsonSerializer.Serialize(message);

            var msg = new Message<Null, string> { Value = json };
            await _producer.ProduceAsync(topic, msg);
        }
    }

    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = default!;
        public string Topic { get; set; } = default!;
    }

}
