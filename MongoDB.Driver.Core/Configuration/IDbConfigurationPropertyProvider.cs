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
using System.Collections.Generic;
using MongoDB.Driver.Core.Support;
namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// Properties used during configuration.
    /// </summary>
    public interface IDbConfigurationPropertyProvider
    {
        /// <summary>
        /// Tries to get the value of the specified key.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the key existed; otherwise <c>false</c>.
        /// </returns>
        bool TryGetValue(DbConfigurationProperty property, out object value);
    }

    /// <summary>
    /// Extensions for <see cref="IDbConfigurationPropertyProvider"/>.
    /// </summary>
    public static class DbConfigurationPropertiesExtensions
    {
        /// <summary>
        /// Tries to get the value of the specified key.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="this">The this.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the key existed; otherwise <c>false</c>.
        /// </returns>
        public static bool TryGetValue<T>(this IDbConfigurationPropertyProvider @this, DbConfigurationProperty property, out T value)
        {
            Ensure.IsNotNull("@this", @this);
            Ensure.IsNotNull("property", property);

            object tempValue;
            if (@this.TryGetValue(property, out tempValue))
            {
                value = (T)tempValue;
                return true;
            }

            value = default(T);
            return false;
        }
    }
}