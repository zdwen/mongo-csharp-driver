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

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// A configuration property.
    /// </summary>
    public sealed class DbConfigurationProperty
    {
        // private fields
        private readonly string _name;
        private readonly Type _type;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DbConfigurationProperty" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        public DbConfigurationProperty(string name, Type type)
        {
            _name = name;
            _type = type;
        }

        // public properties
        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets the type.
        /// </summary>
        public Type Type
        {
            get { return _type; }
        }

        // public methods
        /// <summary>
        /// Validates the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="MongoConfigurationException"></exception>
        public void Validate(object value)
        {
            if (!_type.IsInstanceOfType(value))
            {
                var message = string.Format("The value for '{0}' must be of type '{1}'.", _name, _type);
                throw new MongoConfigurationException(message);
            }
        }
    }
}