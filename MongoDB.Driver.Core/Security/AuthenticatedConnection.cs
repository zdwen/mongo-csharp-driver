using System;
using System.Net;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Protocol;

namespace MongoDB.Driver.Core.Security
{
    /// <summary>
    /// An authenticated <see cref="MongoDB.Driver.Core.Connections.IConnection"/>
    /// </summary>
    public sealed class AuthenticatedConnection : ConnectionBase
    {
        // private fields
        private readonly AuthenticationSettings _settings;
        private readonly IConnection _wrapped;
        private bool _disposed;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticatedConnection" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="wrapped">The wrapped connection.</param>
        public AuthenticatedConnection(AuthenticationSettings settings, IConnection wrapped)
        {
            _settings = settings;
            _wrapped = wrapped;
        }

        // public properties
        /// <summary>
        /// Gets the address.
        /// </summary>
        public override DnsEndPoint DnsEndPoint
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.DnsEndPoint;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this connection is open.
        /// </summary>
        public override bool IsOpen
        {
            get
            {
                ThrowIfDisposed();
                return _wrapped.IsOpen;
            }
        }

        /// <summary>
        /// Opens the connection.
        /// </summary>
        public override void Open()
        {
            ThrowIfDisposed();
            _wrapped.Open();

            foreach (var credential in _settings.Credentials)
            {
                Authenticate(credential);
            }
        }

        /// <summary>
        /// Receives a message.
        /// </summary>
        /// <returns>The reply.</returns>
        public override ReplyMessage Receive()
        {
            ThrowIfDisposed();
            return _wrapped.Receive();
        }

        /// <summary>
        /// Sends the packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        public override void Send(IRequestNetworkPacket packet)
        {
            ThrowIfDisposed();
            _wrapped.Send(packet);
        }

        // protected methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _wrapped.Dispose();
            }

            _disposed = true;
        }

        // private methods
        private void Authenticate(MongoCredential credential)
        {
            foreach (var protocol in _settings.Protocols)
            {
                if (protocol.CanUse(credential))
                {
                    protocol.Authenticate(_wrapped, credential);
                    return;
                }
            }

            var message = string.Format("Unable to find a security protocol to authenticate. The credential for source {0}, username {1} over mechanism {2} could not be authenticated.", credential.Source, credential.Username, credential.Mechanism);
            throw new MongoSecurityException(message);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}