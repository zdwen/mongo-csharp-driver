using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    [TestFixture]
    public class CachingDbDependencyResolverTests
    {
        private class Dep
        {
            public int Id { get; set; }

            public Dep Inner { get; set; }
        }

        [Test]
        public void Should_cache_the_dependency()
        {
            var inner = new InstanceDbDependencyResolver<Dep>(new Dep { Id = 1 });
            var resolver = new CachingDbDependencyResolver(inner);

            var resolved = (Dep)resolver.Resolve(typeof(Dep), null);
            Assert.AreEqual(1, resolved.Id);

            var resolvedAgain = (Dep)resolver.Resolve(typeof(Dep), null);
            Assert.AreSame(resolved, resolvedAgain);
        }

        [Test]
        public void Should_return_null_when_the_type_does_not_match()
        {
            var inner = new InstanceDbDependencyResolver<Dep>(new Dep { Id = 1 });
            var resolver = new CachingDbDependencyResolver(inner);

            var resolved = resolver.Resolve(typeof(object), null);

            Assert.IsNull(resolved);
        }

        [Test]
        public void Should_only_cache_the_top_level_dependency()
        {
            var containerImpl = new TestContainer();
            var inner = new CompositeDbDependencyResolver(new IDbDependencyResolver[] 
            {
                new TransientWrappingDbDependencyResolver<Dep>((i, container) => new Dep { Id = 2, Inner = i }),
                new InstanceDbDependencyResolver<Dep>(new Dep { Id = 1 })
            });
            containerImpl.Resolver = new CachingDbDependencyResolver(inner);

            var result = containerImpl.Resolve<Dep>();

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Id);
            Assert.AreEqual(1, result.Inner.Id);

            var resultAgain = containerImpl.Resolve<Dep>();

            Assert.AreSame(result, resultAgain);
        }

        private class TestContainer : IDbConfigurationContainer
        {
            public IDbDependencyResolver Resolver { get; set; }

            public object Resolve(Type type)
            {
                var result = Resolver.Resolve(type, this);
                if (result == null)
                {
                    throw new MongoConfigurationException(string.Format("Unable to resolve {0}.", type));
                }
                return result;
            }
        }
    }
}