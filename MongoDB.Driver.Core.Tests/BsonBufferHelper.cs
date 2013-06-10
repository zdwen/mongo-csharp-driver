using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Protocol;

namespace MongoDB.Driver.Core
{
    public static class BsonBufferHelper
    {
        public static ReplyMessage BuildReplyMessage(IEnumerable<BsonDocument> documents)
        {
            var buffer = new BsonBuffer();
            int docCount = 0;
            using (var writer = BsonWriter.Create(buffer))
            {
                foreach (var document in documents)
                {
                    docCount++;
                    BsonSerializer.Serialize(writer, document);
                }
            }

            buffer.Position = 0;
            return new ReplyMessage(
                4 + 4 + 4 + 4 + 4 + 8 + 4 + 4 + buffer.Length,
                0,
                0,
                OpCode.Reply,
                ReplyFlags.AwaitCapable,
                0,
                0,
                docCount,
                buffer);
        }

        public static BsonDocument ReadQueryMessage(BsonBufferedRequestMessage request)
        {
            // assuming the query message started at buffer position 0...
            var oldPosition = request.Buffer.Position;
            request.Buffer.Position = 0;
            var length = request.Buffer.ReadInt32();
            var requestId = request.Buffer.ReadInt32();
            var responseTo = request.Buffer.ReadInt32();
            var opCode = (OpCode)request.Buffer.ReadInt32();
            var queryFlags = (QueryFlags)request.Buffer.ReadInt32();
            var collectionFullName = request.Buffer.ReadCString(new UTF8Encoding(false, true));
            var numToSkip = request.Buffer.ReadInt32();
            var numToLimit = request.Buffer.ReadInt32();

            BsonDocument query;
            using (var reader = BsonReader.Create(request.Buffer))
            {
                query = BsonSerializer.Deserialize<BsonDocument>(reader);
            }

            request.Buffer.Position = oldPosition;

            return query;
        }
    }
}