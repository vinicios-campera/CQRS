using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace EventStoreConsumer
{
    public static class Bootstrapper
    {
        public static void ConfigureWorker(this HostApplicationBuilder builder)
        {
            builder.ConfigureMongoDb();
        }

        private static void ConfigureMongoDb(this HostApplicationBuilder builder)
        {
            builder.Services.AddSingleton<IMongoClient>(sp
                => new MongoClient(builder.Configuration.GetConnectionString("MongoDb")));

            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        }
    }
}