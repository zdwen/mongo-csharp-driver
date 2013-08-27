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
    /// Settings for the <see cref="ClusterableServerFactory"/>.
    /// </summary>
    public sealed class ClusterableServerSettings
    {
        // public static fields
        /// <summary>
        /// The default settings.
        /// </summary>
        public static readonly ClusterableServerSettings Defaults = new Builder().Build();

        // private fields
        private readonly TimeSpan _connectRetryFrequency;
        private readonly TimeSpan _heartbeatFrequency;
        private readonly int _maxDocumentSizeDefault;
        private readonly int _maxMessageSizeDefault;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterableServerSettings" /> class.
        /// </summary>
        /// <param name="connectRetryFrequency">The connect retry frequency.</param>
        /// <param name="heartbeatFrequency">The heartbeat frequency.</param>
        /// <param name="maxDocumentSizeDefault">The max document size default.</param>
        /// <param name="maxMessageSizeDefault">The max message size default.</param>
        public ClusterableServerSettings(TimeSpan connectRetryFrequency, TimeSpan heartbeatFrequency, int maxDocumentSizeDefault, int maxMessageSizeDefault)
        {
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero("connectRetryFrequency", connectRetryFrequency);
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero("heartbeatFrequency", heartbeatFrequency);
            Ensure.IsGreaterThan("maxDocumentSizeDefault", maxDocumentSizeDefault, 0);
            Ensure.IsGreaterThan("maxMessageSizeDefault", maxMessageSizeDefault, 0);
            
            _connectRetryFrequency = connectRetryFrequency;
            _heartbeatFrequency = heartbeatFrequency;
            _maxDocumentSizeDefault = maxDocumentSizeDefault;
            _maxMessageSizeDefault = maxMessageSizeDefault;
        }

        // public properties
        /// <summary>
        /// Gets the connect retry frequency.
        /// </summary>
        public TimeSpan ConnectRetryFrequency
        {
            get { return _connectRetryFrequency; }
        }

        /// <summary>
        /// Gets the heartbeat frequency.
        /// </summary>
        public TimeSpan HeartbeatFrequency
        {
            get { return _heartbeatFrequency; }
        }

        /// <summary>
        /// Gets the max document size default.
        /// </summary>
        public int MaxDocumentSizeDefault
        {
            get { return _maxDocumentSizeDefault; }
        }

        /// <summary>
        /// Gets the max message size default.
        /// </summary>
        public int MaxMessageSizeDefault
        {
            get { return _maxMessageSizeDefault; }
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
                "{{ ConnectRetryFrequency: '{0}', HeartbeatFrequency: '{1}', MaxDocumentSizeDefault: {2}, MaxMessageSizeDefault: {3} }}",
                _connectRetryFrequency,
                _heartbeatFrequency,
                _maxDocumentSizeDefault,
                _maxMessageSizeDefault);
        }

        // public static methods
        /// <summary>
        /// Creates the specified callback.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <returns>The built settings.</returns>
        public static ClusterableServerSettings Create(Action<Builder> callback)
        {
            var builder = new Builder();
            callback(builder);
            return builder.Build();
        }

        /// <summary>
        /// Used to build up DefaultServerSettings.
        /// </summary>
        public class Builder
        {
            private TimeSpan _connectRetryFrequency;
            private TimeSpan _heartbeatFrequency;
            private int _maxDocumentSizeDefault;
            private int _maxMessageSizeDefault;

            internal Builder()
            {
                _connectRetryFrequency = TimeSpan.FromSeconds(2);
                _heartbeatFrequency = TimeSpan.FromSeconds(10);
                _maxDocumentSizeDefault = 4 * 1024 * 1024;
                _maxMessageSizeDefault = 16000000; // 16MB (not 16 MiB!)
            }

            internal ClusterableServerSettings Build()
            {
                return new ClusterableServerSettings(
                    _connectRetryFrequency,
                    _heartbeatFrequency,
                    _maxDocumentSizeDefault,
                    _maxMessageSizeDefault);
            }

            /// <summary>
            /// Sets the the connect retry frequency.
            /// </summary>
            /// <param name="frequency">The frequency.</param>
            public void SetConnectRetryFrequency(TimeSpan frequency)
            {
                _connectRetryFrequency = frequency;
            }

            /// <summary>
            /// Sets the heartbeat frequency.
            /// </summary>
            /// <param name="frequency">The frequency.</param>
            public void SetHeartbeatFrequency(TimeSpan frequency)
            {
                _heartbeatFrequency = frequency;
            }

            /// <summary>
            /// Sets the max document size default.
            /// </summary>
            /// <param name="size">The size.</param>
            public void SetMaxDocumentSizeDefault(int size)
            {
                _maxDocumentSizeDefault = size;
            }

            /// <summary>
            /// Sets the max message size default.
            /// </summary>
            /// <param name="size">The size.</param>
            public void SetMaxMessageSizeDefault(int size)
            {
                _maxMessageSizeDefault = size;
            }
        }
    }
}