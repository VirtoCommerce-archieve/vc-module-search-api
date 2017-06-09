using VirtoCommerce.SearchApiModule.Data.Helpers;
using Xunit;

namespace VirtoCommerce.SearchApiModule.Test
{
    [Trait("Category", "CI")]
    public class IndexHelperTests
    {
        private class MyClass
        {
            public bool Boolean { get; set; }
            public int Integer { get; set; }
            public MyClass Reference { get; set; }
        }

        [Fact]
        public void Serialize()
        {
            var obj = new MyClass
            {
                Boolean = true,
                Integer = 1,
                Reference = new MyClass(),
            };

            var result = IndexHelper.SerializeObject(obj);

            Assert.Equal("{\"Boolean\":true,\"Integer\":1,\"Reference\":{\"Boolean\":false,\"Integer\":0}}", result);
        }
    }
}
