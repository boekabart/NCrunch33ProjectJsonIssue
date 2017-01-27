using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Boekabart.Issue.Xml;
using Xunit;

namespace Boekabart.Issue.Tests.Xml
{
    public class XmlExtensionsTests
    {
        private static readonly XNamespace PrimaryNamespace = "urn:edwin:bart";
        private static readonly XNamespace SecondaryNamespace = "urn:abc";
        private static readonly XNamespace NonExistingNamespace = "urn:abc:def";
        private static readonly XDocument ThreeLevelXmlDocument = XDocument.Parse(ThreeLevelXml);
        private const string ThreeLevelXml = @"
<Root xmlns='urn:edwin:bart' xmlns:abc='urn:abc'>
    <Level>
        <Two>Zwei</Two>
        <Two>Deux</Two>
        <abc:Two>TweeAbc</abc:Two>
    </Level>
    <Two>MinusTwo</Two>
    <abc:Two/>
</Root>";

        [Fact]
        public void XDocumentNsElementsDefaultNamespaceByStringTest()
        {
            var xDocument = ThreeLevelXmlDocument;

            var nsAsString = PrimaryNamespace.NamespaceName;
            var ns2AsString = SecondaryNamespace.NamespaceName;
            var wrongNsAsString = NonExistingNamespace.NamespaceName;

            Assert.Equal(2, xDocument.NsElements(nsAsString, "Root", "Level", "Two").Count());
            Assert.Equal(1, xDocument.NsElements(nsAsString, "Root", "Two").Count());
            Assert.Equal(0, xDocument.NsElements(wrongNsAsString, "Root", "Two").Count());
            Assert.Equal(1, xDocument.NsElements(nsAsString, "Root").Count());
            Assert.Equal(1, xDocument.Root.NsElements(ns2AsString, "Two").Count());
            Assert.Equal(1, xDocument.Root.NsElements(ns2AsString, new[] { "Two" }.AsEnumerable()).Count());
        }

        [Fact]
        public void XDocumentNsElementsDefaultNamespaceTest()
        {
            var xDocument = ThreeLevelXmlDocument;
            Assert.Equal(2, xDocument.NsElements(PrimaryNamespace, "Root", "Level", "Two").Count());
            Assert.Equal(1, xDocument.NsElements(PrimaryNamespace, "Root", "Two").Count());
            Assert.Equal(0, xDocument.NsElements("urn:wrong:ns", "Root", "Two").Count());
            Assert.Equal(1, xDocument.NsElements(PrimaryNamespace, "Root").Count());
            Assert.Equal(1, xDocument.Root.NsElements(SecondaryNamespace, "Two").Count());
            Assert.Equal(1, xDocument.Root.NsElements(SecondaryNamespace, new[] { "Two" }.AsEnumerable()).Count());
        }

        [Fact]
        public void XElementNsElementsDefaultNamespaceByStringTest()
        {
            var xDocument = ThreeLevelXmlDocument;
            var nsAsString = PrimaryNamespace.NamespaceName;
            Assert.Equal(2, xDocument.NsElements(nsAsString, "Root").NsElements(nsAsString, "Level", "Two").Count());
            Assert.Equal(2, xDocument.NsElements(nsAsString, "Root").NsElements(nsAsString, new[] { "Level", "Two" }.AsEnumerable()).Count());
            Assert.Equal(1, xDocument.NsElements(nsAsString, "Root").NsElements(nsAsString, "Two").Count());
            Assert.Equal(0, xDocument.NsElements("urn:wrong:ns", "Root").NsElements(nsAsString, "Two").Count());
        }

        [Fact]
        public void XElementNsElementsDefaultNamespaceTest()
        {
            var xDocument = ThreeLevelXmlDocument;
            Assert.Equal(2, xDocument.NsElements(PrimaryNamespace, "Root").NsElements(PrimaryNamespace, "Level", "Two").Count());
            Assert.Equal(2, xDocument.NsElements(PrimaryNamespace, "Root").NsElements(PrimaryNamespace, new[] { "Level", "Two" }.AsEnumerable()).Count());
            Assert.Equal(1, xDocument.NsElements(PrimaryNamespace, "Root").NsElements(PrimaryNamespace, "Two").Count());
            Assert.Equal(0, xDocument.NsElements("urn:wrong:ns", "Root").NsElements(PrimaryNamespace, "Two").Count());
        }

        [Fact]
        public void XElementValuesTest()
        {
            var xDocument = ThreeLevelXmlDocument;
            var values = xDocument.NsElements(PrimaryNamespace, "Root").NsElements(PrimaryNamespace, "Level", "Two").Values().ToList();
            Assert.Equal(2, values.Count);
            Assert.Equal(new[] { "Zwei", "Deux" }, values);
        }

        [Fact]
        public void XElementClone1Test()
        {
            var xDocument = ThreeLevelXmlDocument;

            var nsAsString = PrimaryNamespace.NamespaceName;

            var xElement = xDocument.NsElements(nsAsString, "Root", "Two").FirstOrDefault();
            Assert.NotNull(xElement);
            Assert.Equal(xElement.ToString(), xElement.Clone().ToString());
        }

        [Fact]
        public void XElementClone2Test()
        {
            var xDocument = ThreeLevelXmlDocument;

            var ns2AsString = SecondaryNamespace.NamespaceName;

            var xElement = xDocument.Root.NsElements(ns2AsString, "Two").FirstOrDefault();
            Assert.NotNull(xElement);
            Assert.Equal(xElement.ToString(), xElement.Clone().ToString());
        }

        [Fact]
        public void XElementNullCloneTest()
        {
            XElement xElement = null;

            Assert.Null(xElement.Clone());
        }

        [Fact]
        public void XDocumentNullCloneTest()
        {
            XDocument xDocument = null;

            Assert.Null(xDocument.Clone());

        }

        [Fact]
        public void XDocumentCloneTest()
        {
            var xDocument = ThreeLevelXmlDocument;

            var xDocument2 = xDocument.Clone();

            Assert.NotNull(xDocument2);
            Assert.Equal(xDocument.ToString(), xDocument2.ToString());

        }

        private static readonly XDocument TimeXmlDocument = XDocument.Parse(TimeXml);
        private const string TimeXml = @"
<Root>
    <Time>2015-12-03T17:39:52.27Z</Time>
    <Attr start='2015-12-03T11:39:52.27Z' minutes='1440' notMinutes='twelve'/>
    <Level/>
    <Level start='2015-12-03T11:39:52.27Z'>
        <Two>2015-12-03T11:39:52.27Z</Two>
        <Two>NotADate</Two>
    </Level>
</Root>";

        [Fact]
        public void TimeFromAttributePositiveTest()
        {
            Assert.NotNull(TimeXmlDocument.Root);
            var res = TimeXmlDocument.Root.Element("Attr").TimeFromAttribute("start");
            Assert.True(res.HasValue);
            // ReSharper disable once PossibleInvalidOperationException
            Assert.Equal(0, res.Value.Offset.Ticks);
            Assert.Equal(2015, res.Value.Year);
        }

        [Fact]
        public void TimeFromAttributeNegativeTest()
        {
            Assert.NotNull(TimeXmlDocument.Root);
            var res = TimeXmlDocument.Root.Element("Time").TimeFromAttribute("start");
            Assert.False(res.HasValue);
        }

        [Fact]
        public void MinutesFromAttributePositiveTest()
        {
            Assert.NotNull(TimeXmlDocument.Root);
            var res = TimeXmlDocument.Root.Element("Attr").MinutesFromAttribute("minutes");
            Assert.True(res.HasValue);
            // ReSharper disable once PossibleInvalidOperationException
            Assert.Equal(TimeSpan.FromMinutes(1440), res.Value);
        }

        [Fact]
        public void MinutesFromAttributeNoAttributeTest()
        {
            Assert.NotNull(TimeXmlDocument.Root);
            var res = TimeXmlDocument.Root.Element("Attr").MinutesFromAttribute("minutez");
            Assert.False(res.HasValue);
        }

        [Fact]
        public void MinutesFromAttributeBadValueTest()
        {
            Assert.NotNull(TimeXmlDocument.Root);
            var res = TimeXmlDocument.Root.Element("Attr").MinutesFromAttribute("notMinutes");
            Assert.False(res.HasValue);
        }

        [Fact]
        public void AsDateTimeOffsetsTest()
        {
            Assert.NotNull(TimeXmlDocument.Root);
            var res = TimeXmlDocument.Root.Elements("Time").AsDateTimeOffsets().ToList();
            Assert.Equal(1, res.Count);
            Assert.Equal(2015, res.First().Year);
            Assert.Equal(17, res.First().Hour);
        }

        [Fact]
        public void AsDateTimeOffsetsFailTest()
        {
            Assert.NotNull(TimeXmlDocument.Root);
            var twos = TimeXmlDocument.Root.Elements("Level").Elements("Two");
            Assert.Throws<FormatException>(() => twos.AsDateTimeOffsets().ToList());
        }

        [Fact]
        public void ArgumentExceptionThrownWhenXpathStartsWithSlash()
        {
            var input =
                XElement.Parse(
                    "<Root><Element><Genre href='blackout'><Definition>yes</Definition></Genre></Element></Root>");
            const string xpath = "/Element/Genre[@href='startTime']/Definition";

            Assert.Throws<ArgumentException>(() => input.EnsureXPath(xpath));
        }

        [Fact]
        public void EnsureXPathNopIfExists()
        {
            var input =
                XElement.Parse(
                    "<Root><Element><Genre href='start'><Definition>2011</Definition></Genre><Genre href='end'/></Element></Root>");
            const string xpath = "Element/Genre[@href='start']/Definition";
            Assert.NotNull(input.XPathSelectElement(xpath));
            var output = input.EnsureXPath(xpath);
            Assert.Same(input, output);
            Assert.NotNull(output.XPathSelectElement(xpath));
        }

        [Fact]
        public void EnsureXPathDifferentInstanceIfNotExists()
        {
            var input =
                XElement.Parse(
                    "<Root><Element><Genre href='blackout'><Definition>yes</Definition></Genre></Element></Root>");
            const string xpath = "Element/Genre[@href='startTime']/Definition";
            var output = input.EnsureXPath(xpath);
            Assert.NotSame(input, output);
        }

        [Fact]
        public void EnsureWorksForElementWithOneAttribute()
        {
            var input =
                XElement.Parse(
                    "<Root><Element><Genre href='blackout'><Definition>yes</Definition></Genre></Element></Root>");
            const string xpath = "Element/Genre[@href='startTime']/Definition";
            var output = input.EnsureXPath(xpath);
            Assert.NotNull(output.XPathSelectElement(xpath));
        }

        [Fact]
        public void EnsureWorksForElementWithOneEmptyAttributeAlreadyExisting()
        {
            var input =
                XElement.Parse(
                    "<Root><Element><Genre href='blackout'><Definition>yes</Definition></Genre></Element></Root>");
            var expected = input.Descendants().Count();
            const string xpath = "Element/Genre[@href]/Definition";
            var output = input.EnsureXPath(xpath);
            Assert.NotNull(output.XPathSelectElement(xpath));
            Assert.Equal(expected, output.Descendants().Count());
        }

        [Fact]
        public void EnsureWorksForElementWithOneEmptyAttributeNotYetExisting()
        {
            var input =
                XElement.Parse(
                    "<Root><Element><Genre><Definition>yes</Definition></Genre></Element></Root>");
            const string xpath = "Element/Genre[@href]/Definition";
            var output = input.EnsureXPath(xpath);
            Assert.NotNull(output.XPathSelectElement(xpath));
            // Assert that it didn't modify the 1st one, but added a new one
            Assert.Equal(2, output.XPathSelectElements("/Element/Genre/Definition").Count());
        }

        [Fact]
        public void EnsureWorksForElementWithNamespace()
        {
            var input =
                XElement.Parse(
                    "<Root xmlns='urn:tva:metadata:2010'></Root>");
            const string xpath = "tva:Element/tva:Genre[@href='startTime' and @type='other']/tva:Definition";
            var tvaNamespaceManager = new XmlNamespaceManager(new NameTable());
            tvaNamespaceManager.AddNamespace("tva", "urn:tva:metadata:2010");
            var output = input.EnsureXPath(xpath, tvaNamespaceManager);
            Assert.NotNull(output.XPathSelectElement(xpath, tvaNamespaceManager));
        }

        [Fact]
        public void EnsureWorksForElementWithThreeAttributes()
        {
            var input =
                XElement.Parse(
                    "<Root><Element><Genre href='start' type='first'><Definition>yes</Definition></Genre></Element></Root>");
            const string xpath = "Element/Genre[@href='start' and @type='other' and @index='2']/Definition";
            var output = input.EnsureXPath(xpath);
            Assert.NotNull(output.XPathSelectElement(xpath));
        }

        [Fact]
        public void EnsureXPathWorksForExistingAttribute()
        {
            var input =
                XElement.Parse(
                    "<Root><Element><Genre href='start' type='first'><Definition>yes</Definition></Genre></Element></Root>");
            const string xpath = "Element/Genre/@href";
            var output = input.EnsureXPath(xpath);
            Assert.Same(input, output);
        }

        [Fact]
        public void EnsureXPathWorksForNotExistingAttribute()
        {
            var input =
                XElement.Parse(
                    "<Root><Element><Genre href='start' type='first'><Definition>yes</Definition></Genre></Element></Root>");
            const string xpath = "Element/Genre/Definition/@language";
            var output = input.EnsureXPath(xpath);
            Assert.NotSame(input, output);
            Assert.NotNull(output.XPathSelectObjects(xpath));
        }

        [Fact]
        public void ArgumentExceptionIsThrownForInvalidXpath()
        {
            var input =
                XElement.Parse(
                    "<Root xmlns='urn:tva:metadata:2010'></Root>");
            const string xpath = "/tva:Element/tva:Genre[@href='startTime' and @type='other']/tva:Definition";
            Assert.Throws<ArgumentException>(() => input.GetOrCreateElementFromXPathAfterElement(xpath, new List<string>()));
        }

        [Fact]
        public void EnsureElementIsAdded()
        {
            var input =
                XElement.Parse(
                    "<Root></Root>");
            const string xpath = "A";
            var output = input.GetOrCreateElementFromXPathAfterElement(xpath, new List<string>());
            Assert.Equal("A", output.Name.LocalName);
            Assert.Same(output, input.FirstNode);

        }

        [Fact]
        public void EnsureElementIsAddedAsFirstChildWhenNoPreconditionListIsAdded()
        {
            var input =
                XElement.Parse(
                    "<Root><A></A></Root>");
            const string xpath = "B";
            var output = input.GetOrCreateElementFromXPathAfterElement(xpath);
            Assert.Equal("B", output.Name.LocalName);
            Assert.Same(output, input.FirstNode);
        }

        [Fact]
        public void EnsureElementIsAddedAsFirstChildWhenEmptyPreconditionListIsAdded()
        {
            var input =
                XElement.Parse(
                    "<Root><A></A></Root>");
            const string xpath = "B";
            var output = input.GetOrCreateElementFromXPathAfterElement(xpath, new List<string>());
            Assert.Equal("B", output.Name.LocalName);
            Assert.Same(output, input.FirstNode);
        }

        [Fact]
        public void EnsureElementIsAddedAfterLastPreconditionElement()
        {
            var input =
                XElement.Parse(
                    "<Root><A></A></Root>");
            const string xpath = "B";
            var output = input.GetOrCreateElementFromXPathAfterElement(xpath, new List<string> { "A" });
            Assert.Equal("B", output.Name.LocalName);
            Assert.NotSame(output, input.FirstNode);
            Assert.Same(output, input.FirstNode.NextNode); //FirstNode == A, output should be 'B'
        }

        [Fact]
        public void EnsureElementIsAddedAsFirstChildWhenNoMatchesFromPreconditionListAreFound()
        {
            var input =
                XElement.Parse(
                    "<Root><A></A></Root>");
            const string xpath = "B";
            var output = input.GetOrCreateElementFromXPathAfterElement(xpath, new List<string> { "C" });
            Assert.Equal("B", output.Name.LocalName);
            Assert.Same(output, input.FirstNode);
        }

        [Fact]
        public void EnsureElementIsAddedAfterLastElementInPreconditionList()
        {
            var input =
                XElement.Parse(
                    "<Root><A></A><B></B><C></C></Root>");
            const string xpath = "D";
            var output = input.GetOrCreateElementFromXPathAfterElement(xpath, new List<string> { "A", "B", "C" });
            Assert.Equal("D", output.Name.LocalName);
            Assert.Equal(output, input.LastNode);
        }

        [Fact]
        public void EnsureElementIsAddedAfterLastElementInPreconditionList_02()
        {
            var input =
                XElement.Parse(
                    "<Root><A></A><B></B><C></C></Root>");
            const string xpath = "D";
            var output = input.GetOrCreateElementFromXPathAfterElement(xpath, new List<string> { "A", "C", "B" });
            Assert.Equal("D", output.Name.LocalName);
            Assert.Equal(output, input.FirstNode.NextNode.NextNode);
        }

        [Fact]
        public void EnsureElementIsAddedAfterLastElementInPreconditionList_03()
        {
            var input =
                XElement.Parse(
                    "<Root><C></C><B></B><A></A></Root>");
            const string xpath = "D";
            var output = input.GetOrCreateElementFromXPathAfterElement(xpath, new List<string> { "A", "B", "C" });
            Assert.Equal("D", output.Name.LocalName);
            Assert.Equal(output, input.FirstNode.NextNode);
        }

        [Fact]
        public void EnsureXpathWithAttributeDeclarationContainingWhiteSpaceCanBeUsed()
        {
            var input =
               XElement.Parse(
                   "<Root></Root>");
            const string xpath = "A [ @href ='link'\tand @type=  '65' ] /B";
            var output = input.GetOrCreateElementFromXPathAfterElement(xpath);
            Assert.Equal("B", output.Name.LocalName);
            Assert.Equal("A", output.Parent.Name.LocalName);
            Assert.Equal("link", output.Parent.Attribute("href").Value);
            Assert.Equal("65", output.Parent.Attribute("type").Value);
        }

        [Fact]
        public void EnsureXpathWithAttributeDeclarationContainingWhiteSpaceCanBeUsedForXPathSelectObjects()
        {
            var input =
               XElement.Parse(
                   "<Root></Root>");
            const string xpath = "A [ @href ='link'\tand @type=  '65' ] /B";
            var output = input.GetOrCreateElementFromXPathAfterElement(xpath);
            var xElement = (XElement)input.XPathSelectObjects(xpath).First();
            Assert.Equal("B", xElement.Name.LocalName);
            Assert.Equal("A", xElement.Parent.Name.LocalName);
            Assert.Equal("link", xElement.Parent.Attribute("href").Value);
            Assert.Equal("65", xElement.Parent.Attribute("type").Value);
            Assert.Equal("B", output.Name.LocalName);
        }

        [Theory]
        [InlineData("<A><B/><C/></A>", "<A><B /><C /><F /></A>")]
        [InlineData("<A/>", "<A><F /></A>")]
        [InlineData("<A><B/></A>", "<A><B /><F /></A>")]
        [InlineData("<A><E/></A>", "<A><E /><F /></A>")]
        public void EnsureCreateElementAfterInsertsElementAtCorrectLocation(string inputString, string expected)
        {
            var emptyXmlNamespaceManager = new XmlNamespaceManager(new NameTable());
            var input = XElement.Parse(inputString);
            var toBeInserted = new XElement("F");
            input.AddElementAfter(toBeInserted, new[] { "B", "C", "D", "E" }, emptyXmlNamespaceManager);
            Assert.Equal(expected, input.ToString(SaveOptions.DisableFormatting));
        }

        [Theory]
        [InlineData("abcDEF", "DEF")]
        [InlineData("abc", "abc")]
        [InlineData("DEF", "DEF")]
        [InlineData("aDbEcF", "DEF")]
        public void TestPrefer(string inputSequence, string expectedSequence)
        {
            var actual = new string(inputSequence.Prefer(char.IsUpper).ToArray());
            Assert.Equal(expectedSequence, actual);
        }

        [Theory]
        [InlineData("<Title xml:lang='en-US'/>", "en", true)]
        [InlineData("<Title xml:lang='de-XX'/>", "de", true)]
        [InlineData("<Title xml:lang='de-DE'/>", "en", false)]
        [InlineData("<Title/>", "en", null)]
        [InlineData("<Program xml:lang='en-US'><Title/></Program>", "en", true)]
        [InlineData("<Program xml:lang='de-DE'><Title/></Program>", "en", false)]
        void TestIsLanguage(string xmlString, string testLanguage, bool? expected)
        {
           var element = XElement.Parse(xmlString);
            while (element.HasElements)
                element = element.Elements().First();
            var actual = element.IsLanguage(testLanguage);
            Assert.Equal(expected, actual);
        }
    }
}
