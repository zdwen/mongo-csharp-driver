using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Diagnostics;
using MongoDB.Driver.Core.Events;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Configuration.Resolvers
{
    [TestFixture]
    public class ReflectedDbDependencyResolverTests
    {
        [Test]
        public void Should_resolve_a_type_with_no_arguments()
        {
            var container = Substitute.For<IDbConfigurationContainer>();
            var subject = new ReflectedDbDependencyResolver<ITest, TestNoArgs>();

            var result = subject.Resolve(typeof(ITest), container);

            Assert.IsNotNull(result);
            Assert.IsInstanceOf<TestNoArgs>(result);
        }

        [Test]
        public void Should_resolve_a_type_with_one_argument()
        {
            var dep1 = new Dep1();
            var container = Substitute.For<IDbConfigurationContainer>();
            container.Resolve(typeof(Dep1)).Returns(dep1);
            var subject = new ReflectedDbDependencyResolver<ITest, TestOneArg>();

            var result = subject.Resolve(typeof(ITest), container);

            Assert.IsNotNull(result);
            Assert.IsInstanceOf<TestOneArg>(result);
            Assert.AreSame(dep1, ((TestOneArg)result).Dep1);
        }

        [Test]
        public void Should_resolve_a_type_with_two_arguments()
        {
            var dep1 = new Dep1();
            var dep2 = new Dep2();
            var container = Substitute.For<IDbConfigurationContainer>();
            container.Resolve(typeof(Dep1)).Returns(dep1);
            container.Resolve(typeof(Dep2)).Returns(dep2);
            var subject = new ReflectedDbDependencyResolver<ITest, TestTwoArgs>();

            var result = subject.Resolve(typeof(ITest), container);

            Assert.IsNotNull(result);
            Assert.IsInstanceOf<TestTwoArgs>(result);
            Assert.AreSame(dep1, ((TestTwoArgs)result).Dep1);
            Assert.AreSame(dep2, ((TestTwoArgs)result).Dep2);
        }

        private class Dep1
        { }

        private class Dep2
        { }

        private interface ITest
        { }

        private class TestNoArgs : ITest
        { }

        private class TestOneArg : ITest
        {
            public Dep1 Dep1;

            public TestOneArg(Dep1 dep1)
            {
                Dep1 = dep1;
            }
        }

        private class TestTwoArgs : ITest
        {
            public Dep1 Dep1;
            public Dep2 Dep2;

            public TestTwoArgs(Dep1 dep1, Dep2 dep2)
            {
                Dep1 = dep1;
                Dep2 = dep2;
            }
        }
    }
}