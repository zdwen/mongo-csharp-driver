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
using System.IO;
using System.Text;
using System.Threading;
using MongoDB.Bson.IO;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Represents a Request message.
    /// </summary>
    public abstract class RequestMessage
    {
        // private static fields
        private static int __lastRequestId;

        // private fields
        private readonly int _requestId;
        private readonly OpCode _opCode;
        private int _messageLength;
        private int _messageStartPosition;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMessage" /> class.
        /// </summary>
        /// <param name="opCode">The op code.</param>
        protected RequestMessage(OpCode opCode)
        {
            _requestId = Interlocked.Increment(ref __lastRequestId);
            _opCode = opCode;
        }

        // public properties
        /// <summary>
        /// Gets the length of the message.
        /// </summary>
        public int MessageLength
        {
            get { return _messageLength; }
        }

        /// <summary>
        /// Gets the message start position.
        /// </summary>
        public int MessageStartPosition
        {
            get { return _messageStartPosition; }
        }

        /// <summary>
        /// Gets the request id.
        /// </summary>
        /// <value>
        /// The request id.
        /// </value>
        public int RequestId
        {
            get { return _requestId; }
        }

        // public methods
        /// <summary>
        /// Writes the Request message to a Stream.
        /// </summary>
        /// <param name="stream">The Stream.</param>
        public void WriteTo(Stream stream)
        {
            _messageStartPosition = (int)stream.Position;

            var streamWriter = new BsonStreamWriter(stream);
            WriteMessageHeaderTo(streamWriter);
            WriteBodyTo(streamWriter);

            BackpatchMessageLength(stream);
        }

        // protected methods
        /// <summary>
        /// Backpatches the message length field.
        /// </summary>
        /// <param name="stream">The stream.</param>
        protected void BackpatchMessageLength(Stream stream)
        {
            var streamWriter = new BsonStreamWriter(stream);
            var currentPosition = (int)stream.Position;
            _messageLength = currentPosition - _messageStartPosition;
            stream.Position = _messageStartPosition;
            streamWriter.WriteBsonInt32(_messageLength);
            stream.Position = currentPosition;
        }

        /// <summary>
        /// Writes the body of the message a stream.
        /// </summary>
        /// <param name="streamWriter">The stream.</param>
        protected abstract void WriteBodyTo(BsonStreamWriter streamWriter);

        // private methods
        private void WriteMessageHeaderTo(BsonStreamWriter streamWriter)
        {
            streamWriter.WriteBsonInt32(0); // messageLength (will be backpatched later)
            streamWriter.WriteBsonInt32(_requestId);
            streamWriter.WriteBsonInt32(0); // responseTo not used in requests sent by the client
            streamWriter.WriteBsonInt32((int)_opCode);
        }
    }
}