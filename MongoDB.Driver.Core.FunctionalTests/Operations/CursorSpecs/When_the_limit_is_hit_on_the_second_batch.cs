using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Sessions;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    public class When_the_limit_is_hit_on_the_second_batch : Specification
    {
        private ICursor<BsonDocument> _cursor;

        protected override void Given()
        {
            var docs = new List<BsonDocument>();
            for (int i = 0; i < 10; i++)
            {
                docs.Add(new BsonDocument("_id", i));
            }

            InsertData(docs.ToArray());
        }

        protected override void When()
        {
            var session = BeginSession();
            var findOp = new QueryOperation<BsonDocument>
            {
                Collection = _collection,
                BatchSize = 2,
                Limit = 4,
                Query = new BsonDocument(),
                Session = session
            };

            _cursor = findOp.Execute();
            while (_cursor.MoveNext()) ;
        }

        [Test]
        public void MoveNext_should_return_false_once_the_limit_has_been_reached()
        {
            Assert.IsFalse(_cursor.MoveNext());
        }
    }
}