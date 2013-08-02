using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MongoDB.Driver.Core
{
    public abstract class Specification : DatabaseTest
    {
        [TestFixtureSetUp]
        public void SetUpFixture()
        {
            Given();
            When();
        }

        protected virtual void Given()
        { }
        
        protected abstract void When();


        public class ThenAttribute : TestAttribute
        { }

        public class AndAttribute : TestAttribute
        { }
    }
}