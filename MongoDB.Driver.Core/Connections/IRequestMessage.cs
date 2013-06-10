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

using System.IO;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// A message sent the server.
    /// </summary>
    public interface IRequestMessage
    {
        /// <summary>
        /// Gets the length of the message.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Gets the request id.
        /// </summary>
        int RequestId { get; }

        /// <summary>
        /// Writes the message to the stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        void Write(Stream stream);
    }
}