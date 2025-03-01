var builder = DistributedApplication.CreateBuilder(args);

var admin = builder.AddResource(new ParameterResource("parameter-admin", x => "admin"))
    .ExcludeFromManifest();

var passwordSqlServer = builder.AddResource(new ParameterResource("parameter-password-sql-server", x => "Your_password123"))
    .ExcludeFromManifest();

var sqlserver = builder.AddSqlServer("Sql-server", passwordSqlServer)
    .WithDataVolume()
    .AddDatabase("SqlServer", "OrdersDb");

var mongo = builder.AddMongoDB("Mongo-db", 27017, admin, admin)
    .WithDataVolume()
    .AddDatabase("MongoDb", "OrdersDb");

var kafka = builder.AddKafka("Kafka")
    .WithDataVolume()
    .WithKafkaUI();

var eventstore = builder.AddEventStore("EventStore")
    .WithDataVolume();

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(sqlserver).WaitFor(sqlserver)
    .WithReference(mongo).WaitFor(mongo)
    .WithReference(kafka).WaitFor(kafka)
    .WithReference(eventstore).WaitFor(eventstore);

builder.AddProject<Projects.KafkaConsumer>("kafka-consumer")
    .WithReference(mongo).WaitFor(mongo)
    .WithReference(kafka).WaitFor(kafka)
    .WaitFor(api);

builder.AddProject<Projects.EventStoreConsumer>("event-store-consumer")
    .WithReference(mongo).WaitFor(mongo)
    .WithReference(eventstore).WaitFor(eventstore)
    .WaitFor(api);

builder.Build().Run();