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

using System.Text;
using System.Threading;
using MongoDB.Bson.IO;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Base class for building a <see cref="BsonBufferedRequestMessage"/>.
    /// </summary>
    public abstract class BsonBufferedRequestMessageBuilder
    {
        // private static fields
        private static int _nextId;
        protected static readonly UTF8Encoding __encoding = new UTF8Encoding(false, true);

        // private fields
        private readonly int _requestId;
        private readonly int _responseTo;
        private readonly OpCode _opCode;
        private int _messageLength;
        private int _messageStartPosition;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonBufferedRequestMessageBuilder" /> class.
        /// </summary>
        /// <param name="opCode">The op code.</param>
        protected BsonBufferedRequestMessageBuilder(OpCode opCode)
        {
            _requestId = Interlocked.Increment(ref _nextId);
            _responseTo = 0;
            _opCode = opCode;
        }

        // public properties
        /// <summary>
        /// Gets the message start position.
        /// </summary>
        public int MessageStartPosition
        {
            get { return _messageStartPosition; }
        }

        /// <summary>
        /// Gets the length of the message.
        /// </summary>
        public int MessageLength
        {
            get { return _messageLength; }
        }

        // public methods
        /// <summary>
        /// Adds to request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void AddToRequest(BsonBufferedRequestMessage request)
        {
            _messageStartPosition = request.Buffer.Position;

            request.Buffer.WriteInt32(0); // messageLength (will be backpatched)
            request.Buffer.WriteInt32(_requestId); // requestID
            request.Buffer.WriteInt32(_responseTo); // responseTo
            request.Buffer.WriteInt32((int)_opCode); //opCode

            Write(request.Buffer);

            _messageLength = request.Buffer.Position - _messageStartPosition;

            request.Buffer.Backpatch(_messageStartPosition, _messageLength);
            request.RequestId = _requestId;
        }

        /// <summary>
        /// Removes from request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="byteCount">The byte count.</param>
        /// <returns>The bytes tha were removed from the message</returns>
        public byte[] RemoveFromRequest(BsonBufferedRequestMessage request, int byteCount)
        {
            request.Buffer.Position -= byteCount;
            var bytes = request.Buffer.ReadBytes(byteCount);
            request.Buffer.Position -= byteCount;
            request.Buffer.Length -= byteCount;

            ChangeMessageLength(request, _messageLength - byteCount);
            return bytes;
        }
        
        // protected methods
        /// <summary>
        /// Changes the length of the message.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="newLength">The new length.</param>
        protected void ChangeMessageLength(BsonBufferedRequestMessage request, int newLength)
        {
            _messageLength = newLength;
            request.Buffer.Backpatch(_messageStartPosition, _messageLength);
        }

        /// <summary>
        /// Writes the message to the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        protected abstract void Write(BsonBuffer buffer);
    }
}