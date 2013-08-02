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

            using (var session = BeginSession())
            {
                var op = new AggregationOperation<BsonDocument>
                {
                    Collection = _collection,
                    Pipeline = pipeline,
                    Session = session
                };

                try
                {
                    op.Execute();
                }
                catch(Exception ex)
                {
                    _exception = ex;
                }
            }
        }

        [Then]
        public void Then_an_exception_should_be_thrown()
        {
            Assert.IsNotNull(_exception);
        }

        [And]
        public void And_it_should_be_a_MongoOperationException()
        {
            Assert.IsInstanceOf<MongoOperationException>(_exception);
        }
    }
}