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

using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Protocol;
using MongoDB.Driver.Core.Support;

namespace MongoDB.Driver.Core.Connections
{
    internal static class CommandHelper
    {
        public static int? GetCode(BsonDocument result)
        {
            Ensure.IsNotNull("result", result);

            return result.GetValue("code", BsonNull.Value).AsNullableInt32;
        }

        public static string GetErrorMessage(BsonDocument result)
        {
            Ensure.IsNotNull("result", result);

            if (!IsResultOk(result))
            {
                BsonValue errmsg;
                if (result.TryGetValue("errmsg", out errmsg) && !errmsg.IsBsonNull)
                {
                    return errmsg.ToString();
                }
                else
                {
                    return "Unknown error";
                }
            }

            return null;
        }

        public static bool IsResultOk(BsonDocument result)
        {
            Ensure.IsNotNull("result", result);

            BsonValue ok;
            if (result.TryGetValue("ok", out ok))
            {
                return ok.ToBoolean();
            }

            return false;
        }

        public static BsonDocument RunCommand(string databaseName, BsonDocument command, IConnection connection)
        {
            Ensure.IsNotNull("databaseName", databaseName);
            Ensure.IsNotNull("command", command);
            Ensure.IsNotNull("connection", connection);

            var queryMessage = new QueryMessage(
                new MongoNamespace(databaseName, MongoNamespace.CommandCollectionName),
                command,
                QueryFlags.SlaveOk,
                0,
                1,
                null,
                new BsonBinaryWriterSettings());

            using(var packet = new BufferedRequestNetworkPacket())
            {
                packet.AddMessage(queryMessage);
                connection.Send(packet);
            }

            using (var replyMessage = connection.Receive())
            {
                if (replyMessage.NumberReturned == 0)
                {
                    throw new MongoOperationException(string.Format("Command '{0}' failed. No response returned.", command.GetElement(0).Name));
                }

                var serializer = BsonDocumentSerializer.Instance;
                return replyMessage.DeserializeDocuments<BsonDocument>(serializer, null, new BsonBinaryReaderSettings()).Single();
            }
        }
    }
}