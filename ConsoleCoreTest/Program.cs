using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Security;
using MongoDB.Driver.Core.Sessions;

namespace MongoDB.DriverUnitTests.Jira
{
    public static class Program
    {
        private static object _consoleLock = new object();
        private static DatabaseNamespace _database = new DatabaseNamespace("foo");
        private static CollectionNamespace _collection = new CollectionNamespace("foo", "bar");

        public static void Main()
        {
            var configuration = new DbConfiguration();

            var connString = new DbConnectionString("mongodb://localhost");
            configuration.ConfigureWithConnectionString(connString);

            // or everything can be done manually
            // 1) Create a Stream Factory
            // SSL
            //configuration.RegisterStreamFactory(inner =>
            //{
            //    return new SslStreamFactory(SslStreamSettings.Defaults, inner);
            //});
            // Socks
            //configuration.RegisterStreamFactory(inner =>
            //{
            //    return new Socks5StreamProxy(new DnsEndPoint("localhost", 9000), inner);
            //});

            // 2) Create a Connection Factory
            //configuration.ConfigureConnection(b =>
            //{
            //    b.AddCredential(MongoCredential.CreateMongoCRCredential("users", "user", "password"));
            //});

            // 3) Cluster
            //configuration.ConfigureCluster(b =>
            //{
            //    b.SetReplicaSetName("rs0");
            //    b.AddHosts(
            //        new DnsEndPoint("work-laptop", 30000),
            //        new DnsEndPoint("work-laptop", 30001),
            //        new DnsEndPoint("work-laptop", 30002));
            //});

            // 4) Events
            //configuration.UsePerformanceCounters("MyApplication", true);

            var cluster = configuration.BuildCluster();

            using (var session = new ClusterSession(cluster))
            {
                Console.WriteLine("Clearing Data");
                ClearData(session);
                Console.WriteLine("Inserting Seed Data");
                InsertData(session);
            }

            Console.WriteLine("Running aggregation as a cursor.");
            using (var session = new ClusterSession(cluster))
            {
                RunAggregation(session);
            }

            Console.WriteLine("Running Tests (errors will show up as + (query error) or * (insert/update error))");
            for (int i = 0; i < 7; i++)
            {
                ThreadPool.QueueUserWorkItem(_ => DoWork(new ClusterSession(cluster)));
            }

            DoWork(new ClusterSession(cluster)); // blocking

            cluster.Dispose();
        }

        private static void RunAggregation(ISession session)
        {
            var aggregation = new AggregateOperation<BsonDocument>()
            {
                Collection = _collection,
                Pipeline = new[] 
                {
                    new BsonDocument("$match", new BsonDocument())
                },
                Session = session
            };

            aggregation.ToList(); // pull back all the results
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
                    if (result != null)
                    {
                        result.Dispose();
                    }
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
            var insertOp = new InsertOperation<BsonDocument>()
            {
                Collection = _collection,
                Documents = new[] { document },
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
