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
using System.Collections.Concurrent;
using System.Collections.Generic;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    /// <summary>
    /// Caches the results of the resolution.
    /// </summary>
    public class CachingDbDependencyResolver : IDbDependencyResolver
    {
        // private fields
        private readonly ConcurrentDictionary<Type, object> _cache;
        private readonly IDbDependencyResolver _resolver;
        private readonly HashSet<Type> _currentlyResolving;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CachingDbDependencyResolver" /> class.
        /// </summary>
        /// <param name="resolver">The resolver.</param>
        public CachingDbDependencyResolver(IDbDependencyResolver resolver)
        {
            Ensure.IsNotNull("resolver", resolver);

            _resolver = resolver;
            _currentlyResolving = new HashSet<Type>();
            _cache = new ConcurrentDictionary<Type, object>();
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
            if (_currentlyResolving.Contains(type))
            {
                // don't cache requests for the same type.  
                // we only want to store the top level type.
                return _resolver.Resolve(type, container);
            }
            else
            {
                try
                {
                    _currentlyResolving.Add(type);
                    return _cache.GetOrAdd(type, t => _resolver.Resolve(t, container));
                }
                finally
                {
                    _currentlyResolving.Remove(type);
                }
            }
        }
    }
}