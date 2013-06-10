/* Copyright 2010-2013 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// A reply message from the server.
    /// </summary>
    public sealed class ReplyMessage : IDisposable
    {
        // private fields
        private readonly int _length;
        private readonly int _requestId;
        private readonly int _responseTo;
        private readonly OpCode _opCode;
        private readonly ReplyFlags _flags;
        private readonly long _cursorId;
        private readonly int _startingFrom;
        private readonly int _numberReturned;
        private readonly BsonBuffer _documentsBuffer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplyMessage" /> class.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="requestId">The request id.</param>
        /// <param name="responseTo">The response to.</param>
        /// <param name="opCode">The op code.</param>
        /// <param name="responseFlags">The response flags.</param>
        /// <param name="cursorId">The cursor id.</param>
        /// <param name="startingFrom">The starting from.</param>
        /// <param name="numberReturned">The number returned.</param>
        /// <param name="documentsBuffer">The documents buffer.</param>
        public ReplyMessage(int length, 
            int requestId, 
            int responseTo, 
            OpCode opCode, 
            ReplyFlags responseFlags,
            long cursorId,
            int startingFrom,
            int numberReturned,
            BsonBuffer documentsBuffer)
        {
            _length = length;
            _requestId = requestId;
            _responseTo = responseTo;
            _opCode = opCode;
            _flags = responseFlags;
            _cursorId = cursorId;
            _startingFrom = startingFrom;
            _numberReturned = numberReturned;
            _documentsBuffer = documentsBuffer;
        }

        // public properties
        /// <summary>
        /// Gets the cursor id.
        /// </summary>
        public long CursorId
        {
            get { return _cursorId; }
        }

        /// <summary>
        /// Gets the flags.
        /// </summary>
        public ReplyFlags Flags
        {
            get { return _flags; }
        }

        /// <summary>
        /// Gets the length.
        /// </summary>
        public int Length
        {
            get { return _length; }
        }

        /// <summary>
        /// Gets the number returned.
        /// </summary>
        public int NumberReturned
        {
            get { return _numberReturned; }
        }

        /// <summary>
        /// Gets the op code.
        /// </summary>
        public OpCode OpCode
        {
            get { return _opCode; }
        }

        /// <summary>
        /// Gets the request id.
        /// </summary>
        public int RequestId
        {
            get { return _requestId; }
        }

        /// <summary>
        /// Gets the response to.
        /// </summary>
        public int ResponseTo
        {
            get { return _responseTo; }
        }

        /// <summary>
        /// Gets the starting from.
        /// </summary>
        public int StartingFrom
        {
            get { return _startingFrom; }
        }

        // public static methods
        /// <summary>
        /// Reads the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public static ReplyMessage Read(Stream stream)
        {
            var messageLength = ReadMessageLength(stream);
            var byteBuffer = ByteBufferFactory.Create(BsonChunkPool.Default, messageLength);
            var buffer = new BsonBuffer();
            buffer.WriteInt32(messageLength);
            buffer.LoadFrom(stream, messageLength - 4); // 4 is the size of the message length
            buffer.Position = 4;

            var requestId = buffer.ReadInt32();
            var responseTo = buffer.ReadInt32();
            var opCode = (OpCode)buffer.ReadInt32();
            var responseFlags = (ReplyFlags)buffer.ReadInt32();
            var cursorId = buffer.ReadInt64();
            var startingFrom = buffer.ReadInt32();
            var numberReturned = buffer.ReadInt32();

            return new ReplyMessage(
                messageLength,
                requestId,
                responseTo,
                opCode,
                responseFlags,
                cursorId,
                startingFrom,
                numberReturned,
                buffer); // this is primed to the starting point of the documents
        }

        // public methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _documentsBuffer.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Reads the documents.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="readerSettings">The reader settings.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <returns></returns>
        public IEnumerable<TDocument> ReadDocuments<TDocument>(BsonBinaryReaderSettings readerSettings, IBsonSerializer serializer, IBsonSerializationOptions serializationOptions)
        {
            using (var reader = new BsonBinaryReader(_documentsBuffer, false, readerSettings))
            {
                for (int i = 0; i < _numberReturned; i++)
                {
                    yield return (TDocument)serializer.Deserialize(reader, typeof(TDocument), serializationOptions);
                }
            }
        }

        // private static methods
        private static int ReadMessageLength(Stream stream)
        {
            var bytes = new byte[4];
            var offset = 0;
            var count = 4;
            while (count > 0)
            {
                var bytesRead = stream.Read(bytes, offset, count);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += bytesRead;
                count -= bytesRead;
            }
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}