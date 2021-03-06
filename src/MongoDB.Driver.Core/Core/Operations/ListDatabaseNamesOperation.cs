﻿/* Copyright 2013-2014 MongoDB Inc.
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class ListDatabaseNamesOperation : IReadOperation<IReadOnlyList<string>>, ICommandOperation
    {
        // fields
        private MessageEncoderSettings _messageEncoderSettings;
        
        // constructors
        public ListDatabaseNamesOperation(MessageEncoderSettings messageEncoderSettings)
        {
            _messageEncoderSettings = messageEncoderSettings;
        }

        // properties
        public MessageEncoderSettings MessageEncoderSettings
        {
            get { return _messageEncoderSettings; }
            set { _messageEncoderSettings = value; }
        }

        // methods
        public BsonDocument CreateCommand()
        {
            return new BsonDocument { { "listDatabases", 1 } };
        }

        public async Task<IReadOnlyList<string>> ExecuteAsync(IReadBinding binding, TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(binding, "binding");
            var command = CreateCommand();
            var operation = new ReadCommandOperation("admin", command, _messageEncoderSettings);
            var result = await operation.ExecuteAsync(binding, timeout, cancellationToken);
            var databases = result["databases"];
            return databases.AsBsonArray.Select(x => x["name"].ToString()).ToList();
        }
    }
}
