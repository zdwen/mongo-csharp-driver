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


namespace MongoDB.Driver.Core.Protocol.Messages
{
    /// <summary>
    /// Operation codes for the MongoDB wire protocol.
    /// </summary>
    public enum OpCode
    {
        /// <summary>
        /// OpCode for a Reply message.
        /// </summary>
        Reply = 1,

        /// <summary>
        /// OpCode for a Message message.
        /// </summary>
        Message = 1000,

        /// <summary>
        /// OpCode for a Update message.
        /// </summary>
        Update = 2001,

        /// <summary>
        /// OpCode for a Insert message.
        /// </summary>
        Insert = 2002,

        /// <summary>
        /// OpCode for a Query message.
        /// </summary>
        Query = 2004,

        /// <summary>
        /// OpCode for a GetMore message.
        /// </summary>
        GetMore = 2005,

        /// <summary>
        /// OpCode for a Delete message.
        /// </summary>
        Delete = 2006,

        /// <summary>
        /// OpCode for a KillCursors message.
        /// </summary>
        KillCursors = 2007
    }
}
