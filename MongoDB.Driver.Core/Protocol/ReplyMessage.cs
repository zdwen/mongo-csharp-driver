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
    /// Represents a Reply message from the server.
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
        private readonly Stream _stream;

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
        /// <param name="stream">The Stream.</param>
        public ReplyMessage(
            int length, 
            int requestId, 
            int responseTo, 
            OpCode opCode, 
            ReplyFlags responseFlags,
            long cursorId,
            int startingFrom,
            int numberReturned,
            Stream stream)
        {
            _length = length;
            _requestId = requestId;
            _responseTo = responseTo;
            _opCode = opCode;
            _flags = responseFlags;
            _cursorId = cursorId;
            _startingFrom = startingFrom;
            _numberReturned = numberReturned;
            _stream = stream;
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
        /// Reads a ReplyMessage from a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>A ReplyMessage.</returns>
        public static ReplyMessage ReadFrom(Stream stream)
        {
            var byteBuffer = ByteBufferFactory.LoadLengthPrefixedDataFrom(stream);
            var byteBufferStream = new ByteBufferStream(byteBuffer, ownsByteBuffer: true);
            var byteBufferStreamReader = new BsonStreamReader(byteBufferStream, Utf8Helper.StrictUtf8Encoding);

            var messageLength = byteBufferStreamReader.ReadInt32();
            var requestId = byteBufferStreamReader.ReadInt32();
            var responseTo = byteBufferStreamReader.ReadInt32();
            var opCode = (OpCode)byteBufferStreamReader.ReadInt32();
            var responseFlags = (ReplyFlags)byteBufferStreamReader.ReadInt32();
            var cursorId = byteBufferStreamReader.ReadInt64();
            var startingFrom = byteBufferStreamReader.ReadInt32();
            var numberReturned = byteBufferStreamReader.ReadInt32();

            return new ReplyMessage(
                messageLength,
                requestId,
                responseTo,
                opCode,
                responseFlags,
                cursorId,
                startingFrom,
                numberReturned,
                byteBufferStream); // the stream is positioned at the first result document
        }

        // public methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _stream.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Deserializes the documents.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="serializer">The serializer.</param>
        /// <param name="serializationOptions">The serialization options.</param>
        /// <param name="readerSettings">The reader settings.</param>
        /// <returns></returns>
        public IEnumerable<TDocument> DeserializeDocuments<TDocument>(IBsonSerializer serializer, IBsonSerializationOptions serializationOptions, BsonBinaryReaderSettings readerSettings)
        {
            using (var bsonReader = new BsonBinaryReader(_stream, readerSettings))
            {
                for (int i = 0; i < _numberReturned; i++)
                {
                    yield return (TDocument)serializer.Deserialize(bsonReader, typeof(TDocument), serializationOptions);
                }
            }
        }
    }
}