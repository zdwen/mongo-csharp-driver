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
    public class UpdatedEventArgs<T> : EventArgs
    {
        // private fields
        private readonly T _new;
        private readonly T _old;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatedEventArgs{T}" /> class.
        /// </summary>
        /// <param name="old">The old.</param>
        /// <param name="new">The new.</param>
        public UpdatedEventArgs(T old, T @new)
        {
            _old = old;
            _new = @new;
        }

        // public properties
        /// <summary>
        /// Gets the new.
        /// </summary>
        public T New
        {
            get { return _new; }
        }

        /// <summary>
        /// Gets the old.
        /// </summary>
        public T Old
        {
            get { return _old; }
        }
    }
}