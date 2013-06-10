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

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Settings for the <see cref="ReplicaSetCluster"/>.
    /// </summary>
    public sealed class ReplicaSetClusterSettings
    {
        // public static fields
        public static readonly ReplicaSetClusterSettings Defaults = new Builder().Build();

        // private fields
        private readonly string _replicaSetName;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicaSetClusterSettings" /> class.
        /// </summary>
        /// <param name="connectTimeout">The connect timeout.</param>
        /// <param name="replicaSetName">The name of the replica set.</param>
        public ReplicaSetClusterSettings(string replicaSetName)
        {
            _replicaSetName = replicaSetName;
        }

        // public properties
        /// <summary>
        /// Gets the name of the replica set.
        /// </summary>
        public string ReplicaSetName
        {
            get { return _replicaSetName; }
        }

        // public static methods
        /// <summary>
        /// Creates the specified callback.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <returns>The built settings.</returns>
        public static ReplicaSetClusterSettings Create(Action<Builder> callback)
        {
            var builder = new Builder();
            callback(builder);
            return builder.Build();
        }

        /// <summary>
        /// Used to build up <see cref="ReplicaSetClusterSettings"/>.
        /// </summary>
        public class Builder
        {
            private string _replicaSetName;

            internal Builder()
            { }

            internal ReplicaSetClusterSettings Build()
            {
                return new ReplicaSetClusterSettings(_replicaSetName);
            }

            /// <summary>
            /// Sets the name of the replica set.
            /// </summary>
            /// <param name="replicaSetName">Name of the replica set.</param>
            public void SetReplicaSetName(string replicaSetName)
            {
                _replicaSetName = replicaSetName;
            }
        }
    }
}