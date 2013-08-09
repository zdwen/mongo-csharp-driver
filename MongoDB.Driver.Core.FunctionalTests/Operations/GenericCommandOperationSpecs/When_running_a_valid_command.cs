using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.GenericCommandOperationSpecs
{
    public class When_running_a_valid_command : Specification
    {
        private CommandResult _result;

        protected override void When()
        {
            var op = new GenericCommandOperation<CommandResult>
            {
                Command = new BsonDocument("isMaster", 1),
                Database = _database
            };

            _result = ExecuteOperation(op);
        }

        [Test]
        public void The_result_should_be_ok()
        {
            Assert.IsTrue(_result.Ok);
        }

        [Test]
        public void The_result_should_include_the_command()
        {
            Assert.AreEqual(new BsonDocument("isMaster", 1), _result.Command);
        }

        [Test]
        public void The_result_should_include_the_full_response()
        {
            Assert.IsNotNull(_result.Response);
        }
    }
}