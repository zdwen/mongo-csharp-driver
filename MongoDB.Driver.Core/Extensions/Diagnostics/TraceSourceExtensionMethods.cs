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
namespace MongoDB.Driver.Core.Extensions.Diagnostics
{
    /// <summary>
    /// Extension methods for <see cref="TraceSource"/>.
    /// </summary>
    internal static class TraceSourceExtensionMethods
    {
        // public static methods
        /// <summary>
        /// Traces an error message.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="message">The message.</param>
        public static void TraceError(this TraceSource traceSource, string message)
        {
            Trace(traceSource, TraceEventType.Error, message);
        }

        /// <summary>
        /// Traces an error message.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public static void TraceError(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Error, format, args);
        }

        /// <summary>
        /// Traces an error message.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="message">The message.</param>
        public static void TraceError(this TraceSource traceSource, Exception ex, string message)
        {
            Trace(traceSource, TraceEventType.Error, ex, message);
        }

        /// <summary>
        /// Traces an error message.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public static void TraceError(this TraceSource traceSource, Exception ex, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Error, ex, format, args);
        }

        /// <summary>
        /// Traces a verbose message.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="message">The message.</param>
        public static void TraceVerbose(this TraceSource traceSource, string message)
        {
            Trace(traceSource, TraceEventType.Verbose, message);
        }

        /// <summary>
        /// Traces a verbose message.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public static void TraceVerbose(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Verbose, format, args);
        }

        /// <summary>
        /// Traces a warning message.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="message">The message.</param>
        public static void TraceWarning(this TraceSource traceSource, string message)
        {
            Trace(traceSource, TraceEventType.Warning, message);
        }

        /// <summary>
        /// Traces a warning message.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public static void TraceWarning(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Warning, format, args);
        }

        /// <summary>
        /// Traces an error message.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="message">The message.</param>
        public static void TraceWarning(this TraceSource traceSource, Exception ex, string message)
        {
            Trace(traceSource, TraceEventType.Warning, ex, message);
        }

        /// <summary>
        /// Traces an error message.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public static void TraceWarning(this TraceSource traceSource, Exception ex, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Warning, ex, format, args);
        }

        // private static methods
        private static void Trace(TraceSource traceSource, TraceEventType eventType, string message)
        {
            traceSource.TraceEvent(eventType, 0, message);
        }

        private static void Trace(TraceSource traceSource, TraceEventType eventType, string format, params object[] args)
        {
            traceSource.TraceEvent(eventType, 0, format, args);
        }

        private static void Trace(TraceSource traceSource, TraceEventType eventType, Exception ex, string message)
        {
            Trace(traceSource, eventType, message + ": " + ex.Message);
        }

        private static void Trace(TraceSource traceSource, TraceEventType eventType, Exception ex, string format, params object[] args)
        {
            Trace(traceSource, eventType, format + ": " + ex.Message, args);
        }
    }
}