using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Configuration
{
    public class DbConfigurationPropertyTests
    {
        [Test]
        public void Constructor_should_assign_values()
        {
            var prop = new DbConfigurationProperty("foo", typeof(int));

            Assert.AreEqual("foo", prop.Name);
            Assert.AreEqual(typeof(int), prop.Type);
        }

        [Test]
        public void Validate_should_throw_when_type_is_not_assignable()
        {
            var prop = new DbConfigurationProperty("foo", typeof(int));

            Assert.Throws<MongoConfigurationException>(() => prop.Validate("string"));
        }

        [Test]
        public void Validate_should_not_throw_when_types_are_the_same()
        {
            var prop = new DbConfigurationProperty("foo", typeof(int));

            prop.Validate(12);
        }

        [Test]
        public void Validate_should_not_throw_when_types_are_the_assignable()
        {
            var prop = new DbConfigurationProperty("foo", typeof(IEnumerable<string>));

            prop.Validate(new[] { "bar", "baz" });
            prop.Validate(new List<string> { "bar", "baz" });
        }
    }
}