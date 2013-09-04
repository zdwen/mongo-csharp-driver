using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    [TestFixture]
    public class TransientWrappingDbDependencyResolverTests
    {
        [Test]
        public void Should_resolve_the_dependency_one_layer_deep()
        {
            var containerImpl = new TestContainer();
            containerImpl.Resolvers = new List<IDbDependencyResolver>
            {
                new TransientWrappingDbDependencyResolver<Dep>((inner, container) => new Dep { Id = 2, Inner = inner }),
                new InstanceDbDependencyResolver<Dep>(new Dep { Id = 1 })
            };

            var result = containerImpl.Resolve<Dep>();

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Id);
            Assert.AreEqual(1, result.Inner.Id);
        }

        [Test]
        public void Should_resolve_the_dependency_two_layers_deep()
        {
            var containerImpl = new TestContainer();
            containerImpl.Resolvers = new List<IDbDependencyResolver>
            {
                new TransientWrappingDbDependencyResolver<Dep>((inner, container) => new Dep { Id = 3, Inner = inner }),
                new TransientWrappingDbDependencyResolver<Dep>((inner, container) => new Dep { Id = 2, Inner = inner }),
                new InstanceDbDependencyResolver<Dep>(new Dep { Id = 1 })
            };

            var result = containerImpl.Resolve<Dep>();

            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Id);
            Assert.AreEqual(2, result.Inner.Id);
            Assert.AreEqual(1, result.Inner.Inner.Id);
        }

        [Test]
        public void Should_return_null_when_the_type_does_not_match()
        {
            var resolver = new TransientWrappingDbDependencyResolver<object>((inner, container) => new object());

            var result = resolver.Resolve(typeof(int), null);

            Assert.IsNull(result);
        }

        [Test]
        public void Should_throw_an_exception_when_inner_dependency_could_not_be_resolved()
        {
            var dep = new Dep();
            var containerImpl = new TestContainer();
            containerImpl.Resolvers = new List<IDbDependencyResolver>
            {
                new TransientWrappingDbDependencyResolver<Dep>((inner, container) => new Dep { Inner = inner })
            };

            Assert.Throws<MongoConfigurationException>(() => containerImpl.Resolve<Dep>());
        }

        private class TestContainer : IDbConfigurationContainer
        {
            public List<IDbDependencyResolver> Resolvers { get; set; }

            public object Resolve(Type type)
            {
                foreach (var resolver in Resolvers)
                {
                    var resolved = resolver.Resolve(type, this);
                    if (resolved != null)
                    {
                        return resolved;
                    }
                }

                throw new MongoConfigurationException(string.Format("Unable to resolve {0}.", type));
            }
        }

        private class Dep
        {
            public int Id { get; set; }

            public Dep Inner { get; set; }
        }
    }
}