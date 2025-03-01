using Application.Commands.Order;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Domain.Interfaces.Broker;
using Domain.Interfaces.Repositories;
using EventStore.Client;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Api
{
    public static class Bootstrapper
    {
        public static WebApplicationBuilder ConfigureApi(this WebApplicationBuilder builder)
        {
            builder.ConfigureMediator();
            builder.ConfigureKafka();
            builder.ConfigureRepositories();
            builder.ConfigureMongoDb();
            builder.ConfigureEventStore();
            builder.ConfigureSqlServer();

            return builder;
        }

        public static void ConfigureMediator(this WebApplicationBuilder builder)
        {
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(CreateOrderCommand).Assembly));
        }

        public static void ConfigureKafka(this WebApplicationBuilder builder)
        {
            var topics = builder.Configuration.GetSection("Topics").Get<List<string>>()!.ToArray();
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = builder.Configuration.GetConnectionString("Kafka")
            }).Build();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            foreach (var topicName in topics)
            {
                if (!metadata.Topics.Any(t => t.Topic == topicName))
                {
                    var topicSpec = new TopicSpecification
                    {
                        Name = topicName,
                        NumPartitions = 3,
                        ReplicationFactor = (short)1
                    };

                    adminClient.CreateTopicsAsync([topicSpec]).GetAwaiter().GetResult();
                }
            }

            builder.Services.AddSingleton<IProducer<string, string>>(sp =>
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = builder.Configuration.GetConnectionString("Kafka")
                };
                return new ProducerBuilder<string, string>(config).Build();
            });
        }

        public static void ConfigureRepositories(this WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IOrderWriteRepository, OrderWriteRepository>();
            builder.Services.AddScoped<IOrderReadRepository, OrderReadRepository>();
        }

        public static void ConfigureMongoDb(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<IMongoClient>(sp
                => new MongoClient(builder.Configuration.GetConnectionString("MongoDb")));
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        }

        public static void ConfigureEventStore(this WebApplicationBuilder builder)
        {
            var streams = builder.Configuration.GetSection("Streams").Get<List<string>>()!.ToArray();

            var settings = EventStoreClientSettings.Create(builder.Configuration.GetConnectionString("EventStore")!);
            var client = new EventStorePersistentSubscriptionsClient(settings);
            foreach (var stream in streams)
            {
                var groupName = $"{stream}-group";
                try
                {
                    client.GetInfoToStreamAsync(stream, groupName).GetAwaiter().GetResult();
                }
                catch (PersistentSubscriptionNotFoundException)
                {
                    var settingsSubscription = new PersistentSubscriptionSettings(
                        startFrom: StreamPosition.End,
                        resolveLinkTos: true,
                        extraStatistics: false,
                        messageTimeout: TimeSpan.FromSeconds(30),
                        maxRetryCount: 10,
                        liveBufferSize: 500,
                        readBatchSize: 20,
                        historyBufferSize: 500
                    );

                    client.CreateToStreamAsync(stream, groupName, settingsSubscription).GetAwaiter().GetResult();
                }
            }

            builder.Services.AddSingleton<IEventStore, Application.Broker.EventStore>();
        }

        public static void ConfigureSqlServer(this WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<OrderDbContext>(options
               => options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
        }
    }
}