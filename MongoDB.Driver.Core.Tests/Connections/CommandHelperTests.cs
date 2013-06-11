using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class CommandHelperTests
    {
        [Test]
        public void GetCode_should_return_code_if_one_exists()
        {
            var doc = new BsonDocument { { "code", 10 } };

            var value = CommandHelper.GetCode(doc);

            Assert.AreEqual(10, value);
        }

        [Test]
        public void GetCode_should_return_null_if_one_does_not_exist()
        {
            var doc = new BsonDocument();
            var value = CommandHelper.GetCode(doc);
            Assert.AreEqual(null, value);
        }

        [Test]
        public void GetErrorMessage_should_return_null_if_the_result_is_ok()
        {
            var doc = new BsonDocument { { "ok", true } };

            var result = CommandHelper.GetErrorMessage(doc);

            Assert.IsNull(result);
        }

        [Test]
        public void GetErrorMessage_should_return_the_error_message_if_one_exists()
        {
            var doc = new BsonDocument { { "ok", false}, { "errmsg", "fluffy" } };

            var result = CommandHelper.GetErrorMessage(doc);

            Assert.AreEqual("fluffy", result);
        }

        [Test]
        public void GetErrorMessage_should_return_unknown_error_if_one_does_not_exist()
        {
            var doc = new BsonDocument { { "ok", false } };

            var result = CommandHelper.GetErrorMessage(doc);

            Assert.AreEqual("Unknown error", result);
        }

        [Test]
        public void IsResultOk_should_return_true_if_ok_exists_and_is_true()
        {
            var doc = new BsonDocument { { "ok", true } };

            var result = CommandHelper.IsResultOk(doc);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsResultOk_should_return_false_if_ok_exists_and_is_false()
        {
            var doc = new BsonDocument { { "ok", false } };

            var result = CommandHelper.IsResultOk(doc);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsResultOk_should_return_false_if_ok_does_not_exist()
        {
            var doc = new BsonDocument();

            var result = CommandHelper.IsResultOk(doc);

            Assert.IsFalse(result);
        }
    }
}