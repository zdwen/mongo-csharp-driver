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
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Sessions;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Base class for an operation.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public abstract class OperationBase<TResult> : IOperation<TResult>
    {
        // private fields
        private CancellationToken _cancellationToken;
        private bool _closeSessionOnExecute;
        private BsonBinaryReaderSettings _readerSettings;
        private ISession _session;
        private TimeSpan _timeout;
        private BsonBinaryWriterSettings _writerSettings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="OperationBase{TResult}" /> class.
        /// </summary>
        protected OperationBase()
        {
            _cancellationToken = CancellationToken.None;
            _readerSettings = BsonBinaryReaderSettings.Defaults;
            _timeout = TimeSpan.FromSeconds(30);
            _writerSettings = BsonBinaryWriterSettings.Defaults;
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
        /// Gets or sets a value indicating whether [close session on execute].
        /// </summary>
        public bool CloseSessionOnExecute
        {
            get { return _closeSessionOnExecute; }
            set { _closeSessionOnExecute = value; }
        }

        /// <summary>
        /// Gets or sets the reader settings.
        /// </summary>
        public BsonBinaryReaderSettings ReaderSettings
        {
            get { return _readerSettings; }
            set { _readerSettings = value; }
        }

        /// <summary>
        /// Gets or sets the session.
        /// </summary>
        public ISession Session
        {
            get { return _session; }
            set { _session = value; }
        }

        /// <summary>
        /// Gets or sets the timeout.
        /// </summary>
        public TimeSpan Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        /// <summary>
        /// Gets or sets the writer settings.
        /// </summary>
        public BsonBinaryWriterSettings WriterSettings
        {
            get { return _writerSettings; }
            set { _writerSettings = value; }
        }

        // public methods
        /// <summary>
        /// Executes the operation.
        /// </summary>
        /// <returns>An operation channel provider.</returns>
        public abstract TResult Execute();

        // protected methods
        /// <summary>
        /// Creates the session channel provider.
        /// </summary>
        /// <param name="serverSelector">The server selector.</param>
        /// <param name="isQuery">if set to <c>true</c> the operation is a query.</param>
        /// <returns>A session channel provider.</returns>
        protected IServerChannelProvider CreateServerChannelProvider(IServerSelector serverSelector, bool isQuery)
        {
            var options = new CreateServerChannelProviderArgs(serverSelector, isQuery)
            {
                CancellationToken = _cancellationToken,
                DisposeSession = _closeSessionOnExecute,
                Timeout = _timeout
            };

            return Session.CreateServerChannelProvider(options);
        }

        /// <summary>
        /// Adjusts the reader settings based on server specific settings.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>The adjusted reader settings</returns>
        protected BsonBinaryReaderSettings GetServerAdjustedReaderSettings(ServerDescription server)
        {
            Ensure.IsNotNull("server", server);

            var readerSettings = _readerSettings.Clone();
            readerSettings.MaxDocumentSize = server.MaxDocumentSize;
            return readerSettings;
        }

        // protected methods
        /// <summary>
        /// Adjusts the writer settings based on server specific settings.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>The adjusted writer settings.</returns>
        protected BsonBinaryWriterSettings GetServerAdjustedWriterSettings(ServerDescription server)
        {
            Ensure.IsNotNull("server", server);

            var writerSettings = _writerSettings.Clone();
            writerSettings.MaxDocumentSize = server.MaxDocumentSize;
            return writerSettings;
        }

        /// <summary>
        /// Validates the required properties.
        /// </summary>
        protected virtual void ValidateRequiredProperties()
        {
            Ensure.IsNotNull("ReaderSettings", _readerSettings);
            Ensure.IsNotNull("Session", _session);
            Ensure.IsInfiniteOrZeroOrPositive("Timeout", _timeout);
            Ensure.IsNotNull("WriterSettings", _writerSettings);
        }
    }
}
