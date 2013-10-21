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

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// <see cref="EventArgs"/> that represent a changed value.
    /// </summary>
    /// <typeparam name="T">The type of the changed value.</typeparam>
    public class ChangedEventArgs<T> : EventArgs
    {
        // private fields
        private readonly T _newValue;
        private readonly T _oldValue;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangedEventArgs{T}" /> class.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        public ChangedEventArgs(T oldValue, T newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        // public properties
        /// <summary>
        /// Gets the new value.
        /// </summary>
        public T NewValue
        {
            get { return _newValue; }
        }

        /// <summary>
        /// Gets the old value.
        /// </summary>
        public T OldValue
        {
            get { return _oldValue; }
        }
    }
}