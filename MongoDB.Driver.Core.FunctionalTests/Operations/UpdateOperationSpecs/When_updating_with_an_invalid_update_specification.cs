using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.UpdateOperationSpecs
{
    public class When_updating_with_an_invalid_update_specification : Specification
    {
        private Exception _exception;

        protected override void Given()
        {
            InsertData(new BsonDocument("_id", 1).Add("x", 2));
        }

        protected override void When()
        {
            using (var session = BeginSession())
            {
                var op = new UpdateOperation
                {
                    Collection = _collection,
                    Query = new BsonDocument("_id", 1),
                    Update = new BsonDocument
                    {
                        { "$inc", new BsonDocument("x", 1) },
                        { "invalid", 23 }
                    },
                    Session = session
                };

                _exception = Catch(() => op.Execute());
            }
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