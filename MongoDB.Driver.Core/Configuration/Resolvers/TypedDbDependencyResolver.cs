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

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    /// <summary>
    /// Dependency resolver for a specified type.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    public abstract class TypedDbDependencyResolver<T> : IDbDependencyResolver
    {
        // public methods
        /// <summary>
        /// Resolves a dependency for the specified type.
        /// </summary>
        /// <param name="type">The type to resolve.</param>
        /// <param name="container">The container.</param>
        /// <returns>
        /// The resolved dependency or <c>null</c> if one could not be resolved.
        /// </returns>
        public object Resolve(Type type, IDbConfigurationContainer container)
        {
            if (typeof(T) != type)
            {
                return null;
            }

            return Resolve(container);
        }

        // protected methods
        /// <summary>
        /// Resolves a dependency for the specified type.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>
        /// The resolved dependency or <c>null</c> if one could not be resolved.
        /// </returns>
        protected abstract T Resolve(IDbConfigurationContainer container);
    }
}
