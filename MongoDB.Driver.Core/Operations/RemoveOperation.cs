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
    /// Represents a Remove operation.
    /// </summary>
    public sealed class RemoveOperation : WriteOperationBase<WriteConcernResult>
    {
        // private fields
        private DeleteFlags _flags;
        private object _query;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveOperation" /> class.
        /// </summary>
        public RemoveOperation()
        {
        }

        // public properties
        /// <summary>
        /// Gets or sets the delete flags.
        /// </summary>
        public DeleteFlags Flags
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

        // public methods
        /// <summary>
        /// Executes the remove.
        /// </summary>
        /// <returns>A WriteConcern result (or null if WriteConcern was not enabled).</returns>
        public override WriteConcernResult Execute()
        {
            EnsureRequiredProperties();

            using (var channelProvider = CreateServerChannelProvider(WritableServerSelector.Instance, false))
            {
                var readerSettings = GetServerAdjustedReaderSettings(channelProvider.Server);
                var writerSettings = GetServerAdjustedWriterSettings(channelProvider.Server);

                var protocol = new DeleteProtocol(
                    collection: Collection,
                    flags: _flags,
                    query: _query,
                    readerSettings: GetServerAdjustedReaderSettings(channelProvider.Server),
                    writeConcern: WriteConcern,
                    writerSettings: GetServerAdjustedWriterSettings(channelProvider.Server));

                using (var channel = channelProvider.GetChannel(Timeout, CancellationToken))
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
        }
    }
}
