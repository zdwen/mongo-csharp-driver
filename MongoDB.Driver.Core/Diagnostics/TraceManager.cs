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

namespace MongoDB.Driver.Core.Diagnostics
{
    /// <summary>
    /// Manages trace sources.
    /// </summary>
    public class TraceManager : IDisposable
    {
        // public static fields
        /// <summary>
        /// The source switch name.
        /// </summary>
        public static readonly string SourceSwitchName = "MongoDBSwitch";
        /// <summary>
        /// The prefix of the trace source name.
        /// </summary>
        public static readonly string TraceSourceNamePrefix = "MongoDB.";

        // private fields
        private readonly ConcurrentDictionary<string, TraceSource> _sources;
        private readonly SourceSwitch _sourceSwitch;
        private volatile bool _disposed;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TraceManager" /> class.
        /// </summary>
        public TraceManager()
        {
            _sourceSwitch = new SourceSwitch(SourceSwitchName);
            _sources = new ConcurrentDictionary<string, TraceSource>();
        }

        // public properties
        /// <summary>
        /// Gets the source switch.
        /// </summary>
        public SourceSwitch SourceSwitch
        {
            get { return _sourceSwitch; }
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
        /// Gets the trace source for the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>A trace source.</returns>
        public TraceSource GetTraceSource<T>()
        {
            ThrowIfDisposed();
            var name = GetNameForTraceSource(typeof(T));
            return _sources.GetOrAdd(name, key => new TraceSource(key, SourceLevels.Off)
            {
                Switch = _sourceSwitch
            });
        }

        // protected methods
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                foreach (var traceSource in _sources)
                {
                    traceSource.Value.Flush();
                    traceSource.Value.Close();
                }

                _sources.Clear();
            }

            _disposed = true;
        }

        /// <summary>
        /// Gets the name for trace source.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The name for the trace source of the specified type.</returns>
        protected virtual string GetNameForTraceSource(Type type)
        {
            return TraceSourceNamePrefix + type.Name;
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