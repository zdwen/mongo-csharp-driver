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
using System.Threading;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Sessions
{
    /// <summary>
    /// Options for ISession.CreateServerChannelProvider.
    /// </summary>
    public class CreateServerChannelProviderArgs
    {
        // private fields
        private readonly bool _isQuery;
        private readonly IServerSelector _serverSelector;
        private CancellationToken _cancellationToken;
        private bool _disposeSession;
        private TimeSpan _timeout;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateServerChannelProviderArgs" /> class.
        /// </summary>
        /// <param name="serverSelector">The server selector.</param>
        /// <param name="isQuery">if set to <c>true</c> [is query].</param>
        public CreateServerChannelProviderArgs(IServerSelector serverSelector, bool isQuery)
        {
            _serverSelector = serverSelector;
            _isQuery = isQuery;

            _cancellationToken = CancellationToken.None;
            _disposeSession = false;
            _timeout = TimeSpan.FromSeconds(30);
        }

        // public properties
        /// <summary>
        /// Gets or sets the cancellation token.
        /// </summary>
        public CancellationToken CancellationToken
        {
            get { return _cancellationToken; }
            set { _cancellationToken = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to dispose of the session when the IOperationChannelProvider is disposed.
        /// </summary>
        public bool DisposeSession
        {
            get { return _disposeSession; }
            set { _disposeSession = value; }
        }

        /// <summary>
        /// Gets a value indicating whether to create an IOperationChannelProvider for a query.
        /// </summary>
        public bool IsQuery
        {
            get { return _isQuery; }
        }

        /// <summary>
        /// Gets the server selector.
        /// </summary>
        public IServerSelector ServerSelector
        {
            get { return _serverSelector; }
        }

        /// <summary>
        /// Gets or sets the timeout.
        /// </summary>
        public TimeSpan Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }
    }
}