using MongoDB.Bson;
using MongoDB.Driver;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: MongoReplicaSetCoordinator <init|primary-port> <connection-string>");
    return 2;
}

var direct = new MongoClient(args[1]);
var admin = direct.GetDatabase("admin");
if (args[0].Equals("init", StringComparison.OrdinalIgnoreCase))
{
    var hosts = args.Skip(2).ToArray();
    if (hosts.Length < 1) throw new ArgumentException("At least one replica-set host is required.");
    try
    {
        await admin.RunCommandAsync<BsonDocument>(new BsonDocument
        {
            { "replSetInitiate", new BsonDocument
                {
                    { "_id", "oasisrs" },
                    { "members", new BsonArray(hosts.Select((host, index) =>
                        new BsonDocument { { "_id", index }, { "host", host } })) }
                }
            }
        });
    }
    catch (MongoCommandException ex) when (ex.CodeName == "AlreadyInitialized") { }

    var deadline = DateTime.UtcNow.AddSeconds(60);
    while (DateTime.UtcNow < deadline)
    {
        try
        {
            var hello = await admin.RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1));
            if (hello.TryGetValue("isWritablePrimary", out var writable) && writable.ToBoolean()) return 0;
        }
        catch (MongoException) { }
        await Task.Delay(250);
    }
    throw new TimeoutException("The local MongoDB replica set did not elect a primary.");
}

if (args[0].Equals("primary-port", StringComparison.OrdinalIgnoreCase))
{
    var hello = await admin.RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1));
    string primary = hello["primary"].AsString;
    Console.WriteLine(new Uri("mongodb://" + primary).Port);
    return 0;
}

Console.Error.WriteLine($"Unknown command '{args[0]}'.");
return 2;
