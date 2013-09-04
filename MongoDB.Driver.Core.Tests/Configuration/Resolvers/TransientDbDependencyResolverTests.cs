using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    [TestFixture]
    public class TransientDbDependencyResolverTests
    {
        [Test]
        public void Should_resolve_the_dependency()
        {
            var resolver = new TransientDbDependencyResolver<object>(container => new object());

            var result = resolver.Resolve(typeof(object), null);

            Assert.IsNotNull(result);
        }

        [Test]
        public void Should_return_null_when_the_type_does_not_match()
        {
            var resolver = new TransientDbDependencyResolver<object>(container => new object());

            var result = resolver.Resolve(typeof(int), null);

            Assert.IsNull(result);
        }
    }
}
