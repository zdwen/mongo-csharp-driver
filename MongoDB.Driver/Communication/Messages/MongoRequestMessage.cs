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
using System.Threading;
using MongoDB.Bson.IO;

namespace MongoDB.Driver.Internal
{
    internal abstract class MongoRequestMessage : MongoMessage
    {
        // private static fields
        private static int __lastRequestId = 0;

        // private fields
        private BsonBinaryWriterSettings _writerSettings;
        private int _messageStartPosition = -1; // start position in buffer for backpatching messageLength

        // constructors
        protected MongoRequestMessage(
            MessageOpcode opcode,
            BsonBinaryWriterSettings writerSettings)
            : base(opcode)
        {
            if (writerSettings == null)
            {
                throw new ArgumentNullException("writerSettings");
            }

            _writerSettings = writerSettings;
            RequestId = Interlocked.Increment(ref __lastRequestId);
        }

        // public properties
        public BsonBinaryWriterSettings WriterSettings
        {
            get { return _writerSettings; }
        }

        // internal methods
        internal void WriteTo(Stream stream)
        {
            // normally this method is only called once (from MongoConnection.SendMessage)
            // but in the case of InsertBatch it is called before SendMessage is called to initialize the message so that AddDocument can be called
            // therefore we need the if statement to ignore subsequent calls from SendMessage
            if (_messageStartPosition == -1)
            {
                var streamWriter = new BsonStreamWriter(stream, WriterSettings.Encoding);
                _messageStartPosition = (int)stream.Position;
                WriteMessageHeaderTo(streamWriter);
                WriteBodyTo(streamWriter);
                BackpatchMessageLength(stream);
            }
        }

        // protected methods
        protected void BackpatchMessageLength(Stream stream)
        {
            MessageLength = (int)(stream.Position - _messageStartPosition);
            Backpatch(stream, _messageStartPosition, MessageLength);
        }

        protected abstract void WriteBodyTo(BsonStreamWriter streamWriter);

        // private methods
        private void Backpatch(Stream stream, int position, int value)
        {
            var streamWriter = new BsonStreamWriter(stream, Utf8Helper.StrictUtf8Encoding);
            var currentPosition = stream.Position;
            stream.Position = position;
            streamWriter.WriteInt32(value);
            stream.Position = currentPosition;
        }
    }
}
