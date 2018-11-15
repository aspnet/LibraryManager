using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.LibraryManager.Vsix;
using Microsoft.WebTools.Languages.Json.Parser;
using Microsoft.WebTools.Languages.Json.Parser.Nodes;
using Microsoft.WebTools.Languages.Shared.Parser;
using Microsoft.WebTools.Languages.Shared.Parser.Nodes;
using Microsoft.WebTools.Languages.Shared.Utility;

namespace Microsoft.Web.LibraryManager.Test
{
    [TestClass]
    public class JsonHelpersTest
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
        [DataRow(150, "\"jquery@3.3.1\"")]  // An inside token node postion
        [DataRow(1, "{")]   // The first token node postion
        //[DataRow(171, "}")] // The last token node position
        public void JsonHelpers_GetNodeBeforePosition_ValidJson(int position, string expectedText)
        {
            DocumentNode documentNode = JsonNodeParser.Parse(_validJsonText);
            Node node = JsonHelpers.GetNodeBeforePosition(position, documentNode);

            Assert.IsTrue(node.IsToken);
            Assert.AreEqual(expectedText, node.GetText());
        }

        [DataTestMethod]
        [DataRow(150, "\"jquery@3.3.1\"")]  // An inside token node postion
        [DataRow(1, "{")]   // The first token node postion
        //[DataRow(170, "}")] // The last token node position
        public void JsonHelpers_GetNodeBeforePosition_InvalidJson(int position, string expectedText)
        {
            DocumentNode documentNode = JsonNodeParser.Parse(_invalidJsonText);
            Node node = JsonHelpers.GetNodeBeforePosition(position, documentNode);

            Assert.IsTrue(node.IsToken);
            Assert.AreEqual(expectedText, node.GetText());
        }

        [TestMethod]
        public void JsonHelpers_GetNodeBeforePosition_EmptyFile()
        {
            DocumentNode documentNode = JsonNodeParser.Parse(string.Empty);
            Node node = JsonHelpers.GetNodeBeforePosition(0, documentNode);

            Assert.IsNull(node);
        }

        [DataTestMethod]
        [DataRow(150, 4)]   // An inside index position
        [DataRow(1, 1)] // The first index
        [DataRow(171, 5)]   // The last index
        public void JsonHelpers_FindInsertIndex_ValidJson(int position, int expectedIndex)
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
        public void JsonHelpers_FindInsertIndex_InvalidJson(int position, int expectedIndex)
        {
            Node complexNode = JsonNodeParser.Parse(_invalidJsonText).GetNodeSlot(0);
            SortedNodeList<Node> children = JsonHelpers.GetChildren(complexNode);
            int actualIndex = JsonHelpers.FindInsertIndex(children, position);

            Assert.AreEqual(expectedIndex, actualIndex);
        }

        [TestMethod]
        public void JsonHelpers_GetChildren_ValidJson()
        {
            DocumentNode documentNode = JsonNodeParser.Parse(_validJsonText);
            ObjectNode topLevelValue = (ObjectNode)documentNode.TopLevelValue;
            SortedNodeList<Node> children = JsonHelpers.GetChildren(topLevelValue);

            Assert.AreEqual(5, children.Count);
            Assert.AreEqual(NodeKind.OpenCurlyBrace, children[0].Kind);
            Assert.AreEqual(NodeKind.JSON_Member, children[1].Kind);
            Assert.AreEqual(NodeKind.JSON_Member, children[2].Kind);
            Assert.AreEqual(NodeKind.JSON_Member, children[3].Kind);
            Assert.AreEqual(NodeKind.CloseCurlyBrace, children[4].Kind);
        }

        [TestMethod]
        public void JsonHelpers_GetChildren_TokenNode()
        {
            DocumentNode documentNode = JsonNodeParser.Parse(_validJsonText);
            TokenNode firstToken = documentNode.GetFirstToken();

            Assert.AreEqual(0, JsonHelpers.GetChildren(firstToken).Count);
        }
    }
}
