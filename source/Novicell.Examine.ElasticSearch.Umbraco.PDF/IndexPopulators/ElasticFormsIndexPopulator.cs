using System;
using System.Collections.Generic;
using System.Linq;
using Examine;
using Umbraco.Core.Services;
using Umbraco.Examine;
using UmbracoExamine.PDF;

namespace Novicell.Examine.ElasticSearch.Umbraco.PDF.IndexPopulators
{
    public class ElasticFormsIndexPopulator : PdfIndexPopulator
    {
        public ElasticFormsIndexPopulator(IMediaService mediaService, IPdfIndexValueSetBuilder mediaValueSetBuilder, IExamineManager examineManager) : base(mediaService, mediaValueSetBuilder, examineManager)
        {
            this.RegisterIndex("pdfindex");
        }

        public ElasticFormsIndexPopulator(int? parentId, IMediaService mediaService, IPdfIndexValueSetBuilder mediaValueSetBuilder, IExamineManager examineManager) : base(parentId, mediaService, mediaValueSetBuilder, examineManager)
        {
            this.RegisterIndex("pdfindex");
        }
    }
}