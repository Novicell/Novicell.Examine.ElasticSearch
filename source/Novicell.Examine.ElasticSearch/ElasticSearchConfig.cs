using System;
using System.Configuration;
using ElasticsearchInside.Config;
using Nest;

namespace Novicell.Examine.ElasticSearch
{
    public class ElasticSearchConfig
    {
        public ConnectionSettings ConnectionConfiguration { get; }
        public static ElasticSearchConfig DebugConnectionConfiguration;
        public static ElasticsearchInside.Elasticsearch ElasticSearch;
        public static ElasticSearchConfig GetConfig(string indexName)
        {
          
            if (string.IsNullOrWhiteSpace(indexName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(indexName));
            if (ConfigurationManager.AppSettings["examine:ElasticSearch.Debug"] == "True")
            {
              
                return new ElasticSearchConfig(DebugConnectionConfiguration.ConnectionConfiguration);
            }
            return new ElasticSearchConfig(indexName);
            
        }
        public static ElasticSearchConfig GetConfig(string indexName,ConnectionSettings connectionConfiguration)
        {
            if (string.IsNullOrWhiteSpace(indexName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(indexName));
            if (connectionConfiguration == null) throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionConfiguration));
            return new ElasticSearchConfig(connectionConfiguration);
            
        }

        public ElasticSearchConfig(string indexName)
        {
            var connectionUrl = new Uri(ConfigurationManager.AppSettings[$"examine:ElasticSearch[{indexName}].Url"]);
            ConnectionConfiguration= new ConnectionSettings(connectionUrl);
        }
        public ElasticSearchConfig()
        {
            ElasticSearch = new ElasticsearchInside.Elasticsearch(
                    settings => settings
                        .EnableLogging()
                        .SetPort(9200)
                        .SetElasticsearchStartTimeout(180))
                .ReadySync();
            
                ////Arrange
                ConnectionConfiguration=new ConnectionSettings(ElasticSearch.Url);
            
         
        }
        public ElasticSearchConfig(ConnectionSettings connectionConfiguration)
        {
            ConnectionConfiguration = connectionConfiguration;
        }
        
    }

 
}