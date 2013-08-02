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
    public class When_inserting_a_document_with_a_duplicate_id : Specification
    {
        private Exception _exception;

        protected override void Given()
        {
            InsertData(new BsonDocument("_id", 1));
        }

        protected override void When()
        {
            using (var session = BeginSession())
            {
                var op = new InsertOperation()
                {
                    Collection = _collection,
                    Documents = new[] { new BsonDocument("_id", 1) },
                    DocumentType = typeof(BsonDocument),
                    Session = session
                };

                try
                {
                    op.Execute();
                }
                catch (Exception ex)
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
        public void And_it_should_be_a_MongoDuplicateKeyException()
        {
            Assert.IsInstanceOf<MongoDuplicateKeyException>(_exception);
        }
    }
}