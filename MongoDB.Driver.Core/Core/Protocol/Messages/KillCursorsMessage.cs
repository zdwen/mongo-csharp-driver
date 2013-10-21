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

using MongoDB.Bson.IO;

namespace MongoDB.Driver.Core.Protocol.Messages
{
    /// <summary>
    /// Represents a KillCursors message.
    /// </summary>
    public sealed class KillCursorsMessage : RequestMessage
    {
        // private fields
        private readonly long[] _cursorIds;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="KillCursorsMessage" /> class.
        /// </summary>
        /// <param name="cursorIds">The cursor ids.</param>
        public KillCursorsMessage(long[] cursorIds)
            : base(OpCode.KillCursors)
        {
            _cursorIds = cursorIds;
        }
        
        // protected methods
        /// <summary>
        /// Writes the body of the message a stream.
        /// </summary>
        /// <param name="streamWriter">The stream.</param>
        protected override void WriteBodyTo(BsonStreamWriter streamWriter)
        {
            streamWriter.WriteInt32(0); // reserved
            streamWriter.WriteInt32(_cursorIds.Length); // numberOfCursorIDs
            for (int i = 0; i < _cursorIds.Length; i++)
            {
                streamWriter.WriteInt64(_cursorIds[i]);
            }
        }
    }
}