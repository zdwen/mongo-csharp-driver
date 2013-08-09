using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.UpdateOperationSpecs
{
    public class When_updating_a_document_with_a_valid_update_specification : Specification
    {
        protected override void Given()
        {
            InsertData(new BsonDocument("_id", 1).Add("x", 2));
        }

        protected override void When()
        {
            var op = new UpdateOperation
            {
                Collection = _collection,
                Query = new BsonDocument("_id", 1),
                Update = new BsonDocument("$inc", new BsonDocument("x", 1))
            };

            ExecuteOperation(op);
        }

        [Test]
        public void The_document_should_be_updated()
        {
            var result = FindOne<BsonDocument>(new BsonDocument("_id", 1));
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result["x"].AsInt32);
        }
    }
}