using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VideoTranslate.Shared.DTO.Configuration;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VideoTranslate.PTR.HostedServices
{
    public class ServiceRabbitMQ : IHostedService
    {
        private readonly ILogger<ServiceRabbitMQ> logger;
        private readonly RabbitMQConfiguration rabbitMQConfiguration;

        private ConnectionFactory connectionFactory;
        private IConnection connection;
        private IModel channel;
        private string QueueName = "ffmpeg_convert_for_recognition";

        public ServiceRabbitMQ(
            ILogger<ServiceRabbitMQ> logger,
            RabbitMQConfiguration rabbitMQConfiguration)
        {
            this.logger = logger;
            this.rabbitMQConfiguration = rabbitMQConfiguration;

            this.connectionFactory = new ConnectionFactory()
            {
                HostName = this.rabbitMQConfiguration.HostName,
                // port = 5672, default value
                //VirtualHost = "/",
                UserName = this.rabbitMQConfiguration.User,
                Password = this.rabbitMQConfiguration.Password
            };

            this.connection = this.connectionFactory.CreateConnection();
            this.channel = this.connection.CreateModel();
        }

        // Initiate RabbitMQ and start listening to an input queue
        private void Run()
        {
            // A queue to read messages
            this.channel.QueueDeclare(queue: this.QueueName,
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);
            // A queue to write messages
            Console.WriteLine(" [*] Waiting for messages.");

            var consumer = new EventingBasicConsumer(this.channel);
            consumer.Received += OnMessageRecieved;

            this.channel.BasicConsume(queue: this.QueueName,
                                autoAck: false,
                                consumer: consumer);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.Run();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this.channel.Dispose();
            this.connection.Dispose();
            return Task.CompletedTask;
        }

        private void OnMessageRecieved(object? model, BasicDeliverEventArgs args)
        {
            var body = args.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            //var convertVideoRecognizeCommand = JsonConvert.DeserializeObject<ConvertVideoRecognizeCommand>(message);
            this.logger.LogInformation(" [x] Received {0}", message);



            this.channel.BasicAck(deliveryTag: args.DeliveryTag, multiple: false);
        }

    }
}