﻿/* Copyright 2010-2014 MongoDB Inc.
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

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Operations;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class MongoClientTests
    {
        [Test]
        public void UsesSameMongoServerForIdenticalSettings()
        {
            var client1 = new MongoClient("mongodb://localhost");
            var server1 = client1.GetServer();

            var client2 = new MongoClient("mongodb://localhost");
            var server2 = client2.GetServer();

            Assert.AreSame(server1, server2);
        }

        [Test]
        public void UsesSameMongoServerWhenReadPreferenceTagsAreTheSame()
        {
            var client1 = new MongoClient("mongodb://localhost/?readPreferenceTags=dc:ny");
            var server1 = client1.GetServer();

            var client2 = new MongoClient("mongodb://localhost/?readPreferenceTags=dc:ny");
            var server2 = client2.GetServer();

            Assert.AreSame(server1, server2);
        }

        [Test]
        public async Task ListDatabaseNames()
        {
            var operationExecutor = new MockOperationExecutor();
            var client = new MongoClient(operationExecutor);
            var names = await client.GetDatabaseNamesAsync();

            var call = operationExecutor.GetReadCall<IReadOnlyList<string>>();

            call.Operation.Should().BeOfType<ListDatabaseNamesOperation>();
        }
    }
}
