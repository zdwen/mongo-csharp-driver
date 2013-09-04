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
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    /// <summary>
    /// A resolver that creates a new instance on every call to Resolve.
    /// </summary>
    /// <typeparam name="T">The type to resolve.</typeparam>
    public class TransientDbDependencyResolver<T> : IDbDependencyResolver
    {
        // private fields
        private readonly Func<IDbConfigurationContainer, T> _factory;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TransientDbDependencyResolver{T}" /> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public TransientDbDependencyResolver(Func<IDbConfigurationContainer, T> factory)
        {
            Ensure.IsNotNull("factory", factory);
            _factory = factory;
        }

        /// <summary>
        /// Resolves a dependency for the specified type.
        /// </summary>
        /// <param name="type">The type to resolve.</param>
        /// <param name="container">The container.</param>
        /// <returns>The resolved dependency or <c>null</c> if one could not be resolved.</returns>
        public object Resolve(Type type, IDbConfigurationContainer container)
        {
            if (type == typeof(T))
            {
                return _factory(container);
            }

            return null;
        }
    }
}