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
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Security;

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    internal class StreamConnectionSettingsResolver : TypedDbDependencyResolver<StreamConnectionSettings>
    {
        protected override StreamConnectionSettings Resolve(IDbConfigurationContainer container)
        {
            var props = container.Resolve<IDbConfigurationPropertyProvider>();

            return StreamConnectionSettings.Create(x =>
            {
                IEnumerable<MongoCredential> credentials;
                if (props.TryGetValue<IEnumerable<MongoCredential>>(DbConfigurationProperties.Authentication.Credentials, out credentials))
                {
                    x.AddCredentials(credentials);
                }

                IEnumerable<IAuthenticationProtocol> protocols;
                if (props.TryGetValue<IEnumerable<IAuthenticationProtocol>>(DbConfigurationProperties.Authentication.AuthenticationProtocols, out protocols))
                {
                    x.AddProtocols(protocols);
                }
            });
        }
    }
}