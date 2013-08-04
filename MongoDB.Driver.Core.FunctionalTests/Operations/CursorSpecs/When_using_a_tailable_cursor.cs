using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Protocol.Messages;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations
{
    public class When_using_a_tailable_cursor : Specification
    {
        private ICursor<BsonDocument> _cursor;

        protected override void Given()
        {
            CreateCappedCollection();
            InsertData(new BsonDocument("_id", 1).Add("ts", new BsonTimestamp(5, 0)));
        }

        protected override void When()
        {
            var session = BeginSession();
            var op = new QueryOperation<BsonDocument>
            {
                CloseSessionOnExecute = true,
                Collection = _collection,
                Flags = QueryFlags.TailableCursor,
                Query = new BsonDocument("ts", new BsonDocument("$gte", new BsonTimestamp(5, 0))),
                Session = session
            };

            _cursor = op.Execute();

            _cursor.MoveNext();
        }

        [Test]
        public void It_should_wait_until_the_next_value_is_available()
        {
            Func<bool> moveNext = () => _cursor.MoveNext();

            var asyncResult = moveNext.BeginInvoke(null, null);

            Assert.IsFalse(asyncResult.IsCompleted);
            ThreadPool.QueueUserWorkItem(_ => 
            {
                Thread.Sleep(2000);
                InsertData(new BsonDocument("_id", 2).Add("ts", new BsonTimestamp(6, 0)));
            });

            if (!asyncResult.AsyncWaitHandle.WaitOne(20000))
            {
                Assert.Fail("Cursor did not receive next document after 20 seconds.");
            }
            if (asyncResult.CompletedSynchronously)
            {
                Assert.Fail("Test is invalid as we didn't actually need to wait for the next result.");
            }

            var didMoveNext = moveNext.EndInvoke(asyncResult);
            if (!didMoveNext)
            {
                Assert.Fail("Failed to retrieve the next document.");
            }

            Assert.AreEqual(2, _cursor.Current["_id"].AsInt32);
        }
    }
}