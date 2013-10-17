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
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Support;
using MongoDB.Driver.Core.Security;
using MongoDB.Bson;
using System.Collections.Generic;
using MongoDB.Driver.Core.Security.Mechanisms;

namespace MongoDB.Driver.Core.Connections
{
    internal sealed class StreamConnection : ConnectionBase
    {
        // private static fields
        private static readonly TraceSource __trace = MongoTraceSources.Connections;
        private static readonly List<IAuthenticationProtocol> __authenticationProtocols;

        // private fields
        private readonly DnsEndPoint _dnsEndPoint;
        private readonly IEventPublisher _events;
        private readonly StreamConnectionSettings _settings;
        private readonly IStreamFactory _streamFactory;
        private bool _disposed;
        private ConnectionId _id;
        private State _state;
        private Stream _stream;
        private bool _disposeOnException;

        // constructors
        /// <summary>
        /// Initializes the <see cref="StreamConnection"/> class.
        /// </summary>
        static StreamConnection()
        {
            __authenticationProtocols = new List<IAuthenticationProtocol>
            {
                new MongoCRAuthenticationProtocol(),
                new SaslAuthenticationProtocol(new GssapiMechanism())
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamConnection" /> class.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <param name="streamFactory">The stream factory.</param>
        /// <param name="events">The events.</param>
        public StreamConnection(ServerId serverId, StreamConnectionSettings settings, DnsEndPoint dnsEndPoint, IStreamFactory streamFactory, IEventPublisher events)
        {
            Ensure.IsNotNull("serverId", serverId);
            Ensure.IsNotNull("settings", settings);
            Ensure.IsNotNull("dnsEndPoint", dnsEndPoint);
            Ensure.IsNotNull("streamFactory", streamFactory);
            Ensure.IsNotNull("events", events);

            _disposeOnException = false;
            _dnsEndPoint = dnsEndPoint;
            _events = events;
            _settings = settings;
            _streamFactory = streamFactory;
            _state = State.Initial;
            _id = new ConnectionId(serverId);
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
                return _dnsEndPoint;
            }
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        public override ConnectionId Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets a value indicating whether this connection is open.
        /// </summary>
        public override bool IsOpen
        {
            get { return _state == State.Open; }
        }

        // public methods
        /// <summary>
        /// Opens the connection.
        /// </summary>
        public override void Open()
        {
            ThrowIfDisposed();
            if (_state == State.Initial)
            {
                try
                {
                    _stream = _streamFactory.Create(_dnsEndPoint);
                    _state = State.Open;
                    DiscoverServerConnectionId();
                    _events.Publish(new ConnectionOpenedEvent(_id));
                    _disposeOnException = true;
                }
                catch (SocketException ex)
                {
                    HandleException(ex);
                    if (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        throw new MongoConnectTimeoutException(string.Format("Timed out opening a connection with {0}", _dnsEndPoint), ex);
                    }
                    else
                    {
                        throw new MongoSocketException("Error opening socket.", ex);
                    }
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                    throw new MongoDriverException("Unable to open connection.", ex);
                }
            }

            foreach (var credential in _settings.Credentials)
            {
                Authenticate(credential);
            }
        }

        /// <summary>
        /// Receives a message.
        /// </summary>
        /// <returns>The reply from the server.</returns>
        public override ReplyMessage Receive()
        {
            ThrowIfDisposed();
            ThrowIfNotOpen();

            try
            {
                var reply = ReplyMessage.ReadFrom(_stream);
                __trace.TraceVerbose("{0}: received message#{1} with {2} bytes.", this, reply.ResponseTo, reply.Length);
                _events.Publish(new ConnectionMessageReceivedEvent(_id, reply.ResponseTo, reply.Length));

                return reply;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    __trace.TraceWarning(ex, "{0}: timed out receiving message. Timeout = {1} milliseconds", this, _stream.ReadTimeout);
                    HandleException(ex);
                    throw new MongoSocketReadTimeoutException(string.Format("Timed out receiving message. Timeout = {0} milliseconds", _stream.ReadTimeout), ex);
                }
                else
                {
                    __trace.TraceWarning(ex, "{0}: error receiving message.", this);
                    HandleException(ex);
                    throw new MongoSocketException("Error receiving message", ex);
                }
            }
            catch (Exception ex)
            {
                __trace.TraceWarning(ex, "{0}: error receiving message.", this);
                HandleException(ex);
                if (ex is MongoDriverException)
                {
                    throw;
                }
                else
                {
                    throw new MongoDriverException("Error receiving message.", ex);
                }
            }
        }

        /// <summary>
        /// Sends the packet.
        /// </summary>
        public override void Send(IRequestPacket packet)
        {
            Ensure.IsNotNull("packet", packet);

            ThrowIfDisposed();
            ThrowIfNotOpen();

            try
            {
                packet.WriteTo(_stream);
                __trace.TraceVerbose("{0}: sent message#{1} with {2} bytes.", this, packet.LastRequestId, packet.Length);
                _events.Publish(new ConnectionPacketSendingEvent(_id, packet.LastRequestId, packet.Length));
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    __trace.TraceWarning(ex, "{0}: timed out sending message#{1}. Timeout = {2} milliseconds", this, packet.LastRequestId, _stream.ReadTimeout);
                    HandleException(ex);
                    throw new MongoSocketWriteTimeoutException(string.Format("Timed out sending message#{0}. Timeout = {1} milliseconds", packet.LastRequestId, _stream.ReadTimeout), ex);
                }
                else
                {
                    __trace.TraceWarning(ex, "{0}: error sending message#{1}.", this, packet.LastRequestId);
                    HandleException(ex);
                    throw new MongoSocketException(string.Format("Error sending message #{0}", packet.LastRequestId), ex);
                }
            }
            catch (Exception ex)
            {
                __trace.TraceWarning(ex, "{0}: error sending message#{1}.", this, packet.LastRequestId);
                HandleException(ex);

                if (ex is MongoDriverException)
                {
                    throw;
                }
                else
                {
                    throw new MongoDriverException(string.Format("Error sending message #{0}", packet.LastRequestId), ex);
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return _id.ToString();
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
                __trace.TraceInformation("{0}: closed.", this);
                _events.Publish(new ConnectionClosedEvent(_id));
                _state = State.Disposed;
                try { _stream.Close(); }
                catch { } // ignore exceptions
            }
            _disposed = true;
            base.Dispose(disposing);
        }

        // private methods
        private void Authenticate(MongoCredential credential)
        {
            foreach (var protocol in __authenticationProtocols)
            {
                if (protocol.CanUse(credential))
                {
                    protocol.Authenticate(this, credential);
                    return;
                }
            }

            var message = string.Format("Unable to find a security protocol to authenticate. The credential for source {0}, username {1} over mechanism {2} could not be authenticated.", credential.Identity.Source, credential.Identity.Username, credential.Mechanism);
            throw new MongoSecurityException(message);
        }

        private void DiscoverServerConnectionId()
        {
            try
            {
                var result = CommandHelper.RunCommand<BsonDocument>(
                    new DatabaseNamespace("admin"),
                    new BsonDocument("getLastError", 1),
                    this);
                var connectionId = result.GetValue("connectionId", _id.Value);
                var newId = new ConnectionId(_id.ServerId, ConnectionIdSource.Server, connectionId.ToInt32());

                __trace.TraceInformation("{0}: now {1}.", _id, newId);
                _id = newId;
            }
            catch
            {
                // if this fails, then so be it... we'll just use the 
                // local id generator version
            }
        }

        private void HandleException(Exception ex)
        {
            if (_disposeOnException)
            {
                // we'll always dispose for any error.
                Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ThrowIfNotOpen()
        {
            if (_state != State.Open)
            {
                throw new InvalidOperationException("The connection must be opened before it can be used.");
            }
        }

        // nested classes
        private enum State
        {
            Initial,
            Open,
            Disposed
        }
    }
}
