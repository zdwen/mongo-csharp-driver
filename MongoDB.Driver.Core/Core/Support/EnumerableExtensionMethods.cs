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
using System.Linq;
using System.Security.Cryptography;

namespace MongoDB.Driver.Core.Support
{
    /// <summary>
    /// Extension methods for <see cref="IEnumerable{T}"/>.
    /// </summary>
    internal static class EnumerableExtensionMethods
    {
        private static RNGCryptoServiceProvider __globalRandom = new RNGCryptoServiceProvider();
        [ThreadStatic]
        private static Random __threadRandom;

        /// <summary>
        /// Selects a single item from the items enumerable at random, or the 
        /// default value if the items enumerable contains no values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The items.</param>
        /// <returns></returns>
        public static T RandomOrDefault<T>(this IEnumerable<T> items)
        {
            var collection = items as ICollection<T>;
            if (collection == null)
            {
                // materialize to avoid cost of iterating multiple times
                collection = items.ToList();
            }

            if (collection.Count == 0)
            {
                return default(T);
            }
            else if (collection.Count == 1)
            {
                return collection.First();
            }

            var random = __threadRandom;
            if (random == null)
            {
                byte[] buffer = new byte[4];
                __globalRandom.GetBytes(buffer);
                __threadRandom = random = new Random(BitConverter.ToInt32(buffer, 0));
            }

            var index = random.Next(0, collection.Count);
            return collection.ElementAt(index);
        }
    }
}