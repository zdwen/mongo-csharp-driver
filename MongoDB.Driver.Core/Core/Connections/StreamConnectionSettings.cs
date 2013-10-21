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
using MongoDB.Driver.Core.Security;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Authentication settings.
    /// </summary>
    public class StreamConnectionSettings
    {
        // public static readonly fields
        /// <summary>
        /// The default settings.
        /// </summary>
        public static readonly StreamConnectionSettings Defaults = new Builder().Build();

        // private fields
        private readonly IEnumerable<MongoCredential> _credentials;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamConnectionSettings" /> class.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        public StreamConnectionSettings(IEnumerable<MongoCredential> credentials)
        {
            Ensure.IsNotNull("credentials", credentials);

            _credentials = credentials.ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the credentials.
        /// </summary>
        public IEnumerable<MongoCredential> Credentials
        {
            get { return _credentials; }
        }

        // public static methods
        /// <summary>
        /// A method used to build up settings.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <returns>The built settings.</returns>
        public static StreamConnectionSettings Create(Action<Builder> callback)
        {
            var builder = new Builder();
            callback(builder);
            return builder.Build();
        }

        // public methods
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{{ Credentials: [{0}] }}",
                string.Join(", ", _credentials.Select(x => string.Format("'{0}'", x))));
        }

        // nested classes
        /// <summary>
        /// Builds up <see cref="StreamConnectionSettings"/>.
        /// </summary>
        public class Builder
        {
            private List<MongoCredential> _credentials;

            internal Builder()
            {
                _credentials = new List<MongoCredential>();
            }

            internal StreamConnectionSettings Build()
            {
                return new StreamConnectionSettings(_credentials);
            }

            /// <summary>
            /// Adds the credential.
            /// </summary>
            /// <param name="credential">The credential.</param>
            public void AddCredential(MongoCredential credential)
            {
                _credentials.Add(credential);
            }

            /// <summary>
            /// Adds the credentials.
            /// </summary>
            /// <param name="credentials">The credentials.</param>
            public void AddCredentials(IEnumerable<MongoCredential> credentials)
            {
                _credentials.AddRange(credentials);
            }

            /// <summary>
            /// Clears the credentials.
            /// </summary>
            public void ClearCredentials()
            {
                _credentials.Clear();
            }
        }
    }
}