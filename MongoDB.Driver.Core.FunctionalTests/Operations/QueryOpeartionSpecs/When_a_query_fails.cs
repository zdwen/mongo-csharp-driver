using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.QueryOpeartionSpecs
{
    public class When_a_query_fails : Specification
    {
        private Exception _exception;

        protected override void Given()
        {
            InsertData(new BsonDocument("loc", new BsonArray(new double[] { 1, 2 })));
        }

        protected override void When()
        {
            using (var session = BeginSession())
            {
                var findOp = new QueryOperation<BsonDocument>
                {
                    Collection = _collection,
                    // this is an invalid query specification, $near requires an array 
                    // as well as a 2d index
                    Query = new BsonDocument("loc", new BsonDocument("$near", 2)),
                    Session = session
                };

                _exception = Catch(() => findOp.Execute());
            }
        }

        [Then]
        public void Then_an_exception_should_be_thrown()
        {
            Assert.IsNotNull(_exception);
        }

        [And]
        public void And_it_should_be_a_MongoQueryFailureException()
        {
            Assert.IsInstanceOf<MongoQueryException>(_exception);
        }
    }
}
