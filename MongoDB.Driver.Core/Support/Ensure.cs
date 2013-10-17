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
using System.Threading;

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

        public static void IsNotNullOrEmpty(string argumentName, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Cannot be null or empty.", argumentName);
            }
        }

        public static void IsInfiniteOrGreaterThanOrEqualToZero(string argumentName, TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero && timeSpan != Timeout.InfiniteTimeSpan)
            {
                throw new ArgumentException("Must be either infinite (-1 millisecond) or greater than or equal to 0.", argumentName);
            }
        }

        public static void IsEqualTo<T>(string argumentName, T value, T comparand)
        {
            if (!value.Equals(comparand))
            {
                throw new ArgumentException(string.Format("Must be equal to {0}.", comparand), argumentName);
            }
        }

        public static void IsGreaterThan<T>(string argumentName, T value, T comparand) where T :IComparable<T>
        {
            if (value.CompareTo(comparand) <= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, string.Format("Must be greater than {0}.", comparand));
            }
        }

        public static void IsGreaterThanOrEqualTo<T>(string argumentName, T value, T comparand) where T : IComparable<T>
        {
            if (value.CompareTo(comparand) < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, string.Format("Must be greater than or equal to {0}.", comparand));
            }
        }

        public static void IsLessThan<T>(string argumentName, T value, T comparand) where T : IComparable<T>
        {
            if (value.CompareTo(comparand) >= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, string.Format("Must be less than {0}.", comparand));
            }
        }
    }
}