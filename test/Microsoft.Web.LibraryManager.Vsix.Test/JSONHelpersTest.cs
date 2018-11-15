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

        [TestMethod]
        public void JsonHelpers_GetNodeBeforePosition_ValidJson()
        {
            DocumentNode documentNode = JsonNodeParser.Parse(_validJsonText);

            // First token node
            Node node = JsonHelpers.GetNodeBeforePosition(1, documentNode);
            Assert.IsTrue(node.IsToken);
            Assert.AreEqual("{", node.GetText());

            // Inside member node
            node = JsonHelpers.GetNodeBeforePosition(150, documentNode);
            Assert.IsTrue(node.IsToken);
            Assert.AreEqual("\"jquery@3.3.1\"", node.GetText());

            // Last token node
            node = JsonHelpers.GetNodeBeforePosition(171, documentNode);
            Assert.IsTrue(node.IsToken);
            Assert.AreEqual("}", node.GetText());
        }

        [TestMethod]
        public void JsonHelpers_GetNodeBeforePosition_InvalidJson()
        {
            DocumentNode documentNode = JsonNodeParser.Parse(_invalidJsonText);

            // First token node
            Node node = JsonHelpers.GetNodeBeforePosition(1, documentNode);
            Assert.IsTrue(node.IsToken);
            Assert.AreEqual("{", node.GetText());

            // Inside member node
            node = JsonHelpers.GetNodeBeforePosition(150, documentNode);
            Assert.IsTrue(node.IsToken);
            Assert.AreEqual("\"jquery@3.3.1\"", node.GetText());

            // Last token node
            node = JsonHelpers.GetNodeBeforePosition(170, documentNode);
            Assert.IsTrue(node.IsToken);
            Assert.AreEqual("}", node.GetText());
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
