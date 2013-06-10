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


namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Parameters for receiving a message.
    /// </summary>
    public class ReceiveMessageParameters
    {
        // private fields
        private readonly int _requestId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageParameters" /> class.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        public ReceiveMessageParameters(int requestId)
        {
            _requestId = requestId;
        }

        // public methods
        /// <summary>
        /// Gets the request id.
        /// </summary>
        public int RequestId
        {
            get { return _requestId; }
        }
    }
}