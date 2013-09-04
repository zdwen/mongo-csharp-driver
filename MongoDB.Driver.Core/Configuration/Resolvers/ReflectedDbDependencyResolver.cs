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

using System.Linq;

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    /// <summary>
    /// An <see cref="IDbDependencyResolver"/> that automatically resolves
    /// the dependencies of the most-parameter constructor.
    /// </summary>
    /// <typeparam name="TAs">The type to resolve.</typeparam>
    /// <typeparam name="TImpl">The type of the implementation.</typeparam>
    public class ReflectedDbDependencyResolver<TAs,TImpl> : TypedDbDependencyResolver<TAs>
        where TImpl : TAs
    {
        /// <summary>
        /// Resolves a dependency for the specified type.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>
        /// The resolved dependency or <c>null</c> if one could not be resolved.
        /// </returns>
        protected override TAs Resolve(IDbConfigurationContainer container)
        {
            var ctor = typeof(TImpl)
                .GetConstructors()
                .OrderByDescending(x => x.GetParameters().Length)
                .First();

            var parameters = ctor.GetParameters();
            var args = new object[parameters.Length];

            for(int i = 0; i < parameters.Length; i++)
            {
                args[i] = container.Resolve(parameters[i].ParameterType);
            }

            return (TAs)ctor.Invoke(args);
        }
    }
}