using System;
using System.Configuration;
using System.Linq;
using Elasticsearch.Net;
using Nest;

namespace Novicell.Examine.ElasticSearch
{
    public class ElasticSearchConfig
    {
        public ConnectionSettings ConnectionConfiguration { get; } = new ConnectionSettings();
        public static ElasticSearchConfig DebugConnectionConfiguration;

    
        public static ElasticSearchConfig GetConfig(string indexName)
        {
          
            if (string.IsNullOrWhiteSpace(indexName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(indexName));
            if (ConfigurationManager.AppSettings.AllKeys.Any(s=>s=="examine:ElasticSearch.Debug") &&  Convert.ToBoolean(ConfigurationManager.AppSettings["examine:ElasticSearch.Debug"] ))
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
            ConnectionSettings connection;
            CloudConnectionPool pool;
            string id;
            switch (ConfigurationManager.AppSettings[$"examine:ElasticSearch[{indexName}].Authentication"])
            {
                case "cloud":
                 id = ConfigurationManager.AppSettings[$"examine:ElasticSearch[{indexName}].CloudId"];
                    var basicAuthentication = new BasicAuthenticationCredentials(
                        ConfigurationManager.AppSettings[$"examine:ElasticSearch[{indexName}].UserName"],
                        ConfigurationManager.AppSettings[$"examine:ElasticSearch[{indexName}].Password"]);
                 pool = new CloudConnectionPool(id,basicAuthentication);
                 connection =   new ConnectionSettings(pool);
                 
                break;
                case "CloudApi":
                    id = ConfigurationManager.AppSettings[$"examine:ElasticSearch[{indexName}].CloudId"];
                    var token = ConfigurationManager.AppSettings[$"examine:ElasticSearch[{indexName}].ApiKey"];
                    var auth = new ApiKeyAuthenticationCredentials(token);
                    pool = new CloudConnectionPool(id, auth);
                    connection =   new ConnectionSettings(pool);
                    break;
                default:
                    connection = new ConnectionSettings();
                    break;
            }
            ConnectionConfiguration= connection;
        }
        public ElasticSearchConfig(ConnectionSettings connectionConfiguration)
        {
            ConnectionConfiguration = connectionConfiguration;
        }
        
    }

 
}