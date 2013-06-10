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
    /// Server that does not dispose of its wrapped server.  This is used to protect the driver from a bad user decision.
    /// </summary>
    internal sealed class DisposalProtectedServer : IServer
    {
        // private fields
        private readonly IServer _wrapped;
        private bool _disposed;

        // constructors
        public DisposalProtectedServer(IServer wrapped)
        {
            _wrapped = wrapped;
        }

        // public properties
        public ServerDescription Description
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.Description;
            }
        }

        // public methods
        public void Dispose()
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public IServerChannel GetChannel(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _wrapped.GetChannel(timeout, cancellationToken);
        }

        public void Initialize()
        {
            // do nothing...
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
