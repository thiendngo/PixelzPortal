using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PixelzPortal.Infrastructure.Messaging
{
    public interface IKafkaProducer
    {
        Task ProduceAsync<T>(string topic, T message);
    }

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
