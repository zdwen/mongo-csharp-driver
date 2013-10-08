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

using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Diagnostics.PerformanceCounters;
using MongoDB.Driver.Core.Events;

namespace MongoDB.Driver.Core.Diagnostics
{
    /// <summary>
    /// Event listeners use to implement performance counters.
    /// </summary>
    public class PerformanceCounterEventListeners :
        IEventListener<ConnectionClosedEvent>,
        IEventListener<ConnectionOpenedEvent>,
        IEventListener<ConnectionMessageReceivedEvent>,
        IEventListener<ConnectionPacketSendingEvent>,
        IEventListener<ConnectionAddedToPoolEvent>,
        IEventListener<ConnectionCheckedInToPoolEvent>,
        IEventListener<ConnectionCheckedOutOfPoolEvent>,
        IEventListener<ConnectionPoolClosedEvent>,
        IEventListener<ConnectionPoolOpenedEvent>,
        IEventListener<ConnectionRemovedFromPoolEvent>,
        IEventListener<ConnectionPoolWaitQueueEnteredEvent>,
        IEventListener<ConnectionPoolWaitQueueExitedEvent>
    {
        // private fields
        private readonly string _applicationName;
        private readonly PerformanceCounterPackage _appPackage;
        private readonly ConcurrentDictionary<string, PerformanceCounterPackage> _packages;
        private readonly ConcurrentDictionary<string, ConnectionPerformanceRecorder> _connectionRecorders;
        private readonly ConcurrentDictionary<string, ConnectionPoolPerformanceRecorder> _connectionPoolRecorders;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceCounterEventListeners" /> class.
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        public PerformanceCounterEventListeners(string applicationName)
        {
            _applicationName = applicationName;
            _packages = new ConcurrentDictionary<string, PerformanceCounterPackage>();
            _appPackage = GetAppPackage();
            _connectionRecorders = new ConcurrentDictionary<string, ConnectionPerformanceRecorder>();
            _connectionPoolRecorders = new ConcurrentDictionary<string, ConnectionPoolPerformanceRecorder>();
        }

        // public static methods
        /// <summary>
        /// Installs the performance counters.
        /// </summary>
        public static void Install()
        {
            PerformanceCounterPackage.Install();
        }

        // public methods
        /// <summary>
        /// Applies the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Apply(ConnectionClosedEvent @event)
        {
            ConnectionPerformanceRecorder recorder;
            if (_connectionRecorders.TryRemove(@event.ConnectionId, out recorder))
            {
                recorder.Closed();
            }
        }

        /// <summary>
        /// Applies the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Apply(ConnectionOpenedEvent @event)
        {
            var serverPackage = GetServerPackage(@event.Address);
            ConnectionPerformanceRecorder recorder = new ConnectionPerformanceRecorder(_appPackage, serverPackage);
            if (_connectionRecorders.TryAdd(@event.ConnectionId, recorder))
            {
                recorder.Opened();
            }
        }

        /// <summary>
        /// Applies the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Apply(ConnectionMessageReceivedEvent @event)
        {
            ConnectionPerformanceRecorder recorder;
            if (_connectionRecorders.TryGetValue(@event.ConnectionId, out recorder))
            {
                recorder.MessageReceived(@event.ResponseTo, @event.Size);
            }
        }

        /// <summary>
        /// Applies the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Apply(ConnectionPacketSendingEvent @event)
        {
            ConnectionPerformanceRecorder recorder;
            if (_connectionRecorders.TryGetValue(@event.ConnectionId, out recorder))
            {
                recorder.PacketSent(@event.RequestId, @event.Size);
            }
        }

        /// <summary>
        /// Applies the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Apply(ConnectionAddedToPoolEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ConnectionPoolId, out recorder))
            {
                recorder.ConnectionAdded();
            }
        }

        /// <summary>
        /// Applies the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Apply(ConnectionCheckedInToPoolEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ConnectionPoolId, out recorder))
            {
                recorder.ConnectionCheckedIn();
            }
        }

        /// <summary>
        /// Applies the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Apply(ConnectionCheckedOutOfPoolEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ConnectionPoolId, out recorder))
            {
                recorder.ConnectionCheckedOut();
            }
        }

        /// <summary>
        /// Applies the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Apply(ConnectionPoolClosedEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryRemove(@event.ConnectionPoolId, out recorder))
            {
                recorder.ConnectionRemoved();
            }
        }

        /// <summary>
        /// Applies the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Apply(ConnectionPoolOpenedEvent @event)
        {
            var serverPackage = GetServerPackage(@event.Address);
            ConnectionPoolPerformanceRecorder recorder = new ConnectionPoolPerformanceRecorder(@event.Settings.MaxSize, _appPackage, serverPackage);
            if (_connectionPoolRecorders.TryAdd(@event.ConnectionPoolId, recorder))
            {
                recorder.Opened();
            }
        }

        /// <summary>
        /// Applies the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Apply(ConnectionRemovedFromPoolEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ConnectionPoolId, out recorder))
            {
                recorder.ConnectionRemoved();
            }
        }

        /// <summary>
        /// Applies the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Apply(ConnectionPoolWaitQueueEnteredEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ConnectionPoolId, out recorder))
            {
                recorder.WaitQueueEntered();
            }
        }

        /// <summary>
        /// Applies the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        public void Apply(ConnectionPoolWaitQueueExitedEvent @event)
        {
            ConnectionPoolPerformanceRecorder recorder;
            if (_connectionPoolRecorders.TryGetValue(@event.ConnectionPoolId, out recorder))
            {
                recorder.WaitQueueExited();
            }
        }

        // private methods
        private PerformanceCounterPackage CreatePackage(string instanceName)
        {
            return new PerformanceCounterPackage(instanceName);
        }

        private PerformanceCounterPackage GetAppPackage()
        {
            return _packages.GetOrAdd(_applicationName, CreatePackage);
        }

        private PerformanceCounterPackage GetServerPackage(DnsEndPoint dnsEndPoint)
        {
            var server = string.Format("{0}_{1}", _applicationName, dnsEndPoint.ToString());
            return _packages.GetOrAdd(server, CreatePackage);
        }
    }
}