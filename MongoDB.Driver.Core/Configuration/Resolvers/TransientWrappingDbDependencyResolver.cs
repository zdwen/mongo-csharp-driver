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
    /// Wraps a resolved dependency in another implementation.
    /// </summary>
    /// <typeparam name="T">The type to wrap.</typeparam>
    public class TransientWrappingDbDependencyResolver<T> : IDbDependencyResolver
    {
        // private fields
        private readonly Func<T, IDbConfigurationContainer, T> _factory;
        private bool isBeingResolved;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TransientWrappingDbDependencyResolver{T}" /> class.
        /// </summary>
        /// <param name="factory">The wrapper.</param>
        public TransientWrappingDbDependencyResolver(Func<T, IDbConfigurationContainer, T> factory)
        {
            Ensure.IsNotNull("factory", factory);
            _factory = factory;
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
            if (type == typeof(T) && !isBeingResolved)
            {
                // if we are already resolving this type ourself, we need
                // to skip it when we get called again for it so as 
                // to get back the previously registered version.
                isBeingResolved = true;
                try
                {
                    var inner = container.Resolve<T>();
                    isBeingResolved = false;
                    return _factory(inner, container);
                }
                catch (Exception ex)
                {
                    isBeingResolved = false;
                    var message = string.Format("Unable to resolve inner {0}.", typeof(T));
                    throw new MongoConfigurationException(message, ex);
                }
            }

            return null;
        }
    }
}