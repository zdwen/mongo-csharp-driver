using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Sessions;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Operations.CursorSpecs
{
    public class When_a_cursor_does_not_exist_on_the_server : Specification
    {
        private ICursor<BsonDocument> _cursor;
        private Exception _exception;

        protected override void Given()
        {
            var list = new List<BsonDocument>();
            for (int i = 0; i < 10; i++)
            {
                list.Add(new BsonDocument("_id", i));
            }
            InsertData(list.ToArray());

            var session = BeginSession();
            var op = new QueryOperation<BsonDocument>
            {
                BatchSize = 2,
                CloseSessionOnExecute = true,
                Collection = _collection,
                Query = new BsonDocument(),
                Session = session
            };

            _cursor = op.Execute();
        }

        protected override void When()
        {
            KillCursor(_cursor.CursorId);

            _exception = Catch(() =>
            {
                while (_cursor.MoveNext()) ;
            });
        }

        [Test]
        public void An_exception_should_be_thrown()
        {
            Assert.IsNotNull(_exception);
        }

        [Test]
        public void The_exception_should_be_a_MongoCursorNotFoundException()
        {
            Assert.IsInstanceOf<MongoCursorNotFoundException>(_exception);
        }

        private void KillCursor(long id)
        {
            using (var channel = _cursor.ChannelProvider.GetChannel(Timeout.InfiniteTimeSpan, CancellationToken.None))
            {
                var protocol = new KillCursorsProtocol(new[] { id });
                protocol.Execute(channel);
            }
        }
    }
}