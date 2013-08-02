using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            var collectionName = GetType()
                .FullName
                .Substring("MongoDB.Driver.Core".Length)
                .Replace(".", "_");

            _collection = new CollectionNamespace(_database.DatabaseName, collectionName);

            _cluster = BuildCluster();
            _cluster.Initialize();
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            _cluster.Dispose();
            _cluster = null;
        }

        public ISession BeginSession()
        {
            return new ClusterSession(_cluster);
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