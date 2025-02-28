using Application.Commands.Order;
using Confluent.Kafka;
using Domain.Interfaces.Events;
using Domain.Interfaces.Repositories;
using Infrastructure.Broker;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Api
{
    public static class Bootstrapper
    {
        public static WebApplicationBuilder ConfigureApi(this WebApplicationBuilder builder)
        {
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(CreateOrderCommand).Assembly));

            builder.Services.AddSingleton<IProducer<string, string>>(sp =>
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = builder.Configuration.GetValue<string>("Kafka:Server")
                };
                return new ProducerBuilder<string, string>(config).Build();
            });
            builder.Services.AddSingleton<IConsumer<string, string>>(sp =>
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = builder.Configuration.GetValue<string>("Kafka:Server"),
                    GroupId = "orders-consumer-group",
                    AutoOffsetReset = AutoOffsetReset.Earliest
                };

                return new ConsumerBuilder<string, string>(config).Build();
            });
            builder.Services.AddHostedService<KafkaOrderConsumer>();

            builder.Services.AddScoped<IOrderWriteRepository, OrderWriteRepository>();
            builder.Services.AddScoped<IOrderReadRepository, OrderReadRepository>();

            builder.Services.AddSingleton<IMongoClient>(sp
                => new MongoClient(builder.Configuration.GetConnectionString("MongoDb")));
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            builder.Services.AddDbContext<OrderDbContext>(options
                => options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

            builder.Services.AddSingleton<IEventStore, EventStoreService>();
            builder.Services.AddSingleton<OrderEventHandler>();

            return builder;
        }
    }
}