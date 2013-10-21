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
            _docToInsert = new BsonDocument("x", 2);
            var op = new InsertOperation<BsonDocument>()
            {
                Collection = _collection,
                Documents = new[] { _docToInsert }
            };

            ExecuteOperation(op);
        }

        [Test]
        public void The_local_document_should_still_not_have_an_id()
        {
            Assert.IsFalse(_docToInsert.Elements.Any(x => x.Name == "_id"));
        }

        [Test]
        public void The_document_should_exist_in_the_database()
        {
            var result = FindOne<BsonDocument>();
            Assert.AreEqual(2, result["x"].AsInt32);
        }
    }
}