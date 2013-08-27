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

namespace MongoDB.Bson.Serialization.Serializers
{
    public class ReadOnlyCollectionSubclassSerializer<TValue, TItem> : IEnumerableSerializerBase<TValue, TItem> where TValue : ReadOnlyCollection<TItem>
    {
        // protected methods
        protected override object CreateAccumulator()
        {
            return new List<TItem>();
        }

        protected override TValue FinalizeResult(object accumulator)
        {
            // the subclass must have a constructor that takes an IList<T> to wrap
            return (TValue)Activator.CreateInstance(typeof(TValue), accumulator);
        }
    }
}
