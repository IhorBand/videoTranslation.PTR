using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoTranslate.Shared.DTO.Configuration;

namespace VideoTranslate.PTR
{
    public class Application
    {
        private readonly RabbitMQConfiguration rabbitMQConfiguration;
        private readonly ConnectionStringConfiguration connectionStringConfiguration;
        private readonly ILogger<Application> logger;

        public Application(
            RabbitMQConfiguration rabbitMQConfiguration,
            ConnectionStringConfiguration connectionStringConfiguration,
            ILogger<Application> logger)
        {
            this.rabbitMQConfiguration = rabbitMQConfiguration;
            this.connectionStringConfiguration = connectionStringConfiguration;
            this.logger = logger;
        }

        public void Start()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("==============================================");
            Console.WriteLine("Configurations...");
            Console.WriteLine("==============================================");
            
            Console.WriteLine($"RabbitMQ Hostname - {rabbitMQConfiguration.HostName}");
            Console.WriteLine($"RabbitMQ User - {rabbitMQConfiguration.User}");
            Console.WriteLine($"RabbitMQ Password - {rabbitMQConfiguration.Password}");
            Console.WriteLine($"ConnectionString  - {connectionStringConfiguration.Main}");
            logger.LogInformation("Hello World !");

            var factory = new ConnectionFactory() 
            { 
                HostName = this.rabbitMQConfiguration.HostName,
                UserName = this.rabbitMQConfiguration.User,
                Password = this.rabbitMQConfiguration.Password
            };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "hello",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine(" [x] Received {0}", message);
                };
                channel.BasicConsume(queue: "hello",
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }


            Console.WriteLine("==============================================");
            Console.ResetColor();
        }
    }
}
