using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Security;
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Sessions;
using MongoDB.Driver.Core.Protocol.Messages;

namespace MongoDB.DriverUnitTests.Jira
{
    public static class Program
    {
        private static object _consoleLock = new object();
        private static DatabaseNamespace _database = new DatabaseNamespace("foo");
        private static CollectionNamespace _collection = new CollectionNamespace("foo", "bar");

        public static void Main()
        {
            var events = new EventPublisher();
            var traceManager = new TraceManager();

            // Performance Counters

            // must happen while running as an administrator...
            PerformanceCounterEventListeners.Install();
            
            var perfCounters = new PerformanceCounterEventListeners("My Application");
            events.Subscribe(perfCounters);


            // 1) Create a Stream Factory
            IStreamFactory streamFactory = new NetworkStreamFactory(
                NetworkStreamFactorySettings.Defaults,
                new DnsCache());

            // SSL
            //streamFactory = new SslStreamFactory(SslSettings.Defaults, streamFactory);

            // Socks
            //streamFactory = new Socks5StreamProxy(new DnsEndPoint("localhost", 1080), streamFactory);

            // 2) Create a Connection Factory
            IConnectionFactory connectionFactory = new StreamConnectionFactory(
                streamFactory,
                events,
                traceManager);

            // Authentication
            //var authSettings = AuthenticationSettings.Create(b =>
            //{
            //    b.AddCredential(MongoCredential.CreateMongoCRCredential("users", "user", "password"));
            //});
            //connectionFactory = new AuthenticatedConnectionFactory(authSettings, connectionFactory);

            // 3) Create a Channel Provider Factory
            IChannelProviderFactory channelProviderFactory = new ConnectionPoolChannelProviderFactory(
                new ConnectionPoolFactory(
                    ConnectionPoolSettings.Defaults,
                    connectionFactory,
                    events,
                    traceManager),
                events,
                traceManager);

            // A pipelined channel provider
            //channelProviderFactory = new PipelinedChannelProviderFactory(connectionFactory, 1);

            // 4) Create a Clusterable Server Factory
            var clusterableServerFactory = new ClusterableServerFactory(
                false,
                ClusterableServerSettings.Defaults,
                channelProviderFactory,
                connectionFactory,
                events,
                traceManager);

            // 5) Create a Cluster
            var cluster = new SingleServerCluster(new DnsEndPoint("localhost", 27017), clusterableServerFactory);

            //var cluster = new ReplicaSetCluster(
            //    ReplicaSetClusterSettings.Defaults,
            //    new[] 
            //    {
            //        new DnsEndPoint("work-laptop", 30000),
            //        //new DnsEndPoint("work-laptop", 30001),
            //        //new DnsEndPoint("work-laptop", 30002) 
            //    },
            //    nodeFactory);
            cluster.Initialize();

            using (var session = new ClusterSession(cluster))
            {
                Console.WriteLine("Clearing Data");
                ClearData(session);
                Console.WriteLine("Inserting Seed Data");
                InsertData(session);
            }

            Console.WriteLine("Running aggregation as a cursor.");
            RunAggregation(new ClusterSession(cluster));

            Console.WriteLine("Running Tests (errors will show up as + (query error) or * (insert/update error))");
            for (int i = 0; i < 7; i++)
            {
                ThreadPool.QueueUserWorkItem(_ => DoWork(new ClusterSession(cluster)));
            }

            DoWork(new ClusterSession(cluster)); // blocking
        }

        private static void RunAggregation(ISession session)
        {
            var aggregation = new AggregationOperation<BsonDocument>()
            {
                Collection = _collection,
                Pipeline = new [] 
                {
                    new BsonDocument("$match", new BsonDocument())
                },
                Session = session
            };

            using (var result = aggregation.Execute())
            {
                while(result.MoveNext())
                {
                    Console.WriteLine(result.Current);
                }
            }
        }

        private static void ClearData(ISession session)
        {
            var commandOp = new GenericCommandOperation<CommandResult>()
            {
                Command = new BsonDocument("dropDatabase", 1),
                Database = _database,
                Session = session
            };

            commandOp.Execute();
        }

        private static void InsertData(ISession session)
        {
            for (int i = 0; i < 10000; i++)
            {
                Insert(session, new BsonDocument("i", i));
            }
        }

        private static void DoWork(ISession session)
        {
            var rand = new Random();
            while (true)
            {
                var i = rand.Next(0, 10000);
                BsonDocument doc;
                IEnumerator<BsonDocument> result = null;
                try
                {
                    result = Query(session, new BsonDocument("i", i));
                    if (result.MoveNext())
                    {
                        doc = result.Current;
                    }
                    else
                    {
                        doc = null;
                    }

                    //Console.Write(".");
                }
                catch (Exception)
                {
                    Console.Write("+");
                    continue;
                }
                finally
                {
                    result.Dispose();
                }

                if (doc == null)
                {
                    try
                    {
                        Insert(session, new BsonDocument().Add("i", i));
                        //Console.Write(".");
                    }
                    catch (Exception)
                    {
                        Console.Write("*");
                    }
                }
                else
                {
                    try
                    {
                        var query = new BsonDocument("_id", doc["_id"]);
                        var update = new BsonDocument("$set", new BsonDocument("i", i + 1));
                        Update(session, query, update);
                        //Console.Write(".");
                    }
                    catch (Exception)
                    {
                        Console.Write("*");
                    }
                }
            }
        }

        private static void Insert(ISession session, BsonDocument document)
        {
            var insertOp = new InsertOperation()
            {
                Collection = _collection,
                Documents = new [] { document },
                DocumentType = typeof(BsonDocument),
                Session = session
            };

            insertOp.Execute();
        }

        private static IEnumerator<BsonDocument> Query(ISession session, BsonDocument query)
        {
            var queryOp = new QueryOperation<BsonDocument>()
            {
                Collection = _collection,
                Limit = 1,
                Query = query,
                ReadPreference = ReadPreference.Nearest,
                Session = session
            };

            return queryOp.Execute();
        }

        private static void Update(ISession session, BsonDocument query, BsonDocument update)
        {
            var updateOp = new UpdateOperation()
            {
                Collection = _collection,
                Flags = UpdateFlags.Multi,
                Query = query,
                Session = session,
                Update = update,
            };

            updateOp.Execute();
        }
    }
}
