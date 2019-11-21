using Nest;
using Novicell.Examine.ElasticSearch.Model;

namespace Novicell.Examine.ElasticSearch.EventArgs
{
    public class MappingOperationEventArgs
    {
        public PropertiesDescriptor<Document> descriptor;

        public MappingOperationEventArgs(PropertiesDescriptor<Document> descriptor)
        {
            this.descriptor = descriptor;
        }
    }
    
}