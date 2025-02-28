using Application.Commands.Order;
using Confluent.Kafka;
using Domain.Interfaces.Broker;
using Domain.Interfaces.Repositories;
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
            builder.Services.AddSingleton<IProducer<string, string>>(sp =>
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = builder.Configuration.GetValue<string>("Kafka:Server")
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
            builder.Services.AddSingleton<IEventStore, Application.Broker.EventStore>();
        }

        public static void ConfigureSqlServer(this WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<OrderDbContext>(options
               => options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
        }
    }
}