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
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// An <see cref="IRequestMessage"/> backed by a <see cref="BsonBuffer"/>.
    /// </summary>
    public sealed class BsonBufferedRequestMessage : IRequestMessage, IDisposable
    {
        // private fields
        private readonly BsonBuffer _buffer;
        private int _requestId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonBufferedRequestMessage" /> class.
        /// </summary>
        public BsonBufferedRequestMessage()
        {
            _buffer = new BsonBuffer();
        }

        /// <summary>
        /// Gets the buffer.
        /// </summary>
        public BsonBuffer Buffer
        {
            get { return _buffer; }
        }

        /// <summary>
        /// Gets the length of the message.
        /// </summary>
        public int Length
        {
            get { return _buffer.Length; }
        }

        /// <summary>
        /// Gets the request id.
        /// </summary>
        public int RequestId
        {
            get { return _requestId; }
            set { _requestId = value; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _buffer.Dispose();
        }

        /// <summary>
        /// Writes the message to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void Write(Stream stream)
        {
            _buffer.WriteTo(stream);
        }
    }
}