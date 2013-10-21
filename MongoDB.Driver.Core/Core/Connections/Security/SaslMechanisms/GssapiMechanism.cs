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
using System.Linq;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Security;

namespace MongoDB.Driver.Core.Connections.Security.SaslMechanisms
{
    /// <summary>
    /// A mechanism implementing the GSS API specification.
    /// </summary>
    internal class GssapiMechanism : ISaslMechanism
    {
        // public static fields
        public static readonly string ServiceNameMechanismProperty = "SERVICE_NAME";

        // private static fields
        private static readonly bool __useGsasl = !Environment.OSVersion.Platform.ToString().Contains("Win");

        // public properties
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        public string Name
        {
            get { return "GSSAPI"; }
        }

        // public methods
        /// <summary>
        /// Determines whether this instance can authenticate with the specified credential.
        /// </summary>
        /// <param name="credential">The credential.</param>
        /// <returns>
        ///   <c>true</c> if this instance can authenticate with the specified credential; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool CanUse(MongoCredential credential)
        {
            if (!credential.Mechanism.Name.Equals(Name, StringComparison.InvariantCultureIgnoreCase) || !(credential.Identity is MongoExternalIdentity))
            {
                return false;
            }
            if (__useGsasl)
            {
                // GSASL relies on kinit to work properly and hence, the evidence is external.
                return credential.Evidence is ExternalEvidence;
            }
            return true;
        }

        /// <summary>
        /// Initializes the mechanism.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="credential">The credential.</param>
        /// <returns>The initial step.</returns>
        public ISaslStep Initialize(IConnection connection, MongoCredential credential)
        {
            var serviceNamePair = credential.Mechanism.Properties.SingleOrDefault(p => p.Key == ServiceNameMechanismProperty);
            string serviceName = "mongodb";
            if (serviceNamePair.Key == ServiceNameMechanismProperty)
            {
                serviceName = (string)serviceNamePair.Value;
            }

            if (__useGsasl)
            {
                return new GsaslGssapiImplementation(
                    serviceName,
                    connection.DnsEndPoint.Host,
                    credential.Identity.Username,
                    credential.Evidence);
            }

            return new WindowsGssapiImplementation(
                serviceName,
                connection.DnsEndPoint.Host,
                credential.Identity.Username,
                credential.Evidence);
        }
    }
}