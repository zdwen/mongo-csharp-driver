using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.RemoveOperationSpecs
{
    public class When_removing_a_document_with_a_valid_query : Specification
    {
        protected override void Given()
        {
            InsertData(new BsonDocument("_id", 1).Add("x", 2));
        }

        protected override void When()
        {
            var op = new RemoveOperation
            {
                Collection = _collection,
                Query = new BsonDocument("_id", 1)
            };

            ExecuteOperation(op);
        }

        [Test]
        public void The_document_should_be_removed()
        {
            var result = FindOne<BsonDocument>(new BsonDocument("_id", 1));
            Assert.IsNull(result);
        }
    }
}