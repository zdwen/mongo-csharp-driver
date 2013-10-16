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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Protocol.Messages;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents a connection pool.
    /// </summary>
    internal sealed class ConnectionPool : ConnectionPoolBase
    {
        // private static fields
        private static readonly TraceSource __trace = MongoTraceSources.Connections;

        // private fields
        private readonly object _maintainSizeLock = new object();
        private readonly IConnectionFactory _connectionFactory;
        private readonly DnsEndPoint _dnsEndPoint;
        private readonly IEventPublisher _events;
        private readonly ConcurrentQueue<PooledConnection> _pool;
        private readonly SemaphoreSlim _poolQueue;
        private readonly ServerId _serverId;
        private readonly ConnectionPoolSettings _settings;
        private readonly StateHelper _state;
        private int _currentGenerationId;
        private Timer _sizeMaintenanceTimer;
        private int _waitQueueSize;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPool" /> class.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="events">The events.</param>
        public ConnectionPool(ServerId serverId, ConnectionPoolSettings settings, DnsEndPoint dnsEndPoint, IConnectionFactory connectionFactory, IEventPublisher events)
        {
            Ensure.IsNotNull("serverIds", serverId);
            Ensure.IsNotNull("settings", settings);
            Ensure.IsNotNull("dnsEndPoint", dnsEndPoint);
            Ensure.IsNotNull("connectionFactory", connectionFactory);
            Ensure.IsNotNull("events", events);

            _connectionFactory = connectionFactory;
            _dnsEndPoint = dnsEndPoint;
            _events = events;
            _serverId = serverId;
            _settings = settings;
            _state = new StateHelper(State.Unitialized);

            _pool = new ConcurrentQueue<PooledConnection>();
            _poolQueue = new SemaphoreSlim(_settings.MaxSize, _settings.MaxSize);
            _sizeMaintenanceTimer = new Timer(_ => MaintainSize());

            __trace.TraceVerbose("{0}: {1}", this, _settings);
        }

        // public properties
        /// <summary>
        /// Gets the DNS end point.
        /// </summary>
        public override DnsEndPoint DnsEndPoint
        {
            get { return _dnsEndPoint; }
        }

        /// <summary>
        /// Gets the server identifier.
        /// </summary>
        public override ServerId ServerId
        {
            get { return _serverId; }
        }

        // private properties
        private int CurrentSize
        {
            get { return _settings.MaxSize - _poolQueue.CurrentCount + _pool.Count; }
        }

        // public methods
        /// <summary>
        /// Gets a connection.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> to observe.</param>
        /// <returns>A connection.</returns>
        /// <exception cref="MongoDriverException">Too many threads are already waiting for a connection.</exception>
        public override IConnection GetConnection(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfUninitialized();
            ThrowIfDisposed();
            bool enteredPool = false;
            try
            {
                var currentWaitQueueSize = Interlocked.Increment(ref _waitQueueSize);
                __trace.TraceVerbose("{0}: wait queue size is {1}.", this, currentWaitQueueSize);
                if (currentWaitQueueSize > _settings.MaxSize)
                {
                    __trace.TraceWarning("{0}: wait queue size of {1} exceeded.", this, _settings.MaxWaitQueueSize);
                    throw new MongoDriverException("Too many threads are already waiting for a connection.");
                }
                _events.Publish(new ConnectionPoolWaitQueueEnteredEvent(_serverId));

                enteredPool = _poolQueue.Wait(timeout, cancellationToken);
                if (enteredPool)
                {
                    PooledConnection connection;
                    if (_pool.TryDequeue(out connection))
                    {
                        string expirationReason;
                        if (IsConnectionExpired(connection, out expirationReason))
                        {
                            __trace.TraceInformation("{0}: removed {1} because {2}.", this, connection, expirationReason);
                            _events.Publish(new ConnectionRemovedFromPoolEvent(connection.Id));
                            connection.Dispose();

                            connection = OpenNewConnection();
                            __trace.TraceInformation("{0}: added {1}.", this, connection);
                            _events.Publish(new ConnectionAddedToPoolEvent(connection.Id));
                        }
                    }
                    else
                    {
                        connection = OpenNewConnection();
                        __trace.TraceInformation("{0}: added {1}.", this, connection);
                        __trace.TraceInformation("{0}: pool size is {1}", this, CurrentSize);
                        _events.Publish(new ConnectionAddedToPoolEvent(connection.Id));
                    }

                    _events.Publish(new ConnectionCheckedOutOfPoolEvent(connection.Id));
                    __trace.TraceVerbose("{0}: checking out {1}.", this, connection);
                    return new AcquiredConnection(connection, this);
                }

                __trace.TraceWarning("{0}: timeout waiting for a connection. Timeout was {1}.", this, timeout);
                throw new MongoDriverException("Timeout waiting for a connection.");
            }
            catch
            {
                if (enteredPool)
                {
                    try
                    {
                        _poolQueue.Release();
                    }
                    catch (Exception ex)
                    {
                        __trace.TraceError(ex, "{0}: error releasing pool queue.", this);
                    }
                }
                throw;
            }
            finally
            {
                Interlocked.Decrement(ref _waitQueueSize);
                _events.Publish(new ConnectionPoolWaitQueueExitedEvent(_serverId));
            }
        }

        public override void Initialize()
        {
            ThrowIfDisposed();
            if (_state.TryChange(State.Unitialized, State.Initialized))
            {
                __trace.TraceInformation("{0}: initialized.", this);
                _sizeMaintenanceTimer.Change(TimeSpan.Zero, _settings.SizeMaintenanceFrequency);
                _events.Publish(new ConnectionPoolOpenedEvent(_serverId, _settings));
            }
        }

        public override string ToString()
        {
            return _serverId.ToString();
        }

        // protected methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (_state.TryChange(State.Disposed) && disposing)
            {
                Clear();
                // TODO: go through and dispose of all connections in the pool
                _sizeMaintenanceTimer.Dispose();
                _poolQueue.Dispose();
                _events.Publish(new ConnectionPoolClosedEvent(_serverId));
                __trace.TraceInformation("{0}: closed.", this);
            }
        }

        // private methods
        private void Clear()
        {
            // Simply increasing the generation id will close connections
            // as they get released or as they are checked out.  This will
            // create predictable behavior and never have more connections
            // open than spots exist in the pool.
            Interlocked.Increment(ref _currentGenerationId);
            __trace.TraceInformation("{0}: cleared.", this);
        }

        private void EnsureMinSize()
        {
            while (CurrentSize < _settings.MinSize)
            {
                bool enteredPool = false;
                try
                {
                    enteredPool = _poolQueue.Wait(TimeSpan.FromMilliseconds(20));
                    if (!enteredPool)
                    {
                        return;
                    }

                    var connection = OpenNewConnection();
                    __trace.TraceInformation("{0}: added {1}.", this, connection);
                    __trace.TraceInformation("{0}: pool size is {1}.", this, CurrentSize);
                    _events.Publish(new ConnectionAddedToPoolEvent(connection.Id));
                    _pool.Enqueue(connection);
                }
                finally
                {
                    if (enteredPool)
                    {
                        try
                        {
                            _poolQueue.Release();
                        }
                        catch (Exception ex)
                        {
                            __trace.TraceError(ex, "{0}: error releasing poolQueue.", this);
                        }
                    }
                }
            }
        }

        private void HandleException(Exception ex)
        {
            // If we get an exception, we'll tear down the pool...
            // The only real exceptions we'll get in this area are
            // socket exceptions or authentication exceptions, both
            // would dictate the need to do this.

            if (!(ex is MongoDriverException))
            {
                Clear();
            }
        }

        private bool IsConnectionExpired(PooledConnection connection, out string reason)
        {
            // connection has been closed
            if (!connection.IsOpen)
            {
                reason = "it is closed";
                return true;
            }

            // connection is no longer valid
            if (connection.Info.GenerationId != Interlocked.CompareExchange(ref _currentGenerationId, 0, 0))
            {
                reason = "its generation is too old";
                return true;
            }

            // connection has lived too long
            var now = DateTime.UtcNow;
            if (_settings.ConnectionMaxLifeTime.TotalMilliseconds > -1 && now > connection.Info.OpenedAtUtc.Add(_settings.ConnectionMaxLifeTime))
            {
                reason = "it has lived too long";
                return true;
            }

            // connection has been idle for too long
            if (_settings.ConnectionMaxIdleTime.TotalMilliseconds > -1 && now > connection.Info.LastUsedAtUtc.Add(_settings.ConnectionMaxIdleTime))
            {
                reason = "it has been idle too long";
                return true;
            }

            reason = null;
            return false;
        }

        private void MaintainSize()
        {
            if (_state.Current == State.Disposed)
            {
                return;
            }

            bool lockTaken = false;
            try
            {
                lockTaken = Monitor.TryEnter(_maintainSizeLock, TimeSpan.Zero);
                if (!lockTaken)
                {
                    return;
                }

                using (__trace.TraceActivity("{0}: Maintaining Size", this))
                {
                    PrunePool();
                    EnsureMinSize();
                }
            }
            catch
            {
                // eat all these exceptions.  Any that leak would cause an application crash.
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(_maintainSizeLock);
                }
            }
        }

        private PooledConnection OpenNewConnection()
        {
            // we are going to set both LastUsedAtUtc and OpenedAtUtc.
            // we want to make sure that connections that get checked
            // out and not used are not thrown away.  This is also
            // true for the connections created in EnsureMinSize.
            // OpenedAtUtc will be roughly correct
            // LastUsedAtUtc will get changed when it's used.
            var info = new ConnectionInfo
            {
                GenerationId = Interlocked.CompareExchange(ref _currentGenerationId, 0, 0),
                LastUsedAtUtc = DateTime.UtcNow,
                OpenedAtUtc = DateTime.UtcNow
            };
            var connection = _connectionFactory.Create(_serverId, _dnsEndPoint);
            connection.Open();
            return new PooledConnection(connection, this, info);
        }

        private void PrunePool()
        {
            bool enteredPool = false;
            try
            {
                // if it takes too long to enter the pool, then the pool is fully utilized
                // and we don't want to mess with it.
                enteredPool = _poolQueue.Wait(TimeSpan.FromMilliseconds(20));
                if (!enteredPool)
                {
                    return;
                }

                PooledConnection connection;
                int numAttempts = 0;
                int count = _pool.Count; // TODO:  maybe do count / 2
                // we don't want to do this forever...
                while (numAttempts < count && _pool.TryDequeue(out connection))
                {
                    string expirationReason;
                    if (IsConnectionExpired(connection, out expirationReason))
                    {
                        connection.Dispose();
                        __trace.TraceInformation("{0}: removed {1} because {2}.", this, connection, expirationReason);
                        __trace.TraceInformation("{0}: pool size is {1}.", this, CurrentSize);
                        _events.Publish(new ConnectionRemovedFromPoolEvent(connection.Id));
                        break; // only going to kill off one connection per-round
                    }
                    else
                    {
                        // put it back on... of course, it might be expired by the time the 
                        // next guy pulls it off, but maybe not.
                        _pool.Enqueue(connection);
                    }
                    numAttempts++;
                }
            }
            finally
            {
                if (enteredPool)
                {
                    try
                    {
                        _poolQueue.Release();
                    }
                    catch (Exception ex)
                    {
                        __trace.TraceError(ex, "{0}: error releasing poolQueue.", this);
                    }
                }
            }
        }

        private void ReleaseConnection(PooledConnection connection)
        {
            if (_state.Current == State.Disposed)
            {
                // events could get out of wack because we
                // aren't raising events for connection checked in 
                // or connection removed.
                connection.Dispose();
                return;
            }

            _events.Publish(new ConnectionCheckedInToPoolEvent(connection.Id));
            string expirationReason;
            if (IsConnectionExpired(connection, out expirationReason))
            {
                _poolQueue.Release();
                connection.Dispose();
                __trace.TraceInformation("{0}: removed {1} because {2}.", this, connection, expirationReason);
                __trace.TraceInformation("{0}: pool size is {1}.", this, _settings.MaxSize - _poolQueue.CurrentCount);
                _events.Publish(new ConnectionRemovedFromPoolEvent(connection.Id));
            }
            else
            {
                __trace.TraceVerbose("{0}: returning {1}.", this, connection, expirationReason);
                _pool.Enqueue(connection);
                _poolQueue.Release();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_state.Current == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ThrowIfUninitialized()
        {
            if (_state.Current == State.Unitialized)
            {
                throw new InvalidOperationException("ConnectionPool must be initialized.");
            }
        }

        private static class State
        {
            public const int Unitialized = 0;
            public const int Initialized = 1;
            public const int Disposed = 2;
        }

        private class ConnectionInfo
        {
            public DateTime LastUsedAtUtc;
            public DateTime OpenedAtUtc;
            public int GenerationId;
        }

        private sealed class AcquiredConnection : ConnectionBase
        {
            private readonly PooledConnection _wrapped;
            private readonly ConnectionPool _pool;
            private bool _disposed;

            public AcquiredConnection(PooledConnection connection, ConnectionPool pool)
            {
                _wrapped = connection;
                _pool = pool;
            }

            public override DnsEndPoint DnsEndPoint
            {
                get
                {
                    ThrowIfDisposed();
                    return _wrapped.DnsEndPoint;
                }
            }

            public override ConnectionId Id
            {
                get { return _wrapped.Id; }
            }

            public override bool IsOpen
            {
                get
                {
                    ThrowIfDisposed();
                    return _wrapped.IsOpen;
                }
            }

            public override void Open()
            {
                ThrowIfDisposed();
                _wrapped.Open();
            }

            public override ReplyMessage Receive()
            {
                ThrowIfDisposed();
                return _wrapped.Receive();
            }

            public override void Send(IRequestPacket packet)
            {
                ThrowIfDisposed();
                _wrapped.Send(packet);
            }

            public override string ToString()
            {
                return _wrapped.ToString();
            }

            protected override void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        _pool.ReleaseConnection(_wrapped);
                    }
                    _disposed = true;
                }
                base.Dispose(disposing);
            }

            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
            }
        }

        private sealed class PooledConnection : ConnectionBase
        {
            private readonly IConnection _wrapped;
            private readonly ConnectionPool _pool;
            private readonly ConnectionInfo _info;
            private bool _disposed;

            public PooledConnection(IConnection connection, ConnectionPool pool, ConnectionInfo info)
            {
                _wrapped = connection;
                _pool = pool;
                _info = info;
            }

            public override DnsEndPoint DnsEndPoint
            {
                get
                {
                    ThrowIfDisposed();
                    return _wrapped.DnsEndPoint;
                }
            }

            public override ConnectionId Id
            {
                get { return _wrapped.Id; }
            }

            public ConnectionInfo Info
            {
                get
                {
                    ThrowIfDisposed();
                    return _info;
                }
            }

            public override bool IsOpen
            {
                get
                {
                    ThrowIfDisposed();
                    return _wrapped.IsOpen;
                }
            }

            public override void Open()
            {
                ThrowIfDisposed();
                _wrapped.Open();
            }

            public override ReplyMessage Receive()
            {
                ThrowIfDisposed();
                try
                {
                    _info.LastUsedAtUtc = DateTime.UtcNow;
                    return _wrapped.Receive();
                }
                catch (ObjectDisposedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _pool.HandleException(ex);
                    throw;
                }
            }

            public override void Send(IRequestPacket packet)
            {
                ThrowIfDisposed();
                try
                {
                    _info.LastUsedAtUtc = DateTime.UtcNow;
                    _wrapped.Send(packet);
                }
                catch (Exception ex)
                {
                    _pool.HandleException(ex);
                    throw;
                }
            }

            public override string ToString()
            {
                return _wrapped.ToString();
            }

            protected override void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        _wrapped.Dispose();
                    }
                    _disposed = true;
                }
                base.Dispose(disposing);
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
