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

namespace MongoDB.Driver.Core
{
    /// <summary>
    /// Represents read preference modes.
    /// </summary>
    public enum ReadPreferenceMode
    {
        /// <summary>
        /// Use primary only.
        /// </summary>
        Primary,
        /// <summary>
        /// Use primary if possible, otherwise a secondary.
        /// </summary>
        PrimaryPreferred,
        /// <summary>
        /// Use secondary only.
        /// </summary>
        Secondary,
        /// <summary>
        /// Use a secondary if possible, otherwise primary.
        /// </summary>
        SecondaryPreferred,
        /// <summary>
        /// Use any near by server, primary or secondary.
        /// </summary>
        Nearest
    }
}