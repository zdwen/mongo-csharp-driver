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


namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// A pair of implementations of <see cref="IDbConfigurationPropertyProvider"/> that will resolve
    /// from the first and only call into the second if the first failed to retrieve a value.
    /// </summary>
    public class DbConfigurationPropertiesPair : IDbConfigurationPropertyProvider
    {
        // private fields
        private readonly IDbConfigurationPropertyProvider _first;
        private readonly IDbConfigurationPropertyProvider _second;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DbConfigurationPropertiesPair" /> class.
        /// </summary>
        /// <param name="first">The first.</param>
        /// <param name="second">The second.</param>
        public DbConfigurationPropertiesPair(IDbConfigurationPropertyProvider first, IDbConfigurationPropertyProvider second)
        {
            _first = first;
            _second = second;
        }

        // public methods
        /// <summary>
        /// Tries to get the value of the specified key.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the key existed; otherwise <c>false</c>.
        /// </returns>
        public bool TryGetValue(DbConfigurationProperty property, out object value)
        {
            return _first.TryGetValue(property, out value) || _second.TryGetValue(property, out value);
        }
    }
}