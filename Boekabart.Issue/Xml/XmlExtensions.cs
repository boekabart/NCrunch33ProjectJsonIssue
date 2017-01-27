using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Boekabart.Issue.Xml
{
    public static class XmlExtensions
    {
        private static readonly Lazy<Regex> EnsureNodeRegex =
            new Lazy<Regex>(
                () => new Regex(@"/?(([\w]+):)?([\w]+)\s*(\[\s*@[\w]+(\s*=\s*'[^']*'|)(\s+and\s+@[\w]+(\s*=\s*'[^']*'|))*\s*\])?|/@([\w]+)"));

        private static readonly Lazy<Regex> EnsureNodeAttributesRegex =
            new Lazy<Regex>(() => new Regex(@"@([\w]+)\s*(=\s*'([^']*)'|)"));

        private static readonly XmlNamespaceManager EmptyXmlNamespaceManager = new XmlNamespaceManager(new NameTable());

        public static XElement EnsureXPath(this XElement input, string xPath)
        {
            return input.EnsureXPath(xPath, EmptyXmlNamespaceManager);
        }

        public static XElement EnsureXPath(this XElement input, string xPath, XmlNamespaceManager nsmgr)
        {
            if (xPath.StartsWith("/"))
                throw new ArgumentException("Absolute xPaths are not supported", nameof(xPath));
            if (XPathSelectObjects(input, xPath, nsmgr).Any())
                return input;
            var clone = input.Clone();
            clone.GetOrCreateNodeFromXPath(xPath, nsmgr);
            return clone;
        }

        public static IEnumerable<XObject> XPathSelectObjects(this XNode input, string xPath)
        {
            return input.XPathSelectObjects(xPath, EmptyXmlNamespaceManager);
        }

        public static IEnumerable<XObject> XPathSelectObjects(this XNode input, string xPath,
            IXmlNamespaceResolver nsmgr)
        {
            return ((IEnumerable) input.XPathEvaluate(xPath, nsmgr)).Cast<XObject>();
        }

        /// <summary>
        /// Creates elements according to the Xpath. Use a format like configuration/appSettings/add[@key='name' and @attr]/ns:name
        /// </summary>
        /// <param name="node">The doc.</param>
        /// <param name="xpath">The xpath.</param>
        /// <param name="nsmgr"></param>
        /// <returns></returns>
        public static XElement CreateElementFromXPath(this XElement node, string xpath,
            IXmlNamespaceResolver nsmgr = null)
        {
            var createdNode = _GetOrCreateNodeFromXPath(node, xpath, true, nsmgr);
            var element = createdNode as XElement;
            if (element != null)
                return element;
            throw new ArgumentException("XPath must end in element, not attribute", nameof(xpath));
        }

        /// <summary>
        /// Gets Or Creates elements according to the Xpath. Use a format like configuration/appSettings/add[@key='name' and @attr]/ns:name
        /// </summary>
        /// <param name="node">The doc.</param>
        /// <param name="xpath">The xpath.</param>
        /// <param name="nsmgr"></param>
        /// <returns></returns>
        public static XElement GetOrCreateElementFromXPath(this XElement node, string xpath,
            IXmlNamespaceResolver nsmgr = null)
        {
            var createdNode = _GetOrCreateNodeFromXPath(node, xpath, false, nsmgr);
            var element = createdNode as XElement;
            if (element != null)
                return element;
            throw new ArgumentException("XPath must end in element, not attribute", nameof(xpath));
        }

        /// <summary>
        /// Creates elements and attributes according to the Xpath. Use a format like configuration/appSettings/add[@key='name' and @attr]/@value
        /// </summary>
        /// <param name="node">The doc.</param>
        /// <param name="xpath">The xpath.</param>
        /// <param name="nsmgr"></param>
        /// <returns></returns>
        public static XObject CreateNodeFromXPath(this XElement node, string xpath,
            IXmlNamespaceResolver nsmgr = null)
        {
            return _GetOrCreateNodeFromXPath(node, xpath, true, nsmgr);
        }

        /// <summary>
        /// Creates elements and attributes according to the Xpath. Use a format like configuration/appSettings/add[@key='name' and @attr]/@value
        /// Any part of the path that already exists, is not created again
        /// </summary>
        /// <param name="node">The doc.</param>
        /// <param name="xpath">The xpath.</param>
        /// <param name="nsmgr"></param>
        /// <returns></returns>
        public static XObject GetOrCreateNodeFromXPath(this XElement node, string xpath,
            IXmlNamespaceResolver nsmgr = null)
        {
            return _GetOrCreateNodeFromXPath(node, xpath, false, nsmgr);
        }

        private static XObject _GetOrCreateNodeFromXPath(this XElement node, string xpath, bool alwaysCreate, IXmlNamespaceResolver nsmgr = null)
        {
            if (xpath.StartsWith("/"))
                throw new ArgumentException("Absolute xPaths are not supported", nameof(xpath));
            nsmgr = nsmgr ?? EmptyXmlNamespaceManager;
            // Find matches
            var m = EnsureNodeRegex.Value.Match(xpath);

            var currentNode = node;
            var currentPath = new StringBuilder();

            while (m.Success && currentNode != null)
            {
                var currentXPath = m.Groups[0].Value; // "/configuration" or "/appSettings" or "/add"
                var elementNs = m.Groups[2].Value; // "configuration" or "appSettings" or "add"
                var elementName = m.Groups[3].Value; // "configuration" or "appSettings" or "add"
                var filters = m.Groups[4].Value; // "" or "key"
                var attributeName = m.Groups[8].Value; // "" or "value"

                var builder = currentPath.Append(currentXPath);
                var relativePath = builder.ToString();
                var newNode = alwaysCreate ? null : node.XPathSelectObjects(relativePath, nsmgr).FirstOrDefault();

                if (newNode == null)
                {
                    if (!string.IsNullOrEmpty(attributeName))
                    {
                        currentNode.Add(new XAttribute(attributeName, string.Empty));
                        newNode = node.XPathSelectObjects(relativePath, nsmgr).First();
                    }
                    else if (!string.IsNullOrEmpty(elementName))
                    {
                        XNamespace ns = nsmgr.LookupNamespace(elementNs);
                        var xName = ns != null ? ns + elementName : elementName;
                        var element = new XElement(xName);
                        var m2 = EnsureNodeAttributesRegex.Value.Match(filters);
                        while (m2.Success)
                        {
                            var filterName = m2.Groups[1].Value; // "" or "key"
                            var filterValue = m2.Groups[3].Value; // "" or "name"
                            if (!string.IsNullOrEmpty(filterName))
                            {
                                element.Add(new XAttribute(filterName, filterValue));
                            }
                            m2 = m2.NextMatch();
                        }

                        currentNode.Add(element);
                        newNode = element;
                    }
                    else
                    {
                        throw new FormatException("The given xPath is not supported " + relativePath);
                    }
                }

                currentNode = newNode as XElement;

                m = m.NextMatch();
            }

            // Assure that the node is found or created
            if (node.XPathSelectObjects(xpath, nsmgr) == null)
            {
                throw new FormatException("The given xPath cannot be created " + xpath);
            }

            return currentNode;
        }

        public static XElement GetOrCreateElementFromXPathAfterElement(this XElement node, string xpath,
            IEnumerable<string> xpathAfterNodeList = null, IXmlNamespaceResolver nsmgr = null)
        {
            if (xpath.StartsWith("/"))
                throw new ArgumentException("Absolute xPaths are not supported", nameof(xpath));

            xpathAfterNodeList = xpathAfterNodeList ?? new List<string>();
            nsmgr = nsmgr ?? EmptyXmlNamespaceManager;

            // Find matches
            var m = EnsureNodeRegex.Value.Match(xpath);

            var currentNode = node;
            var currentPath = new StringBuilder();
            for (int xpathLevel = 0; m.Success && currentNode != null; xpathLevel++)
            {
                var currentXPath = m.Groups[0].Value; // "/configuration" or "/appSettings" or "/add"
                var elementNs = m.Groups[2].Value; // "configuration" or "appSettings" or "add"
                var elementName = m.Groups[3].Value; // "configuration" or "appSettings" or "add"
                var filters = m.Groups[4].Value; // "" or "key"

                var builder = currentPath.Append(currentXPath);
                var relativePath = builder.ToString();
                var newNode = node.XPathSelectObjects(relativePath, nsmgr).FirstOrDefault();


                if (newNode == null)
                {
                   if (!string.IsNullOrEmpty(elementName))
                    {
                        XNamespace ns = nsmgr.LookupNamespace(elementNs);
                        var xName = ns != null ? ns + elementName : elementName;
                        var element = new XElement(xName);
                        var m2 = EnsureNodeAttributesRegex.Value.Match(filters);
                        while (m2.Success)
                        {
                            var filterName = m2.Groups[1].Value; // "" or "key"
                            var filterValue = m2.Groups[3].Value; // "" or "name"
                            if (!string.IsNullOrEmpty(filterName))
                            {
                                element.Add(new XAttribute(filterName, filterValue));
                            }
                            m2 = m2.NextMatch();
                        }
                        if (xpathLevel == 0)
                        {
                            AddElementAfter(currentNode, element, xpathAfterNodeList, nsmgr);
                        }
                        else
                        {
                            currentNode.Add(element);
                        }

                        newNode = element;
                    }
                    else
                    {
                        throw new FormatException("The given xPath is not supported " + relativePath);
                    }
                }

                currentNode = newNode as XElement;

                m = m.NextMatch();
            }

            // Assure that the node is found or created
            if (node.XPathSelectObjects(xpath, nsmgr) == null)
            {
                throw new FormatException("The given xPath cannot be created " + xpath);
            }

            return currentNode;
        }

        public static void AddElementAfter(this XElement currentNode, XElement element, IEnumerable<string> xpathAfterNodeList, IXmlNamespaceResolver nsmgr)
        {
            var xPathSelectElement =
                xpathAfterNodeList.Reverse()
                    .Select(s => currentNode.XPathSelectElement(s + "[last()]", nsmgr))
                    .FirstOrDefault(foundElem => foundElem != null);
            if (xPathSelectElement != null)
            {
                xPathSelectElement.AddAfterSelf(element); // Add node after xPathSelectElement
            }
            else
            {
                currentNode.AddFirst(element);
            }
        }

        public static IEnumerable<XElement> NsElements(this XDocument xmlDoc, string ns, params string[] elementNames)
        {
            return xmlDoc.NsElements((XNamespace) ns, elementNames);
        }

        public static IEnumerable<XElement> NsElements(this XDocument xmlDoc, XNamespace nameSpace,
            params string[] elementNames)
        {
            return !elementNames.Any()
                ? Enumerable.Empty<XElement>()
                : xmlDoc.Elements(nameSpace + elementNames[0]).NsElements(nameSpace, elementNames.Skip(1).ToArray());
        }

        public static IEnumerable<XElement> NsElements(this XElement element, XNamespace nameSpace,
            IEnumerable<string> elementNames)
        {
            return element.Elements(elementNames.Select(name => nameSpace + name));
        }

        public static IEnumerable<XElement> NsElements(this XElement element, string ns,
            IEnumerable<string> elementNames)
        {
            return element.NsElements((XNamespace) ns, elementNames);
        }

        public static IEnumerable<XElement> NsElements(this XElement element, string ns, params string[] elementNames)
        {
            return element.NsElements((XNamespace) ns, elementNames);
        }

        public static IEnumerable<XElement> NsElements(this XElement element, XNamespace nameSpace,
            params string[] elementNames)
        {
            return element.Elements(elementNames.Select(name => nameSpace + name));
        }

        public static IEnumerable<XElement> NsElements(this IEnumerable<XElement> elements, XNamespace nameSpace,
            IEnumerable<string> elementNames)
        {
            return elements.Elements(elementNames.Select(name => nameSpace + name));
        }

        /// <summary>
        /// Gets only all elements that comply with the predicate, unless there are no compliant elements, then return all (other) elements
        /// </summary>
        /// <typeparam name="T">The input/output type</typeparam>
        /// <param name="src">The source sequence</param>
        /// <param name="preferencePredicate">The predicate</param>
        /// <returns>All elements for which the predicate is true, if there are none, all elements</returns>
        public static IEnumerable<T> Prefer<T>(this IEnumerable<T> src, Func<T, bool> preferencePredicate)
        {
            //TODO could be optimized by using an enumerator that stashes non hits and yields hits, then yields the stash upon completion of the source if no hits were found
            return src.Any(preferencePredicate) ? src.Where(preferencePredicate) : src;
        }

        public static IEnumerable<string> Values(this IEnumerable<XElement> elements)
        {
            return elements.Select(e => e.Value);
        }

        public static async Task<IEnumerable<string>> Values(this Task<IEnumerable<XElement>> elements)
        {
            return (await elements).Select(e => e.Value);
        }

        public static IEnumerable<XElement> NsElements(this IEnumerable<XElement> elements, string ns,
            IEnumerable<string> elementNames)
        {
            return elements.NsElements((XNamespace) ns, elementNames);
        }

        public static IEnumerable<XElement> NsElements(this IEnumerable<XElement> elements, XNamespace nameSpace,
            params string[] elementNames)
        {
            return elements.Elements(elementNames.Select(name => nameSpace + name));
        }

        public static IEnumerable<XElement> NsElements(this IEnumerable<XElement> elements, string ns,
            params string[] elementNames)
        {
            return elements.NsElements((XNamespace) ns, elementNames);
        }

        public static IEnumerable<XElement> Elements(this XElement element, IEnumerable<XName> elementXNames)
        {
            return new[] {element}.Elements(elementXNames);
        }

        public static IEnumerable<XElement> Elements(this IEnumerable<XElement> elements,
            IEnumerable<XName> elementXNames)
        {
            var iter = elements;
            return elementXNames.Aggregate(iter, (current, elementXName) => current.Elements(elementXName));
        }

        public static DateTimeOffset? TimeFromAttribute(this XElement elem, string attributeName)
        {
            return elem.Attributes(attributeName)
                .Select(a => a.Value)
                .Select(TryDateTimeOffset)
                .FirstOrDefault();
        }

        public static TimeSpan? MinutesFromAttribute(this XElement elem, string attributeName)
        {
            return elem.Attributes(attributeName)
                .Select(a => a.Value)
                .Select(TryMinutes)
                .FirstOrDefault();
        }

        public static IEnumerable<DateTimeOffset> AsDateTimeOffsets(this IEnumerable<XElement> elem)
        {
            return elem
                .Select(a => a.Value)
                .Select(v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture));
        }

        public static DateTimeOffset AsDateTimeOffsets(this XAttribute attr)
        {
            return DateTimeOffset.Parse(attr.Value);
        }

        private static TimeSpan? TryMinutes(string m)
        {
            return TryInt(m)?.Minutes();
        }

        private static int? TryInt(string intString)
        {
            int retVal;
            return !int.TryParse(intString, out retVal) ? (int?) null : retVal;
        }

        private static TimeSpan Minutes(this int min)
        {
            return TimeSpan.FromMinutes(min);
        }

        private static DateTimeOffset? TryDateTimeOffset(string s)
        {
            DateTimeOffset retVal;
            return !DateTimeOffset.TryParse(s, out retVal) ? (DateTimeOffset?) null : retVal;
        }

        /// <summary>
        /// Tests whether the element has the requested xml:lang language
        /// </summary>
        /// <param name="src"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public static bool? IsLanguage(this XElement src, string language)
        {
            var xmlLanguage = src.XmlLanguage();
            return xmlLanguage?.StartsWith(language, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns the (nested) xml:lang element value, or null
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static string XmlLanguage(this XElement src)
        {
            return src.Attributes(XNamespace.Xml + "lang").Select(e => e.Value).FirstOrDefault() ?? src.Parent?.XmlLanguage();
        }

        public static XElement Clone(this XElement src)
        {
            return src == null
                ? null
                : XElement.Parse(src.ToString(SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces));
        }

        public static XDocument Clone(this XDocument src)
        {
            return src == null
                ? null
                : XDocument.Parse(src.ToString(SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces));
        }

        public static XElement AsXElement(this byte[] xmlBytes)
        {
            if (xmlBytes == null)
                return null;
            using (var ms = new MemoryStream(xmlBytes, false))
                return XElement.Load(ms, LoadOptions.None);
        }
    }
}
