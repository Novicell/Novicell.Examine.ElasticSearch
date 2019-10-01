using System.ComponentModel;
using Examine;
using Novicell.Examine.ElasticSearch.Model;

namespace Novicell.Examine.ElasticSearch.EventArgs
{
    public class DocumentWritingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Lucene.NET Document, including all previously added fields
        /// </summary>        
        public Document Document { get; }

        /// <summary>
        /// Fields of the indexer
        /// </summary>
        public ValueSet ValueSet { get; }
        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="valueSet"></param>
        /// <param name="d"></param>
        public DocumentWritingEventArgs(ValueSet valueSet, Document d)
        {
            this.Document = d;
            this.ValueSet = valueSet;
        }
        
    }
}