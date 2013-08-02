using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.InsertOperationSpecs
{
    [TestFixture]
    public class When_inserting_a_document_with_an_id : Specification
    {
        private BsonDocument _docToInsert;

        protected override void When()
        {
            using (var session = BeginSession())
            {
                _docToInsert = new BsonDocument("_id", 1);
                var op = new InsertOperation()
                {
                    Collection = _collection,
                    Documents = new[] { _docToInsert },
                    DocumentType = typeof(BsonDocument),
                    Session = session
                };

                op.Execute();
            }
        }

        [Then]
        public void Then_then_local_document_should_still_have_an_id()
        {
            Assert.IsTrue(_docToInsert.Elements.Any(x => x.Name == "_id"));
        }

        [Then]
        public void And_the_id_should_be_unchanged()
        {
            Assert.AreEqual(1, _docToInsert["_id"].AsInt32);
        }

        [And]
        public void And_the_document_should_exist_in_the_database()
        {
            using (var session = BeginSession())
            {
                var findOp = new QueryOperation<BsonDocument>
                {
                    Collection = _collection,
                    Query = new BsonDocument("_id", 1),
                    Session = session
                };

                var result = findOp.Single();
                Assert.AreEqual(1, _docToInsert["_id"].AsInt32);
            }
        }
    }
}