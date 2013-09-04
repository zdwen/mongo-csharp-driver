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

using System.Collections.Generic;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// An implementation of <see cref="IDbConfigurationPropertyProvider"/> that allows
    /// values to be set.
    /// </summary>
    public class DbConfigurationPropertyProvider : IDbConfigurationPropertyProvider
    {
        // private fields
        private readonly Dictionary<string, object> _properties;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DbConfigurationPropertyProvider" /> class.
        /// </summary>
        public DbConfigurationPropertyProvider()
        {
            _properties = new Dictionary<string, object>();
        }

        // public methods
        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        public void SetValue(DbConfigurationProperty property, object value)
        {
            Ensure.IsNotNull("property", property);

            property.Validate(value);
            SetValue(property.Name, value);
        }
        
        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void SetValue(string key, object value)
        {
            Ensure.IsNotNull("key", key);

            _properties[key] = value;
        }

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
            if (_properties.TryGetValue(property.Name, out value))
            {
                property.Validate(value);
                return true;
            }

            return false;
        }
    }
}