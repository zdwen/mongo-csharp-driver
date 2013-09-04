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
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Settings for the <see cref="ConnectionPoolChannelProviderFactory"/>.
    /// </summary>
    public sealed class ConnectionPoolSettings
    {
        // public static fields
        /// <summary>
        /// The default settings.
        /// </summary>
        public static readonly ConnectionPoolSettings Defaults = new Builder().Build();

        // private fields
        private readonly TimeSpan _connectionMaxIdleTime;
        private readonly TimeSpan _connectionMaxLifeTime;
        private readonly int _maxSize;
        private readonly int _maxWaitQueueSize;
        private readonly int _minSize;
        private readonly TimeSpan _sizeMaintenanceFrequency;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPoolSettings" /> class.
        /// </summary>
        /// <param name="connectionMaxIdleTime">The maximum amount of time a connection is allowed to not be used.</param>
        /// <param name="connectionMaxLifeTime">The maximum amount of time a connection is allowed to be used.</param>
        /// <param name="maxSize">The maximum size of the connection pool.</param>
        /// <param name="minSize">The minimum size of the connection pool.</param>
        /// <param name="sizeMaintenanceFrequency">The frequency to ensure the min and max size of the pool.</param>
        /// <param name="maxWaitQueueSize">Size of the max wait queue.</param>
        public ConnectionPoolSettings(TimeSpan connectionMaxIdleTime, TimeSpan connectionMaxLifeTime, int maxSize, int minSize, TimeSpan sizeMaintenanceFrequency, int maxWaitQueueSize)
        {
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero("connectionMaxIdleTime", connectionMaxIdleTime);
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero("connectionMaxLifeTime", connectionMaxLifeTime);
            Ensure.IsGreaterThan("maxSize", maxSize, -1);
            Ensure.IsGreaterThan("minSize", minSize, -1);
            if (minSize > maxSize)
            {
                throw new ArgumentException("Must be less than or equal to maxSize", "minSize");
            }
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero("sizeMaintenanceFrequency", sizeMaintenanceFrequency);
            Ensure.IsGreaterThan("maxWaitQueueSize", maxWaitQueueSize, -1);

            _connectionMaxIdleTime = connectionMaxIdleTime;
            _connectionMaxLifeTime = connectionMaxLifeTime;
            _maxSize = maxSize;
            _minSize = minSize;
            _sizeMaintenanceFrequency = sizeMaintenanceFrequency;
            _maxWaitQueueSize = maxWaitQueueSize;
        }

        // public properties
        /// <summary>
        /// The maximum amount of time a connection is allowed to not be used.
        /// </summary>
        public TimeSpan ConnectionMaxIdleTime
        {
            get { return _connectionMaxIdleTime; }
        }

        /// <summary>
        /// The maximum amount of time a connection is allowed to be used.
        /// </summary>
        public TimeSpan ConnectionMaxLifeTime
        {
            get { return _connectionMaxLifeTime; }
        }

        /// <summary>
        /// The maximum size of the connection pool.
        /// </summary>
        public int MaxSize
        {
            get { return _maxSize; }
        }

        /// <summary>
        /// The maximum size of the wait queue.
        /// </summary>
        public int MaxWaitQueueSize
        {
            get { return _maxWaitQueueSize; }
        }

        /// <summary>
        /// The minimum size of the connection pool.
        /// </summary>
        public int MinSize
        {
            get { return _minSize; }
        }

        /// <summary>
        /// The frequency to ensure the min and max size of the pool.
        /// </summary>
        public TimeSpan SizeMaintenanceFrequency
        {
            get { return _sizeMaintenanceFrequency; }
        }

        // public methods
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                "{{ ConnectionMaxIdleTime: '{0}', ConnectionMaxLifeTime: '{1}', MaxSize: {2}, MinSize: {3}, MaxWaitQueueSize: {4} }}",
                _connectionMaxIdleTime,
                _connectionMaxLifeTime,
                _maxSize,
                _minSize,
                _maxWaitQueueSize);
        }

        // public static methods
        /// <summary>
        /// A method used to build up settings.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <returns>The built settings.</returns>
        public static ConnectionPoolSettings Create(Action<Builder> callback)
        {
            var builder = new Builder();
            callback(builder);
            return builder.Build();
        }

        /// <summary>
        /// Used to build up <see cref="ConnectionPoolSettings"/>.
        /// </summary>
        public sealed class Builder
        {
            private TimeSpan _connectionMaxIdleTime;
            private TimeSpan _connectionMaxLifeTime;
            private int _maxSize;
            private int _minSize;
            private TimeSpan _sizeMaintenanceFrequency;
            private int _waitQueueSize;

            internal Builder()
            {
                _connectionMaxIdleTime = TimeSpan.FromMinutes(10);
                _connectionMaxLifeTime = TimeSpan.FromMinutes(30);
                _maxSize = 100;
                _minSize = 0;
                _sizeMaintenanceFrequency = TimeSpan.FromMinutes(1);
                _waitQueueSize = 500; // maxSize * 5
            }

            internal ConnectionPoolSettings Build()
            {
                return new ConnectionPoolSettings(
                    _connectionMaxIdleTime,
                    _connectionMaxLifeTime,
                    _maxSize,
                    _minSize,
                    _sizeMaintenanceFrequency,
                    _waitQueueSize);
            }

            /// <summary>
            /// Sets the maximum amount of time a connection is allowed to not be used.
            /// </summary>
            /// <param name="time">The time.</param>
            public void SetConnectionMaxIdleTime(TimeSpan time)
            {
                _connectionMaxIdleTime = time;
            }

            /// <summary>
            /// Sets the maximum amount of time a connection is allowed to be used.
            /// </summary>
            /// <param name="time">The time.</param>
            public void SetConnectionMaxLifeTime(TimeSpan time)
            {
                _connectionMaxLifeTime = time;
            }

            /// <summary>
            /// Sets the maximum size of the connection pool.
            /// </summary>
            /// <param name="size">The size.</param>
            public void SetMaxSize(int size)
            {
                _maxSize = size;
            }

            /// <summary>
            /// Sets the maximum size of the wait queue.
            /// </summary>
            /// <param name="size">The size.</param>
            public void SetMaxWaitQueueSize(int size)
            {
                _waitQueueSize = size;
            }

            /// <summary>
            /// Sets the maximum size of the connection pool.
            /// </summary>
            /// <param name="size">The size.</param>
            public void SetMinSize(int size)
            {
                _minSize = size;
            }

            /// <summary>
            /// Sets the frequency to ensure the min and max size of the pool.
            /// </summary>
            public void SetSizeMaintenanceFrequency(TimeSpan frequency)
            {
                _sizeMaintenanceFrequency = frequency;
            }
        }
    }
}