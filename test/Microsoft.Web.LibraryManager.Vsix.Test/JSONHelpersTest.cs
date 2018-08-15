using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Vsix;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class JSONHelpersTest
    {
        private const string _validJsonText = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""libraries"": [
    {
      ""destination"": ""wwwroot/lib/jquery/"",
      ""library"": ""jquery@3.3.1""
    }
  ]
}";
        private const string _invalidJsonText = @"{
  ""version"": ""1.0"",
  ""defaultProvider"": ""cdnjs"",
  ""libraries"": [
    {
      ""destination"": ""wwwroot/lib/jquery/"",
      ""library"": ""jquery@3.3.1""
    }
  
}";

        [DataTestMethod]
        [DataRow(150, "\"jquery@3.3.1\"")]  // An inside token item postion
        [DataRow(1, "{")]   // The first token item postion
        [DataRow(171, "}")] // The last token item position
        public void JSONHelpers_GetItemBeforePosition_ValidJson(int position, string expectedText)
        {
            JSONComplexItem complexItem = JSONParser.Parse(_validJsonText);

            JSONParseItem item = JsonHelpers.GetItemBeforePosition(position, complexItem);

            Assert.IsTrue(item is JSONTokenItem);
            Assert.AreEqual(expectedText, item.Text);
        }

        [DataTestMethod]
        [DataRow(150, "\"jquery@3.3.1\"")]  // An inside token item postion
        [DataRow(1, "{")]   // The first token item postion
        [DataRow(170, "}")] // The last token item position
        public void JSONHelpers_GetItemBeforePosition_InvalidJson(int position, string expectedText)
        {
            JSONComplexItem complexItem = JSONParser.Parse(_invalidJsonText);

            JSONParseItem item = JsonHelpers.GetItemBeforePosition(position, complexItem);

            Assert.IsTrue(item is JSONTokenItem);
            Assert.AreEqual(expectedText, item.Text);
        }

        [TestMethod]
        public void JSONHelpers_GetItemBeforePosition_EmptyFile()
        {
            JSONComplexItem complexItem = JSONParser.Parse(string.Empty);

            JSONParseItem item = JsonHelpers.GetItemBeforePosition(0, complexItem);

            Assert.IsNull(item);
        }

        [DataTestMethod]
        [DataRow(150, 4)]   // An inside index position
        [DataRow(1, 1)] // The first index
        [DataRow(171, 5)]   // The last index
        public void JSONHelpers_FindInsertIndex_ValidJson(int position, int expectedIndex)
        {
            JSONComplexItem item = JSONParser.Parse(_validJsonText).Children[0] as JSONComplexItem;
            JSONParseItemList items = item.Children;

            int actualIndex = JsonHelpers.FindInsertIndex(items, position);

            Assert.AreEqual(expectedIndex, actualIndex);
        }

        [DataTestMethod]
        [DataRow(150, 4)]   // An inside index position
        [DataRow(1, 1)] // The first index
        [DataRow(170, 5)]   // The last index
        public void JSONHelpers_FindInsertIndex_InvalidJson(int position, int expectedIndex)
        {
            JSONComplexItem item = JSONParser.Parse(_invalidJsonText).Children[0] as JSONComplexItem;
            JSONParseItemList items = item.Children;

            int actualIndex = JsonHelpers.FindInsertIndex(items, position);

            Assert.AreEqual(expectedIndex, actualIndex);
        }
    }
}
