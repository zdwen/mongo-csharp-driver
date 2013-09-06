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

using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Security
{
    [TestFixture]
    public class MongoCredentialTests
    {
        [Test]
        public void CreateMongoCRCredential_should_generate_a_valid_credential()
        {
            var credential = MongoCredential.CreateMongoCRCredential("db", "username", "password");
            Assert.IsInstanceOf<MongoInternalIdentity>(credential.Identity);
            Assert.AreEqual("MONGODB-CR", credential.Mechanism);
            Assert.AreEqual("username", credential.Username);
        }

        [Test]
        public void CreateGssapiCredential_with_a_password_should_generate_a_valid_credential()
        {
            var credential = MongoCredential.CreateGssapiCredential("username", "password");
            Assert.IsInstanceOf<MongoExternalIdentity>(credential.Identity);
            Assert.AreEqual("GSSAPI", credential.Mechanism);
            Assert.AreEqual("username", credential.Username);
        }

        [Test]
        public void CreateGssapiCredential_without_a_password_should_generate_a_valid_credential()
        {
            var credential = MongoCredential.CreateGssapiCredential("username");
            Assert.IsInstanceOf<MongoExternalIdentity>(credential.Identity);
            Assert.AreEqual("GSSAPI", credential.Mechanism);
            Assert.AreEqual("username", credential.Username);
            Assert.IsInstanceOf<ExternalEvidence>(credential.Evidence);
        }
    }
}
