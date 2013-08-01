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

using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Sessions;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents an Update operation.
    /// </summary>
    public sealed class UpdateOperation : WriteOperationBase<WriteConcernResult>
    {
        // private fields
        private bool _checkUpdateDocument;
        private UpdateFlags _flags;
        private object _query;
        private object _update;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateOperation" /> class.
        /// </summary>
        public UpdateOperation()
        {
            _checkUpdateDocument = true;
        }

        // public properties
        /// <summary>
        /// Gets or sets a value indicating whether to check the update document.  What does this mean???
        /// </summary>
        public bool CheckUpdateDocument
        {
            get { return _checkUpdateDocument; }
            set { _checkUpdateDocument = value; }
        }

        /// <summary>
        /// Gets or sets the update flags.
        /// </summary>
        public UpdateFlags Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }

        /// <summary>
        /// Gets or sets the query object.
        /// </summary>
        public object Query
        {
            get { return _query; }
            set { _query = value; }
        }

        /// <summary>
        /// Gets or sets the update object.
        /// </summary>
        public object Update
        {
            get { return _update; }
            set { _update = value; }
        }

        // public methods
        /// <summary>
        /// Executes the update.
        /// </summary>
        /// <returns>A WriteConcern result (or null if WriteConcern was not enabled).</returns>
        public override WriteConcernResult Execute()
        {
            EnsureRequiredProperties();

            using (var channelProvider = CreateServerChannelProvider(WritableServerSelector.Instance, false))
            {
                var protocol = new UpdateProtocol(
                    checkUpdateDocument: _checkUpdateDocument,
                    collection: Collection,
                    flags: _flags,
                    query: _query,
                    readerSettings: GetServerAdjustedReaderSettings(channelProvider.Server),
                    update: _update,
                    writeConcern: WriteConcern,
                    writerSettings: GetServerAdjustedWriterSettings(channelProvider.Server));

                using(var channel = channelProvider.GetChannel(Timeout, CancellationToken))
                {
                    return protocol.Execute(channel);
                }
            }
        }

        // protected methods
        /// <summary>
        /// Ensures that required properties have been set or provides intelligent defaults.
        /// </summary>
        protected override void EnsureRequiredProperties()
        {
            base.EnsureRequiredProperties();
            Ensure.IsNotNull("Query", _query);
            Ensure.IsNotNull("Update", _update);
        }
    }
}
