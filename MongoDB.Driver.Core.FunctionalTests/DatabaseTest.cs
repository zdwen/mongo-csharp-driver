using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Sessions;
using NUnit.Framework;

namespace MongoDB.Driver.Core
{
    public abstract class DatabaseTest : ClusterBuilder
    {
        // these are protected for ease of use
        protected DatabaseNamespace _database;
        protected ICluster _cluster;
        protected CollectionNamespace _collection;

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            _database = SuiteSetup.Database;

            var type = GetType();
            var lastDotIndex = type.Namespace.LastIndexOf('.');
            var collectionName = type.Namespace.Substring(lastDotIndex + 1) + "_" + type.Name;

            if (_database.DatabaseName.Length + collectionName.Length > 100)
            {
                collectionName = collectionName.Substring(0, 100 - _database.DatabaseName.Length);
            }

            _collection = new CollectionNamespace(_database.DatabaseName, collectionName);

            _cluster = BuildCluster();
            _cluster.Initialize();
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            using (var session = new ClusterSession(_cluster))
            {
                var command = new GenericCommandOperation<CommandResult>
                {
                    Command = new BsonDocument("drop", _collection.CollectionName),
                    Database = _database,
                    Session = session
                };

                try
                {
                    command.Execute();
                }
                catch (MongoOperationException ex)
                {
                    // this might occur if the collection was never created.  We don't care
                    // in this case.
                    if (ex.Response["errmsg"].AsString != "ns not found")
                    {
                        throw;
                    }
                }
            }

            _cluster.Dispose();
            _cluster = null;
        }

        public ISession BeginSession()
        {
            return new ClusterSession(_cluster);
        }

        public void CreateCappedCollection()
        {
            using (var session = BeginSession())
            {
                var op = new GenericCommandOperation<CommandResult>
                {
                    Command = new BsonDocument
                    {
                        { "create", _collection.CollectionName },
                        { "capped", true },
                        { "max", 1000 },
                        { "size", 1000 }
                    },
                    Database = _database,
                    Session = session
                };

                op.Execute();
            }
        }

        public IEnumerable<T> Find<T>(BsonDocument query)
        {
            using (var session = BeginSession())
            {
                var findOp = new QueryOperation<T>
                {
                    Collection = _collection,
                    Limit = 1,
                    Query = query,
                    Session = session
                };

                return findOp.ToList();
            }
        }

        public T FindOne<T>(BsonDocument query)
        {
            using (var session = BeginSession())
            {
                var findOp = new QueryOperation<T>
                {
                    Collection = _collection,
                    Limit = 1,
                    Query = query,
                    Session = session
                };

                return findOp.FirstOrDefault();
            }
        }

        public void InsertData<T>(params T[] documents)
        {
            using (var session = BeginSession())
            {
                var op = new InsertOperation()
                {
                    Collection = _collection,
                    Documents = documents,
                    DocumentType = typeof(T),
                    Session = session
                };

                op.Execute();
            }
        }
    }
}