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
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents a connection pool.
    /// </summary>
    internal sealed class ConnectionPool : IConnectionPool
    {
        // private static fields
        private static int __nextId;

        // private fields
        private readonly object _maintainSizeLock = new object();
        private readonly IConnectionFactory _connectionFactory;
        private readonly DnsEndPoint _dnsEndPoint;
        private readonly IEventPublisher _events;
        private readonly int _id;
        private readonly ConcurrentQueue<PooledConnection> _pool;
        private readonly SemaphoreSlim _poolQueue;
        private readonly ConnectionPoolSettings _settings;
        private readonly string _toStringDescription;
        private readonly TraceSource _trace;
        private int _currentGenerationId;
        private Timer _sizeMaintenanceTimer;
        private int _state;
        private int _waitQueueSize;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPool" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="dnsEndPoint">The DNS end point.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="events">The events.</param>
        /// <param name="traceManager">The trace manager.</param>
        public ConnectionPool(ConnectionPoolSettings settings, DnsEndPoint dnsEndPoint, IConnectionFactory connectionFactory, IEventPublisher events, TraceManager traceManager)
        {
            Ensure.IsNotNull("settings", settings);
            Ensure.IsNotNull("dnsEndPoint", dnsEndPoint);
            Ensure.IsNotNull("connectionFactory", connectionFactory);
            Ensure.IsNotNull("events", events);
            Ensure.IsNotNull("traceManager", traceManager);

            _id = Interlocked.Increment(ref __nextId);
            _connectionFactory = connectionFactory;
            _dnsEndPoint = dnsEndPoint;
            _events = events;
            _settings = settings;
            _toStringDescription = string.Format("pool#{0}", _id);
            _trace = traceManager.GetTraceSource<ConnectionPool>();

            _pool = new ConcurrentQueue<PooledConnection>();
            _poolQueue = new SemaphoreSlim(_settings.MaxSize, _settings.MaxSize);
            _sizeMaintenanceTimer = new Timer(_ => MaintainSize());
        }

        // public properties
        /// <summary>
        /// Gets the DNS end point.
        /// </summary>
        public DnsEndPoint DnsEndPoint
        {
            get { return _dnsEndPoint; }
        }

        /// <summary>
        /// Gets the connection pool settings.
        /// </summary>
        public ConnectionPoolSettings Settings
        {
            get { return _settings; }
        }

        // private properties
        private int CurrentSize
        {
            get { return _settings.MaxSize - _poolQueue.CurrentCount + _pool.Count; }
        }

        private bool IsDisposed
        {
            get { return Interlocked.CompareExchange(ref _state, 0, 0) == State.Disposed; }
        }

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
        /// Gets a connection.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="cancellationToken">The <see cref="T:System.Threading.CancellationToken" /> to observe.</param>
        /// <returns>A connection.</returns>
        /// <exception cref="MongoDriverException">Too many threads are already waiting for a connection.</exception>
        public IConnection GetConnection(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfUninitialized();
            ThrowIfDisposed();
            try
            {
                var currentWaitQueueSize = Interlocked.Increment(ref _waitQueueSize);
                if (currentWaitQueueSize > _settings.MaxSize)
                {
                    _trace.TraceWarning("{0}: wait queue size of {1} exceeded.", _toStringDescription, _settings.MaxWaitQueueSize);
                    throw new MongoDriverException("Too many threads are already waiting for a connection.");
                }
                _events.Publish(new ConnectionPoolWaitQueueEnteredEvent(this));

                if (_poolQueue.Wait(timeout, cancellationToken))
                {
                    PooledConnection connection;
                    if (_pool.TryDequeue(out connection))
                    {
                        if (IsConnectionExpired(connection))
                        {
                            _trace.TraceVerbose("{0}: removed {1} because it was expired.", _toStringDescription, connection);
                            _events.Publish(new ConnectionRemovedFromPoolEvent(this, connection));
                            connection.Wrapped.Dispose();

                            connection = OpenNewConnection();
                            _trace.TraceVerbose("{0}: added {1}.", _toStringDescription, connection);
                            _events.Publish(new ConnectionAddedToPoolEvent(this, connection));
                        }

                        _events.Publish(new ConnectionCheckedOutOfPoolEvent(this, connection));
                        return connection;
                    }
                    else
                    {
                        // make sure connection is created successfully before incrementing poolSize
                        // connection will be opened later outside of the lock
                        connection = OpenNewConnection();
                        _trace.TraceVerbose("{0}: added {1}.", _toStringDescription, connection);
                        _trace.TraceInformation("{0}: pool size is {1}", _toStringDescription, CurrentSize);
                        _events.Publish(new ConnectionAddedToPoolEvent(this, connection));
                        _events.Publish(new ConnectionCheckedOutOfPoolEvent(this, connection));
                        return connection;
                    }
                }

                _trace.TraceWarning("{0}: timeout waiting for a connection. Timeout was {1}.", _toStringDescription, timeout);
                throw new MongoDriverException("Timeout waiting for a connection.");
            }
            finally
            {
                Interlocked.Decrement(ref _waitQueueSize);
                _events.Publish(new ConnectionPoolWaitQueueExitedEvent(this));
            }
        }

        public void Initialize()
        {
            ThrowIfDisposed();
            if (Interlocked.CompareExchange(ref _state, State.Initialized, State.Unitialized) == State.Unitialized)
            {
                _trace.TraceInformation("{0}: initialized with {1}.", _toStringDescription, _dnsEndPoint);
                _sizeMaintenanceTimer.Change(TimeSpan.Zero, _settings.SizeMaintenanceFrequency);
                _events.Publish(new ConnectionPoolOpenedEvent(this));
            }
        }

        // protected methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _state, State.Disposed) != State.Disposed && disposing)
            {
                Clear();
                // TODO: go through and dispose of all connections in the pool
                _sizeMaintenanceTimer.Dispose();
                _poolQueue.Dispose();
                _events.Publish(new ConnectionPoolClosedEvent(this));
                _trace.TraceInformation("{0}: closed with {1}.", _toStringDescription, _dnsEndPoint);
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
            _trace.TraceInformation("{0}: cleared.", _toStringDescription);
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

        private void EnsureMinSize()
        {
            int enteredPoolCount = 0;
            try
            {
                while (CurrentSize - enteredPoolCount < _settings.MinSize)
                {
                    if (!_poolQueue.Wait(TimeSpan.FromMilliseconds(20)))
                    {
                        break;
                    }

                    enteredPoolCount++;
                    var connection = OpenNewConnection();
                    _pool.Enqueue(connection);
                    _trace.TraceVerbose("{0}: added {1}.", _toStringDescription, connection);
                    _trace.TraceInformation("{0}: pool size is {1}.", _toStringDescription, CurrentSize);
                    _events.Publish(new ConnectionAddedToPoolEvent(this, connection));
                }
            }
            finally
            {
                if (enteredPoolCount > 0)
                {
                    try
                    {
                        _poolQueue.Release(enteredPoolCount);
                    }
                    catch (Exception ex)
                    {
                        _trace.TraceError(ex, "{0}: error releasing poolQueue.", _toStringDescription);
                    }
                }
            }
        }

        private bool IsConnectionExpired(PooledConnection connection)
        {
            // connection has been closed
            if (!connection.IsOpen)
            {
                return true;
            }

            // connection is no longer valid
            if (connection.Info.GenerationId != Interlocked.CompareExchange(ref _currentGenerationId, 0, 0))
            {
                return true;
            }

            // connection has lived too long
            var now = DateTime.UtcNow;
            if (_settings.ConnectionMaxLifeTime.TotalMilliseconds > -1 && now > connection.Info.OpenedAtUtc.Add(_settings.ConnectionMaxLifeTime))
            {
                return true;
            }

            // connection has been idle for too long
            if (_settings.ConnectionMaxIdleTime.TotalMilliseconds > -1 && now > connection.Info.LastUsedAtUtc.Add(_settings.ConnectionMaxIdleTime))
            {
                return true;
            }

            return false;
        }

        private void MaintainSize()
        {
            if (IsDisposed)
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

                PrunePool();
                EnsureMinSize();
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
            var info = new ConnectionInfo
            {
                GenerationId = Interlocked.CompareExchange(ref _currentGenerationId, 0, 0),
                OpenedAtUtc = DateTime.UtcNow
            };
            var connection = _connectionFactory.Create(_dnsEndPoint);
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
                    if (IsConnectionExpired(connection))
                    {
                        connection.Wrapped.Dispose();
                        _trace.TraceVerbose("{0}: removed {1} because it has expired.", _toStringDescription, connection);
                        _trace.TraceInformation("{0}: pool size is {1}.", _toStringDescription, CurrentSize);
                        _events.Publish(new ConnectionRemovedFromPoolEvent(this, connection));
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
                        _trace.TraceError(ex, "{0}: error releasing poolQueue.", _toStringDescription);
                    }
                }
            }
        }

        private void ReleaseConnection(PooledConnection connection)
        {
            if (IsDisposed)
            {
                // events could get out of wack because we
                // aren't raising events for connection checked in 
                // or connection removed.
                connection.Wrapped.Dispose();
                return;
            }

            _events.Publish(new ConnectionCheckedInToPoolEvent(this, connection));
            if (IsConnectionExpired(connection))
            {
                _poolQueue.Release();
                connection.Wrapped.Dispose();
                _trace.TraceVerbose("{0}: removed {1} because it has expired.", _toStringDescription, connection);
                _trace.TraceInformation("{0}: pool size is {1}.", _toStringDescription, _settings.MaxSize - _poolQueue.CurrentCount);
                _events.Publish(new ConnectionRemovedFromPoolEvent(this, connection));
            }
            else
            {
                _pool.Enqueue(connection);
                _poolQueue.Release();
            }
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
                throw new InvalidOperationException("ConnectionPool must be initialized.");
            }
        }

        private class State
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

        private sealed class PooledConnection : ConnectionBase
        {
            private ConnectionInfo _info;
            private ConnectionPool _pool;
            private IConnection _wrapped;
            private bool _disposed;

            public PooledConnection(IConnection connection, ConnectionPool pool, ConnectionInfo info)
            {
                _wrapped = connection;
                _pool = pool;
                _info = info;
            }

            public IConnection Wrapped
            {
                get
                {
                    ThrowIfDisposed();
                    return _wrapped;
                }
            }

            public override DnsEndPoint DnsEndPoint
            {
                get
                {
                    ThrowIfDisposed();
                    return _wrapped.DnsEndPoint;
                }
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

            protected override void Dispose(bool disposing)
            {
                if (disposing && _pool != null)
                {
                    _pool.ReleaseConnection(this);
                    _pool = null;
                    _info = null;
                    _wrapped = null;
                }
                _disposed = true;
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
