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

using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core
{
    /// <summary>
    /// Represents a database name and a collection name.
    /// </summary>
    public class MongoNamespace
    {
        // private static fields
        private static readonly string __template = "{0}.{1}";

        // public static fields
        public static readonly string CommandCollectionName = "$cmd";

        // private fields
        private readonly string _databaseName;
        private readonly string _collectionName;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoNamespace" /> class.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="collectionName">Name of the collection.</param>
        public MongoNamespace(string databaseName, string collectionName)
        {
            Ensure.IsNotNull("databaseName", databaseName);
            Ensure.IsNotNull("collectionName", collectionName);

            _databaseName = databaseName;
            _collectionName = collectionName;
        }

        // public properties
        /// <summary>
        /// Gets the name of the collection.
        /// </summary>
        public string CollectionName
        {
            get { return _collectionName; }
        }

        /// <summary>
        /// Gets the command collection.
        /// </summary>
        public MongoNamespace CommandCollection
        {
            get { return new MongoNamespace(_databaseName, CommandCollectionName); }
        }

        /// <summary>
        /// Gets the name of the database.
        /// </summary>
        public string DatabaseName
        {
            get { return _databaseName; }
        }

        /// <summary>
        /// Gets the full name.
        /// </summary>
        public string FullName
        {
            get { return string.Format(__template, _databaseName, _collectionName); }
        }

        // public methods
        /// <summary>
        /// Gets a sibling collection.
        /// </summary>
        /// <param name="collectionName">Name of the collection.</param>
        /// <returns>The namespace for the sibling.</returns>
        public MongoNamespace GetSiblingCollection(string collectionName)
        {
            return new MongoNamespace(_databaseName, collectionName);
        }
    }
}