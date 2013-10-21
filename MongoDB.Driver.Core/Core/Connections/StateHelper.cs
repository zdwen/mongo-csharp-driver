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

using System.Threading;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Thread-safe helper to manage state.
    /// </summary>
    internal class StateHelper
    {
        // private fields
        private int _current;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="StateHelper" /> class.
        /// </summary>
        /// <param name="initialState">The initial state.</param>
        public StateHelper(int initialState)
        {
            _current = initialState;
        }

        // public properties
        /// <summary>
        /// Gets the current.
        /// </summary>
        public int Current
        {
            get { return Interlocked.CompareExchange(ref _current, 0, 0); }
        }

        // public methods
        /// <summary>
        /// Tries to change the state to the new state.
        /// </summary>
        /// <param name="newState">The new state.</param>
        /// <returns><c>true</c> if the state was changed; otherwise <c>false</c>.</returns>
        public bool TryChange(int newState)
        {
            return Interlocked.Exchange(ref _current, newState) != newState;
        }

        /// <summary>
        /// Tries to change the state to the new state from the old state.
        /// </summary>
        /// <param name="oldState">The old state.</param>
        /// <param name="newState">The new state.</param>
        /// <returns><c>true</c> if the state was changed; otherwise <c>false</c>.</returns>
        public bool TryChange(int oldState, int newState)
        {
            return Interlocked.CompareExchange(ref _current, newState, oldState) == oldState;
        }
    }
}