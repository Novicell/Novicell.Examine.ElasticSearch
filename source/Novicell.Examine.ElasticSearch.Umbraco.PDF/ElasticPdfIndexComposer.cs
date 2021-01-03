using Novicell.Examine.ElasticSearch.Umbraco.PDF.IndexPopulators;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Examine;
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
            composition.Register<IIndexPopulator, ElasticFormsIndexPopulator>(Lifetime.Singleton);
            composition.Register<ElasticFormsIndexPopulator>(Lifetime.Singleton);
        }
    }
}