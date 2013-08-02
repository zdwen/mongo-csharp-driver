using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Sessions;
using NUnit.Framework;

namespace MongoDB.Driver.Core
{
    [SetUpFixture]
    public class SuiteSetup
    {
        private static DatabaseNamespace __database;

        public static DatabaseNamespace Database
        {
            get { return __database; }
        }

        [SetUp]
        public void RunBeforeAnyTests()
        {
            __database = new DatabaseNamespace("Driver_Core_Functional_" + DateTime.Now.ToFileTimeUtc());
        }

        [TearDown]
        public void RunAfterAllTests()
        {
            using (var cluster = new ClusterBuilder().BuildCluster())
            {
                cluster.Initialize();
                using (var session = new ClusterSession(cluster))
                {
                    var command = new GenericCommandOperation<CommandResult>
                    {
                        Command = new BsonDocument("dropDatabase", 1),
                        Database = __database,
                        Session = session
                    };

                    command.Execute();
                }
            }
        }
    }
}