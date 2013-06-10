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

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Builds a <see cref="BsonBufferedRequestMessage"/> to kill cursors.
    /// </summary>
    public sealed class KillCursorsMessageBuilder : BsonBufferedRequestMessageBuilder
    {
        // private fields
        private readonly long[] _cursorIds;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="KillCursorsMessageBuilder" /> class.
        /// </summary>
        /// <param name="cursorIds">The cursor ids.</param>
        public KillCursorsMessageBuilder(long[] cursorIds)
            : base(OpCode.KillCursors)
        {
            _cursorIds = cursorIds;
        }
        
        // protected methods
        /// <summary>
        /// Writes the message to the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        protected override void Write(BsonBuffer buffer)
        {
            buffer.WriteInt32(0); // ZERO
            buffer.WriteInt32(_cursorIds.Length); // numberOfCursorIDs
            for (int i = 0; i < _cursorIds.Length; i++)
            {
                buffer.WriteInt64(_cursorIds[i]); // cursorIDs
            }
        }
    }
}