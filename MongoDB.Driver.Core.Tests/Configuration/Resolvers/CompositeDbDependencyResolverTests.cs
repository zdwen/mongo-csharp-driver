using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    [TestFixture]
    public class CompositeDbDependencyResolverTests
    {
        private class Dep
        {
            public int Id { get; set; }
        }

        [Test]
        public void Should_resolve_the_dependency_from_the_first_matching_resolver()
        {
            var resolvers = new List<IDbDependencyResolver>
            {
                new InstanceDbDependencyResolver<object>(new object()),
                new InstanceDbDependencyResolver<Dep>(new Dep { Id = 1 }),
                new InstanceDbDependencyResolver<Dep>(new Dep { Id = 2 })
            };

            var resolver = new CompositeDbDependencyResolver(resolvers);

            var resolved = (Dep)resolver.Resolve(typeof(Dep), null);

            Assert.AreEqual(1, resolved.Id);
        }

        [Test]
        public void Should_return_null_when_the_type_does_not_match()
        {
            var resolvers = new List<IDbDependencyResolver>
            {
                new InstanceDbDependencyResolver<Dep>(new Dep { Id = 1 }),
                new InstanceDbDependencyResolver<Dep>(new Dep { Id = 2 })
            };

            var resolver = new CompositeDbDependencyResolver(resolvers);

            var resolved = resolver.Resolve(typeof(object), null);

            Assert.IsNull(resolved);
        }
    }
}