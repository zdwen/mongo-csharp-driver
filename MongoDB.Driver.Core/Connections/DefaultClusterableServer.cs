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
using System.Net;
using System.Text;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents a remove server which maintains an open connection for health checking.
    /// </summary>
    internal sealed class DefaultClusterableServer : ClusterableServerBase
    {
        // private static fields
        private static int __nextId;

        // private fields
        private readonly object _descriptionUpdateLock = new object();
        private readonly object _disposingLock = new object();
        private readonly DnsEndPoint _dnsEndPoint;
        private readonly ServerDescription _connectingDescription;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IChannelProvider _channelProvider;
        private readonly Timer _descriptionUpdateTimer;
        private readonly IEventPublisher _events;
        private readonly int _id;
        private readonly PingTimeAggregator _pingTimeAggregator;
        private readonly DefaultClusterableServerSettings _settings;
        private readonly string _toStringDescription;
        private readonly TraceSource _trace;
        private IConnection _updateDescriptionConnection;
        private int _state;
        private ServerDescription _currentDescription; // only read and written with Interlocked...

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultClusterableServer" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <param name="channelProvider">The channel provider.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="events">The events.</param>
        /// <param name="traceManager">The trace manager.</param>
        public DefaultClusterableServer(DefaultClusterableServerSettings settings, DnsEndPoint dnsEndPoint, IChannelProvider channelProvider, IConnectionFactory connectionFactory, IEventPublisher events, TraceManager traceManager)
        {
            Ensure.IsNotNull("settings", settings);
            Ensure.IsNotNull("dnsEndPoint", dnsEndPoint);
            Ensure.IsNotNull("connectionProvider", channelProvider);
            Ensure.IsNotNull("connectionFactory", connectionFactory);
            Ensure.IsNotNull("events", events);
            Ensure.IsNotNull("traceManager", traceManager);

            _id = Interlocked.Increment(ref __nextId);
            _settings = settings;
            _dnsEndPoint = dnsEndPoint;
            _events = events;
            _pingTimeAggregator = new PingTimeAggregator(5);
            _toStringDescription = string.Format("server#{0}", _id);
            _trace = traceManager.GetTraceSource<DefaultClusterableServer>();

            _connectingDescription = new ServerDescription(
                averagePingTime: TimeSpan.MaxValue,
                buildInfo: null,
                dnsEndPoint: dnsEndPoint,
                maxDocumentSize: _settings.MaxDocumentSizeDefault,
                maxMessageSize: _settings.MaxMessageSizeDefault,
                replicaSetInfo: null,
                status: ServerStatus.Connecting,
                type: ServerType.Unknown);

            _currentDescription = new ServerDescription(
                averagePingTime: TimeSpan.MaxValue,
                buildInfo: null,
                dnsEndPoint: dnsEndPoint,
                maxDocumentSize: _settings.MaxDocumentSizeDefault,
                maxMessageSize: _settings.MaxMessageSizeDefault,
                replicaSetInfo: null,
                status: ServerStatus.Disconnected,
                type: ServerType.Unknown);

            _channelProvider = channelProvider;
            _connectionFactory = connectionFactory;
            _descriptionUpdateTimer = new Timer(o => UpdateDescription());
        }

        // public properties
        /// <summary>
        /// Gets the description.
        /// </summary>
        public override ServerDescription Description
        {
            get { return Interlocked.CompareExchange(ref _currentDescription, null, null); }
        }

        // public events
        public override event EventHandler<ServerDescriptionChangedEventArgs<ServerDescription>> DescriptionUpdated;

        // public methods
        public override IServerChannel GetChannel(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfUninitialized();
            ThrowIfDisposed();
            return new ServerChannel(this, _channelProvider.GetChannel(timeout, cancellationToken));
        }

        public override void Initialize()
        {
            ThrowIfDisposed();
            if (Interlocked.CompareExchange(ref _state, State.Initialized, State.Unitialized) == State.Unitialized)
            {
                _trace.TraceInformation("{0}: initialized with {1}.", _toStringDescription, _dnsEndPoint);
                OnDescriptionUpdated(_connectingDescription);
                _channelProvider.Initialize();
                _descriptionUpdateTimer.Change(TimeSpan.Zero, _settings.ConnectRetryFrequency);
            }
        }

        public override void Invalidate()
        {
            ThrowIfUninitialized();
            ThrowIfDisposed();
            OnDescriptionUpdated(_connectingDescription);
            ThreadPool.QueueUserWorkItem(_ => UpdateDescription());
        }

        // protected methods
        protected override void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _state, State.Disposed) != State.Disposed && disposing)
            {
                lock (_disposingLock)
                {
                    _descriptionUpdateTimer.Dispose();
                    _channelProvider.Dispose();
                }

                var description = new ServerDescription(
                    averagePingTime: TimeSpan.MaxValue,
                    buildInfo: null,
                    dnsEndPoint: _dnsEndPoint,
                    maxDocumentSize: _settings.MaxDocumentSizeDefault,
                    maxMessageSize: _settings.MaxMessageSizeDefault,
                    replicaSetInfo: null,
                    status: ServerStatus.Disposed,
                    type: ServerType.Unknown);

                _trace.TraceInformation("{0}: closed with {1}.", _toStringDescription, _dnsEndPoint);
                OnDescriptionUpdated(description);
            }
        }

        // private methods
        private void HandleException(Exception ex)
        {
            // NOTE: we will not end up in this method when an error occurs in UpdateDescription
            // because the ServerConnection private class is not being used internally.
            if (ex is MongoSocketException)
            {
                // if we experienced some socket issue, let's referesh our state
                // immediately.
                ThreadPool.QueueUserWorkItem(o => UpdateDescription());
            }
        }

        private ServerDescription LookupDescription()
        {
            if (_updateDescriptionConnection == null || !_updateDescriptionConnection.IsOpen)
            {
                if (_updateDescriptionConnection != null)
                {
                    // make sure we close this up correctly...
                    _updateDescriptionConnection.Dispose();
                }
                _updateDescriptionConnection = _connectionFactory.Create(_dnsEndPoint);
                _updateDescriptionConnection.Open();
            }

            var isMasterCommand = new BsonDocument("ismaster", 1);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var isMasterResult = CommandHelper.RunCommand<IsMasterResult>("admin", isMasterCommand, _updateDescriptionConnection);
            stopwatch.Stop();
            _pingTimeAggregator.Include(stopwatch.Elapsed);

            if (!isMasterResult.Ok)
            {
                throw new MongoOperationException("Command 'ismaster' failed.", isMasterResult.Response);
            }

            var buildInfoCommand = new BsonDocument("buildinfo", 1);
            var buildInfoResult = CommandHelper.RunCommand<CommandResult>("admin", buildInfoCommand, _updateDescriptionConnection);
            if (!buildInfoResult.Ok)
            {
                throw new MongoOperationException("Command 'buildinfo' failed.", buildInfoResult.Response);
            }
            var buildInfo = ServerBuildInfo.FromCommandResult(buildInfoResult);

            return new ServerDescription(
                averagePingTime: _pingTimeAggregator.Average,
                buildInfo: buildInfo,
                dnsEndPoint: _dnsEndPoint,
                maxDocumentSize: isMasterResult.GetMaxDocumentSize(_settings.MaxDocumentSizeDefault),
                maxMessageSize: isMasterResult.GetMaxMessageSize(_settings.MaxDocumentSizeDefault, _settings.MaxMessageSizeDefault),
                replicaSetInfo: isMasterResult.GetReplicaSetInfo(_updateDescriptionConnection.DnsEndPoint.AddressFamily),
                status: ServerStatus.Connected,
                type: isMasterResult.ServerType);
        }

        private void ThrowIfDisposed()
        {
            if (Interlocked.CompareExchange(ref _state, 0, 0) == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ThrowIfUninitialized()
        {
            if (Interlocked.CompareExchange(ref _state, 0, 0) == State.Unitialized)
            {
                throw new InvalidOperationException("DefaultClusterableServer must be initialized.");
            }
        }

        private void UpdateDescription()
        {
            bool lockTaken = false;

            try
            {
                // if we are already inside, we don't want to wait and then do this again.
                // instead, just exit and take the result of the current operation.
                lockTaken = Monitor.TryEnter(_descriptionUpdateLock, TimeSpan.Zero);
                if (!lockTaken)
                {
                    return;
                }

                ServerDescription description = null;
                lock (_disposingLock)
                {
                    if (Interlocked.CompareExchange(ref _state, 0, 0) == State.Initialized)
                    {
                        try
                        {
                            _trace.TraceVerbose("{0}: checking health.", _toStringDescription);
                            description = LookupDescription();

                            // we want to use the presumably longer frequency for normal status updates.
                            _descriptionUpdateTimer.Change(_settings.HeartbeatFrequency, _settings.HeartbeatFrequency);
                        }
                        catch(Exception ex)
                        {
                            // we want to catch every exception because this is occuring on a background
                            // thread and any unhandled exceptions could crash the process.

                            bool takeDownServer = true;
                            if (Description.Status == ServerStatus.Connected)
                            {
                                // if we used to be connected and we had an error, let's immediately try this
                                // again in case it is just a one-time deal...
                                _trace.TraceWarning(ex, "{0}: unable to communicate with server. Trying again immediately.", _toStringDescription);

                                try
                                {
                                    description = LookupDescription();

                                    // we want to use the presumably longer frequency for normal status updates.
                                    _descriptionUpdateTimer.Change(_settings.HeartbeatFrequency, _settings.HeartbeatFrequency);
        
                                    takeDownServer = false;
                                }
                                catch(Exception ex2)
                                {
                                    // we want the takeDownServer code below to report the next error 
                                    // since the previous one was already reported
                                    ex = ex2;
                                } 
                            }

                            if (takeDownServer)
                            {
                                _trace.TraceWarning(ex, "{0}: unable to communicate with server.", _toStringDescription);
                                description = _connectingDescription;

                                // we want to use the presumably shorter frequency
                                // when we are supposed to be connected but are not.
                                _descriptionUpdateTimer.Change(_settings.ConnectRetryFrequency, _settings.ConnectRetryFrequency);
                            }
                        }
                    }
                }

                if (description != null)
                {
                    OnDescriptionUpdated(description);
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(_descriptionUpdateLock);
                }
            }
        }

        private void OnDescriptionUpdated(ServerDescription description)
        {
            var oldDescription = Interlocked.Exchange(ref _currentDescription, description);

            bool areEqual = AreDescriptionsEqual(oldDescription, description);
            if (!areEqual)
            {
                var descriptionString = GetServerDescriptionString(description);
                _events.Publish(new ServerDescriptionUpdatedEvent(this));
                _trace.TraceInformation("{0}: description updated - {1}.", _toStringDescription, descriptionString);
                var args = new ServerDescriptionChangedEventArgs<ServerDescription>(description);
                if (DescriptionUpdated != null)
                {
                    DescriptionUpdated(this, args);
                }
            }
        }

        private bool AreDescriptionsEqual(ServerDescription a, ServerDescription b)
        {
            return a.AveragePingTime == b.AveragePingTime &&
                a.Status == b.Status &&
                a.Type == b.Type &&
                AreReplicaSetsEqual(a.ReplicaSetInfo, b.ReplicaSetInfo);
        }

        private bool AreReplicaSetsEqual(ReplicaSetInfo a, ReplicaSetInfo b)
        {
            if (a == null && b == null)
            {
                return true;
            }
            else if (a == null || b == null)
            {
                return false;
            }

            return a.Name == b.Name &&
                a.Primary != null && a.Primary.Equals(b.Primary) &&
                a.Members.SequenceEqual(b.Members) &&
                a.Tags.SequenceEqual(b.Tags);
        }

        private string GetServerDescriptionString(ServerDescription description)
        {
            var builder = new StringBuilder();
            builder.AppendFormat("Type={0},Status={1},PingTime={2}", description.Type, description.Status, description.AveragePingTime);
            if (description.ReplicaSetInfo != null)
            {
                var members = string.Join(",", description.ReplicaSetInfo.Members);
                builder.AppendFormat(",Primary={0},Members=[{1}]", description.ReplicaSetInfo.Primary, members);
                if (description.ReplicaSetInfo.Tags.Any())
                {
                    var tags = string.Join(",", description.ReplicaSetInfo.Tags.Select(x => string.Format("{0}={1}", x.Key, x.Value)));
                    builder.AppendFormat(",Tags=[{0}]", tags);
                }
            }

            return builder.ToString();
        }

        private class State
        {
            public const int Unitialized = 0;
            public const int Initialized = 1;
            public const int Disposed = 2;
        }

        private sealed class ServerChannel : ServerChannelBase
        {
            private readonly DefaultClusterableServer _server;
            private readonly IChannel _wrapped;
            private bool _disposed;

            public ServerChannel(DefaultClusterableServer server, IChannel wrapped)
            {
                _server = server;
                _wrapped = wrapped;
            }

            public override DnsEndPoint DnsEndPoint
            {
                get
                {
                    ThrowIfDisposed();
                    return _wrapped.DnsEndPoint;
                }
            }

            public override ServerDescription Server
            {
                get
                {
                    ThrowIfDisposed();
                    return _server.Description;
                }
            }

            public override ReplyMessage Receive(ChannelReceiveArgs args)
            {
                ThrowIfDisposed();
                try
                {
                    return _wrapped.Receive(args);
                }
                catch (Exception ex)
                {
                    _server.HandleException(ex);
                    throw;
                }
            }

            public override void Send(IRequestPacket packet)
            {
                ThrowIfDisposed();
                try
                {
                    _wrapped.Send(packet);
                }
                catch (Exception ex)
                {
                    _server.HandleException(ex);
                    throw;
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (!_disposed && disposing)
                {
                    _wrapped.Dispose();
                }

                _disposed = true;
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
}