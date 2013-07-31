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

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// A logical connection to a remote server.
    /// </summary>
    public abstract class ClusterableServerBase : IClusterableServer
    {
        /// <summary>
        /// Gets the description.
        /// </summary>
        public abstract ServerDescription Description { get; }

        /// <summary>
        /// Occurs when the description is updated.
        /// </summary>
        public abstract event EventHandler<ServerDescriptionChangedEventArgs<ServerDescription>> DescriptionUpdated;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets a channel.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> to observe.</param>
        /// <returns>A channel.</returns>
        public abstract IChannel GetChannel(TimeSpan timeout, CancellationToken cancellationToken);

        /// <summary>
        /// Initializes the server.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Invalidates this instance's description.
        /// </summary>
        public abstract void Invalidate();

        // protected methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Nothing to do...
        }
    }
}