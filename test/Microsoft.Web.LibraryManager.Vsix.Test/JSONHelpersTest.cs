using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Vsix;
using Microsoft.WebTools.Languages.Json.Parser;
using Microsoft.WebTools.Languages.Json.Parser.Nodes;
using Microsoft.WebTools.Languages.Shared.Parser.Nodes;
using Microsoft.WebTools.Languages.Shared.Utility;

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
        public void JSONHelpers_GetNodeBeforePosition_ValidJson(int position, string expectedText)
        {
            DocumentNode documentNode = JsonNodeParser.Parse(_validJsonText);
            Node node = JsonHelpers.GetNodeBeforePosition(position, documentNode);

            Assert.IsTrue(node.IsToken);
            Assert.AreEqual(expectedText, node.GetText());
        }

        [DataTestMethod]
        [DataRow(150, "\"jquery@3.3.1\"")]  // An inside token item postion
        [DataRow(1, "{")]   // The first token item postion
        [DataRow(170, "}")] // The last token item position
        public void JSONHelpers_GetNodeBeforePosition_InvalidJson(int position, string expectedText)
        {
            DocumentNode documentNode = JsonNodeParser.Parse(_invalidJsonText);
            Node node = JsonHelpers.GetNodeBeforePosition(position, documentNode);

            Assert.IsTrue(node.IsToken);
            Assert.AreEqual(expectedText, node.GetText());
        }

        [TestMethod]
        public void JSONHelpers_GetNodeBeforePosition_EmptyFile()
        {
            DocumentNode documentNode = JsonNodeParser.Parse(string.Empty);
            Node node = JsonHelpers.GetNodeBeforePosition(0, documentNode);

            Assert.IsNull(node);
        }

        [DataTestMethod]
        [DataRow(150, 4)]   // An inside index position
        [DataRow(1, 1)] // The first index
        [DataRow(171, 5)]   // The last index
        public void JSONHelpers_FindInsertIndex_ValidJson(int position, int expectedIndex)
        {
            Node complexNode = JsonNodeParser.Parse(_validJsonText).GetNodeSlot(0);
            SortedNodeList<Node> children = JsonHelpers.GetChildren(complexNode);
            int actualIndex = JsonHelpers.FindInsertIndex(children, position);

            Assert.AreEqual(expectedIndex, actualIndex);
        }

        [DataTestMethod]
        [DataRow(150, 4)]   // An inside index position
        [DataRow(1, 1)] // The first index
        [DataRow(170, 5)]   // The last index
        public void JSONHelpers_FindInsertIndex_InvalidJson(int position, int expectedIndex)
        {
            Node complexNode = JsonNodeParser.Parse(_invalidJsonText).GetNodeSlot(0);
            SortedNodeList<Node> children = JsonHelpers.GetChildren(complexNode);
            int actualIndex = JsonHelpers.FindInsertIndex(children, position);

            Assert.AreEqual(expectedIndex, actualIndex);
        }
    }
}
