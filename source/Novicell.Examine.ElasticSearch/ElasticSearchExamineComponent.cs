using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Examine;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Changes;
using Umbraco.Core.Sync;
using Umbraco.Examine;
using Umbraco.Web.Cache;
using Umbraco.Web.Search;
using Novicell.Examine.ElasticSearch.ContentTypes;
namespace Novicell.Examine.ElasticSearch
{
    public class ElasticSearchExamineComponent : IComponent, Umbraco.Core.Composing.IComponent
    {
        private readonly IExamineManager _examineManager;
        private readonly IUmbracoIndexesCreator _indexCreator;
        private readonly IProfilingLogger _logger;

        // the default enlist priority is 100
        // enlist with a lower priority to ensure that anything "default" runs after us
        // but greater that SafeXmlReaderWriter priority which is 60
        private const int EnlistPriority = 80;
        public ElasticSearchExamineComponent(IExamineManager examineManager,
            IUmbracoIndexesCreator indexCreator,
            IProfilingLogger profilingLogger
        )
        {
            _examineManager = examineManager;
            _indexCreator = indexCreator;
            _logger = profilingLogger;
        }


        public void Initialize()
        {
       


        }


        public void Terminate()
        {
        }

        public void Dispose()
        {
            Disposed?.Invoke(this, new EventArgs());
        }

        

        public ISite Site { get; set; }
        public event EventHandler Disposed;
    }
}