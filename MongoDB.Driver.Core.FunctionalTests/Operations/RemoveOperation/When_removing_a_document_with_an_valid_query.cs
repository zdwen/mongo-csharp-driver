using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.RemoveOperationSpecs
{
    public class When_removing_a_document_with_an_invalid_query : Specification
    {
        private Exception _exception;

        protected override void Given()
        {
            InsertData(new BsonDocument("loc", new BsonArray(new double[] { 1, 2 })));
        }

        protected override void When()
        {
            var op = new RemoveOperation
            {
                Collection = _collection,
                // this is an invalid query specification, $near requires an array 
                // as well as a 2d index
                Query = new BsonDocument("loc", new BsonDocument("$near", 2))
            };

            _exception = Catch(() => ExecuteOperation(op));
        }


        [Test]
        public void An_exception_should_be_thrown()
        {
            Assert.IsNotNull(_exception);
        }

        [Test]
        public void The_exception_should_be_a_MongoWriteConcernException()
        {
            Assert.IsInstanceOf<MongoWriteConcernException>(_exception);
        }
    }
}