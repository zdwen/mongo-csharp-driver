using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.AggregationOperation
{
    [TestFixture]
    public class When_executing_an_invalid_pipeline : Specification
    {
        private Exception _exception;

        protected override void Given()
        {
            InsertData(
                new BsonDocument("x", 1),
                new BsonDocument("x", 2),
                new BsonDocument("x", 3));
        }

        protected override void When()
        {
            var pipeline = new[] { new BsonDocument("$invalid", new BsonDocument()) };

            var op = new AggregateOperation<BsonDocument>
            {
                Collection = _collection,
                Pipeline = pipeline
            };

            _exception = Catch(() => ExecuteOperation(op));
        }

        [Test]
        public void An_exception_should_be_thrown()
        {
            Assert.IsNotNull(_exception);
        }

        [Test]
        public void The_exception_should_be_a_MongoOperationException()
        {
            Assert.IsInstanceOf<MongoOperationException>(_exception);
        }
    }
}