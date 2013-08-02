using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    [TestFixture]
    public class InsertOperationTests : DatabaseTest
    {
        [TestFixtureSetUp]
        public void SetUp()
        {
            InsertData(new BsonDocument("_id", 1));
        }

        [Test]
        public void Executing_a_valid_insert_should_assign_an_id_and_add_the_document()
        {
            using (var session = BeginSession())
            {
                var docToInsert = new BsonDocument("_id", 2);
                var op = new InsertOperation()
                {
                    Collection = _collection,
                    Documents = new [] { docToInsert },
                    DocumentType = typeof(BsonDocument),
                    Session = session
                };

                op.Execute();

                var findOp = new QueryOperation<BsonDocument>
                {
                    Collection = _collection,
                    Query = new BsonDocument("_id", 2),
                    Session = session
                };

                var result = findOp.Single();
                Assert.AreEqual(2, result["_id"].AsInt32);
            }
        }

        [Test]
        public void Executing_an_insert_should_add_an_id_if_necessary()
        {
            using (var session = BeginSession())
            {
                var docToInsert = new BsonDocument("x", 2);
                var op = new InsertOperation()
                {
                    Collection = _collection,
                    Documents = new[] { docToInsert },
                    DocumentType = typeof(BsonDocument),
                    Session = session
                };

                op.Execute();

                Assert.IsTrue(docToInsert.Contains("_id"));
            }
        }

        [Test]
        public void Executing_an_insert_with_a_duplicate_id_should_throw_a_MongoDuplicateKeyException()
        {
            using (var session = BeginSession())
            {
                var docToInsert = new BsonDocument("_id", 1);
                var op = new InsertOperation()
                {
                    Collection = _collection,
                    Documents = new[] { docToInsert },
                    DocumentType = typeof(BsonDocument),
                    Session = session
                };

                Assert.Throws<MongoDuplicateKeyException>(() => op.Execute());
            }
        }
    }
}