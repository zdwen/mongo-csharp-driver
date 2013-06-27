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


namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// The type reported by the remote server.
    /// </summary>
    public enum ServerType
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown,
        /// <summary>
        /// The remote server is a stand alone instance.
        /// </summary>
        StandAlone,
        /// <summary>
        /// The remote server is a replica set primary.
        /// </summary>
        ReplicaSetPrimary,
        /// <summary>
        /// The remote server is a replica set secondary.
        /// </summary>
        ReplicaSetSecondary,
        /// <summary>
        /// The remote server is a replica set arbiter.
        /// </summary>
        ReplicaSetArbiter,
        /// <summary>
        /// The remote server is a member of a replica set other than primary, secondary, or arbiter.
        /// </summary>
        ReplicaSetOther,
        /// <summary>
        /// The remove server is a shard router (mongos).
        /// </summary>
        ShardRouter
    }

    /// <summary>
    /// Extension methods for <see cref="ServerType"/>.
    /// </summary>
    public static class ServerTypeExtensions
    {
        /// <summary>
        /// Indicates whether the server can accept writes.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the server can accept writes; otherwise, <c>false</c>.
        /// </returns>
        public static bool CanWrite(this ServerType type)
        {
            return type == ServerType.ReplicaSetPrimary ||
                type == ServerType.ShardRouter ||
                type == ServerType.StandAlone;
        }

        /// <summary>
        /// Indicates whether or not the server is a member of a replica set.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if the server is a member of a replica set; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsReplicaSetMember(this ServerType type)
        {
            return type == ServerType.ReplicaSetPrimary ||
                type == ServerType.ReplicaSetSecondary ||
                type == ServerType.ReplicaSetArbiter ||
                type == ServerType.ReplicaSetOther;
        }
    }
}