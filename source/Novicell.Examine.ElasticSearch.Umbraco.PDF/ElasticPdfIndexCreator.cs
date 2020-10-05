using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Examine;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using Umbraco.Core.Logging;
using UmbracoExamine.PDF;

namespace Novicell.Examine.ElasticSearch.Umbraco.PDF
{
    public class ElasticPdfIndexCreator : PdfIndexCreator
    {
        private string prefix = ConfigurationManager.AppSettings.AllKeys.Any(s => s == "examine:ElasticSearch.Prefix")
            ? ConfigurationManager.AppSettings["examine:ElasticSearch.Prefix"]
            : "";

        private IProfilingLogger ProfilingLogger { get; }

        public ElasticPdfIndexCreator(IProfilingLogger logger) : base(logger)
        {
            ProfilingLogger = logger;
        }

        public virtual IEnumerable<IIndex> Create() => (IEnumerable<IIndex>) new PdfElasticIndex[1]
        {
            new PdfElasticIndex("PDFIndex", ElasticSearchConfig.GetConfig("PDFIndex"), new FieldDefinitionCollection(
                new FieldDefinition[1]
                {
                    new FieldDefinition("fileTextContent", "fulltext")
                }), "standard", new PdfValueSetValidator(new int?()), false)
        };
    }
}