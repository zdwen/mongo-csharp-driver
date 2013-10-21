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
    public class When_inserting_multiple_documents_requiring_multiple_batches : Specification
    {
        private List<BsonDocument> _docsToInsert;

        protected override void When()
        {
            _docsToInsert = new List<BsonDocument>();
            for(int i = 0; i < 10; i++)
            {
                var bytes = new byte[400];
                _docsToInsert.Add(new BsonDocument { { "i", i }, { "bytes", bytes } });
            }
            var op = new InsertOperation<BsonDocument>()
            {
                Collection = _collection,
                Documents = _docsToInsert,
                MaxMessageSize = 1024
            };

            ExecuteOperation(op);
        }

        [Test]
        public void The_local_documents_should_still_not_have_an_id()
        {
            for (int i = 0; i < _docsToInsert.Count; i++)
            {
                Assert.IsFalse(_docsToInsert[i].Elements.Any(x => x.Name == "_id"));
            }
        }

        [Test]
        public void The_documents_should_exist_in_the_database()
        {
            for (int i = 0; i < _docsToInsert.Count; i++)
            {
                var result = FindOne<BsonDocument>(new BsonDocument("i", i));
                Assert.IsNotNull(result, "Document {0} was not inserted.", i);
                Assert.AreEqual(400, result["bytes"].AsBsonBinaryData.Bytes.Length, "Document {0} did not have 400 bytes.", i);
            }
        }
    }
}