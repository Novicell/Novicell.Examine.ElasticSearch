using Umbraco.Core;
using Umbraco.Core.Composing;
using UmbracoExamine.PDF;

namespace Novicell.Examine.ElasticSearch.Umbraco.PDF
{
    [ComposeAfter(typeof (ExaminePdfComposer))]
    [RuntimeLevel]
    public class ElasticPdfIndexComposer
    {
        public void Compose(Composition composition)
        {
           
            composition.RegisterUnique<PdfIndexCreator, ElasticPdfIndexCreator>();
        }
    }
}