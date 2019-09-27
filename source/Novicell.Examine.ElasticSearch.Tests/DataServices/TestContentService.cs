using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Novicell.Examine.ElasticSearch.Tests.DataServices
{

    /// <summary>
    /// A mock data service used to return content from the XML data file created with CWS
    /// </summary>
    public class TestContentService 
    {
        public TestContentService()
        {
            var xmlFile = new DirectoryInfo(TestHelper.AssemblyDirectory).GetDirectories("App_Data")
                .Single()
                .GetFiles("umbraco.config")
                .Single();

            _xDoc = XDocument.Load(xmlFile.FullName);
        }

        /// <summary>
        /// Return the XDocument containing the xml from the umbraco.config xml file
        /// </summary>
        /// <param name="xpath"></param>
        /// <returns></returns>
        public XDocument GetPublishedContentByXPath(string xpath)
        {
            var xdoc = XDocument.Parse("<content></content>");
            xdoc.Root.Add(_xDoc.XPathSelectElements(xpath));

            return xdoc;
        }

        private readonly XDocument _xDoc;

        







    }
}
