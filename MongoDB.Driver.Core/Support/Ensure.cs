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

namespace MongoDB.Driver.Core.Support
{
    internal static class Ensure
    {
        public static void IsNotNull<T>(string argumentName, T value)
            where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(argumentName, "Cannot be null.");
            }
        }

        public static void IsInfiniteOrZeroOrPositive(string argumentName, TimeSpan timeSpan)
        {
            if (timeSpan.TotalMilliseconds < -1)
            {
                throw new ArgumentException("Must be either -1 (infinite) or greater.", argumentName);
            }
        }

        public static void IsGreaterThan<T>(string argumentName, T value, T comparand) where T :IComparable<T>
        {
            if (value.CompareTo(comparand) <= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, string.Format("Must be greater than {0}", comparand));
            }
        }

        public static void IsLessThan<T>(string argumentName, T value, T comparand) where T : IComparable<T>
        {
            if (value.CompareTo(comparand) >= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, string.Format("Must be less than {0}", comparand));
            }
        }
    }
}