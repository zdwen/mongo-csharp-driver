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
using System.Collections.ObjectModel;
using System.Linq;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    /// <summary>
    /// An <see cref="IDbDependencyResolver"/> that delegates to a number of other resolvers in order.
    /// </summary>
    public class CompositeDbDependencyResolver : IDbDependencyResolver
    {
        // private fields
        private readonly ReadOnlyCollection<IDbDependencyResolver> _resolvers;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeDbDependencyResolver" /> class.
        /// </summary>
        /// <param name="resolvers">The resolvers.</param>
        public CompositeDbDependencyResolver(IEnumerable<IDbDependencyResolver> resolvers)
        {
            Ensure.IsNotNull("resolvers", resolvers);

            _resolvers = resolvers.ToList().AsReadOnly();
        }

        // public methods
        /// <summary>
        /// Resolves a dependency for the specified type.
        /// </summary>
        /// <param name="type">The type to resolve.</param>
        /// <param name="container">The container.</param>
        /// <returns>The resolved dependency or <c>null</c> if one could not be resolved.</returns>
        public object Resolve(Type type, IDbConfigurationContainer container)
        {
            foreach (var resolver in _resolvers)
            {
                var resolved = resolver.Resolve(type, container);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            return null;
        }
    }
}
