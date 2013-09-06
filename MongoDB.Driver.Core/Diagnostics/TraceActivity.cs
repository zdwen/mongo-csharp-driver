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
using System.Security;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Diagnostics
{
    internal sealed class TraceActivity : IDisposable
    {
        private readonly string _format;
        private readonly object[] _args;
        private readonly Guid _oldActivityId;
        private readonly TraceSource _traceSource;
        private bool _disposed;

        [SecuritySafeCritical]
        public TraceActivity(TraceSource traceSource, string format, params object[] args)
        {
            Ensure.IsNotNull("traceSource", traceSource);
            Ensure.IsNotNull("format", format);

            _traceSource = traceSource;
            _format = format;
            _args = args;

            _oldActivityId = Trace.CorrelationManager.ActivityId;
            var activityId = Guid.NewGuid();
            if (_oldActivityId != Guid.Empty)
            {
                traceSource.TraceTransfer(0, "transfer", activityId);
            }

            Trace.CorrelationManager.StartLogicalOperation(activityId);
            Trace.CorrelationManager.ActivityId = activityId;
            _traceSource.TraceEvent(TraceEventType.Start, 0, _format, _args);
        }

        [SecuritySafeCritical]
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_oldActivityId != Guid.Empty)
                {
                    _traceSource.TraceTransfer(0, "transfer", _oldActivityId);
                }
                _traceSource.TraceEvent(TraceEventType.Stop, 0, _format, _args);
                Trace.CorrelationManager.ActivityId = _oldActivityId;
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }
    }
}