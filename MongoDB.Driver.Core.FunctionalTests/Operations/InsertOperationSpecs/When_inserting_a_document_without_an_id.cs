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
    public class When_inserting_a_document_without_an_id : Specification
    {
        private BsonDocument _docToInsert;

        protected override void When()
        {
            using (var session = BeginSession())
            {
                _docToInsert = new BsonDocument("x", 2);
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
        public void Then_then_local_document_should_have_been_assigned_an_id()
        {
            Assert.IsTrue(_docToInsert.Elements.Any(x => x.Name == "_id"));
        }

        [Then]
        public void And_the_id_should_be_the_first_element()
        {
            var firstElement = _docToInsert.GetElement(0);
            Assert.AreEqual("_id", firstElement.Name);
        }

        [And]
        public void And_the_document_should_exist_in_the_database()
        {
            using (var session = BeginSession())
            {
                var findOp = new QueryOperation<BsonDocument>
                {
                    Collection = _collection,
                    Query = new BsonDocument("x", 2),
                    Session = session
                };

                var result = findOp.Single();
                Assert.AreEqual(2, result["x"].AsInt32);
            }
        }
    }
}