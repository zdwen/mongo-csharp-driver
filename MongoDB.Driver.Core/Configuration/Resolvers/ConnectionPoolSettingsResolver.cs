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
    internal class ConnectionPoolSettingsResolver : TypedDbDependencyResolver<ConnectionPoolSettings>
    {
        protected override ConnectionPoolSettings Resolve(IDbConfigurationContainer container)
        {
            var props = container.Resolve<IDbConfigurationPropertyProvider>();

            return ConnectionPoolSettings.Create(x =>
            {
                TimeSpan connectionMaxIdleTime;
                if (props.TryGetValue(DbConfigurationProperties.Connection.MaxIdleTime, out connectionMaxIdleTime))
                {
                    x.SetConnectionMaxIdleTime(connectionMaxIdleTime);
                }

                TimeSpan connectionMaxLifeTime;
                if (props.TryGetValue(DbConfigurationProperties.Connection.MaxLifeTime, out connectionMaxLifeTime))
                {
                    x.SetConnectionMaxLifeTime(connectionMaxLifeTime);
                }

                int maxSize;
                if (props.TryGetValue(DbConfigurationProperties.Pool.MaxSize, out maxSize))
                {
                    x.SetMaxSize(maxSize);
                }
                else
                {
                    maxSize = ConnectionPoolSettings.Defaults.MaxSize;
                }

                int maxWaitQueueSizeMultiple;
                if (props.TryGetValue(DbConfigurationProperties.Pool.MaxWaitQueueSizeMultiple, out maxWaitQueueSizeMultiple))
                {
                    x.SetMaxWaitQueueSize(maxWaitQueueSizeMultiple * maxSize);
                }

                int minSize;
                if (props.TryGetValue(DbConfigurationProperties.Pool.MinSize, out minSize))
                {
                    x.SetMinSize(minSize);
                }

                TimeSpan sizeMaintenanceFrequency;
                if (props.TryGetValue(DbConfigurationProperties.Pool.SizeMaintenanceFrequency, out sizeMaintenanceFrequency))
                {
                    x.SetSizeMaintenanceFrequency(sizeMaintenanceFrequency);
                }
            });
        }
    }
}