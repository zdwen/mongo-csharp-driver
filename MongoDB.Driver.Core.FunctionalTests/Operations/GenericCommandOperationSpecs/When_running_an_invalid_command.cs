using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.GenericCommandOperationSpecs
{
    public class When_running_an_invalid_command : Specification
    {
        private Exception _exception;

        protected override void When()
        {
            using (var session = BeginSession())
            {
                var op = new GenericCommandOperation<CommandResult>
                {
                    Command = new BsonDocument("invalid", 1),
                    Database = _database,
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
        public void The_exception_should_be_a_MongoOperationException()
        {
            Assert.IsInstanceOf<MongoOperationException>(_exception);
        }
    }
}