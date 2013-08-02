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
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Protocol
{
    /// <summary>
    /// Represents an interaction with the server.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public interface IProtocol<TResult>
    {
        /// <summary>
        /// Executes the specified channel.
        /// </summary>
        /// <param name="channel">The channel.</param>
        /// <returns>The result of the execution.</returns>
        TResult Execute(IChannel channel);
    }
}