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
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Channel that does not dispose of its wrapped channel.  This is used to protect the driver from a bad user decision.
    /// </summary>
    internal sealed class DisposalProtectedChannel : IChannel
    {
        // private fields
        private readonly IChannel _wrapped;
        private bool _disposed;

        // constructors
        public DisposalProtectedChannel(IChannel wrapped)
        {
            Ensure.IsNotNull("wrapped", wrapped);

            _wrapped = wrapped;
        }

        // public properties
        public DnsEndPoint DnsEndPoint
        {
            get { return _wrapped.DnsEndPoint; }
        }

        // public methods
        public void Dispose()
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public ReplyMessage Receive(ChannelReceiveArgs args)
        {
            return _wrapped.Receive(args);
        }

        public void Send(IRequestPacket packet)
        {
            _wrapped.Send(packet);
        }

        // private methods
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}