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
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Represents an <see cref="IRequestPacket"/> that contains one or more Messages that have been written to a backing Stream.
    /// </summary>
    public sealed class BufferedRequestPacket : IRequestPacket, IDisposable
    {
        // private fields
        private readonly Stream _stream;
        private int _lastRequestId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BufferedRequestPacket" /> class.
        /// </summary>
        public BufferedRequestPacket()
            : this(new MemoryStream())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferedRequestPacket"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public BufferedRequestPacket(Stream stream)
        {
            _stream = stream;
        }

        // public properties
        /// <summary>
        /// Gets the length of the message.
        /// </summary>
        public int Length
        {
            get { return (int)_stream.Length; }
        }

        /// <summary>
        /// Gets the request id of the last message.
        /// </summary>
        public int LastRequestId
        {
            get { return _lastRequestId; }
        }

        /// <summary>
        /// Gets the buffer.
        /// </summary>
        public Stream Stream
        {
            get { return _stream; }
        }

        // public methods
        /// <summary>
        /// Adds a message to the packet.
        /// </summary>
        /// <param name="message">The message.</param>
        public void AddMessage(RequestMessage message)
        {
            message.WriteTo(_stream);
            _lastRequestId = message.RequestId;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _stream.Dispose();
        }

        /// <summary>
        /// Writes the buffered packet to another stream.
        /// </summary>
        /// <param name="destination">The destination stream.</param>
        public void WriteTo(Stream destination)
        {
            _stream.Position = 0;
            _stream.CopyTo(destination);
        }
    }
}