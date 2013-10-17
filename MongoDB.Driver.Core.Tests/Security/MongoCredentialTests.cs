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
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Security
{
    [TestFixture]
    public class MongoCredentialTests
    {
        [Test]
        [TestCase("MONGODB-CR", "source", "username", "password", typeof(MongoInternalIdentity), typeof(PasswordEvidence))]
        [TestCase("MONGODB-X509", "$external", "username", null, typeof(MongoExternalIdentity), typeof(ExternalEvidence))]
        [TestCase("GSSAPI", null, "username", "password", typeof(MongoExternalIdentity), typeof(PasswordEvidence))]
        [TestCase("GSSAPI", null, "username", null, typeof(MongoExternalIdentity), typeof(ExternalEvidence))]
        [TestCase("PLAIN", "source", "username", "password", typeof(MongoInternalIdentity), typeof(PasswordEvidence))]
        [TestCase("PLAIN", "$external", "username", "password", typeof(MongoExternalIdentity), typeof(PasswordEvidence))]
        public void FromComponents_should_generate_a_valid_credential_when_the_input_is_valid(string mechanismName, string source, string username, string password, Type identityType, Type evidenceType)
        {
            var credential = MongoCredential.FromComponents(mechanismName, source, username, password);
            
            Assert.IsInstanceOf(identityType, credential.Identity);
            Assert.AreEqual(mechanismName, credential.Mechanism.Name);
            Assert.AreEqual(username, credential.Identity.Username);
            if (identityType == typeof(MongoInternalIdentity))
            {
                Assert.AreEqual(source, credential.Identity.Source);
            }
            else
            {
                Assert.AreEqual("$external", credential.Identity.Source);
            }
            Assert.IsInstanceOf(evidenceType, credential.Evidence);
            if(evidenceType == typeof(PasswordEvidence))
            {
                Assert.AreEqual(password, ((PasswordEvidence)credential.Evidence).PlainTextPassword);
            }
        }
    }
}
