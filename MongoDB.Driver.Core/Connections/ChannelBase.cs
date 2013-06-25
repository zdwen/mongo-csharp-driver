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
using System.Net;
using MongoDB.Driver.Core.Protocol;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// A channel.
    /// </summary>
    public abstract class ChannelBase : IChannel
    {
        // public properties
        /// <summary>
        /// Gets the address.
        /// </summary>
        public abstract DnsEndPoint DnsEndPoint { get; }

        // public methods
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Receives a message.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>The reply.</returns>
        public abstract ReplyMessage Receive(ChannelReceiveArgs args);

        /// <summary>
        /// Sends the packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        public abstract void Send(IRequestPacket packet);

        // protected methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            // nothing to do...
        }
    }
}