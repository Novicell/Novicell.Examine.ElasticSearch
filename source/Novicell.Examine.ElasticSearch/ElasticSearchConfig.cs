using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Elasticsearch.Net;
using Nest;
using Newtonsoft.Json;

namespace Novicell.Examine.ElasticSearch
{
    public class ElasticSearchConfig
    {
        public static Dictionary<string, ConnectionSettings> ConnectionConfiguration =
            new Dictionary<string, ConnectionSettings>();


        public static ConnectionSettings GetConnectionString(string indexName)
        {
            
            if (ConnectionConfiguration.ContainsKey(indexName))
            {
                return ConnectionConfiguration[indexName];
            }

            GetConfig(indexName);
            return ConnectionConfiguration[indexName];
        }

        public static ElasticSearchConfig GetConfig(string indexName)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(indexName));
            if (ConfigurationManager.AppSettings.AllKeys.Any(s => s == "examine:ElasticSearch.Debug") &&
                Convert.ToBoolean(ConfigurationManager.AppSettings["examine:ElasticSearch.Debug"]))
            {
                return new ElasticSearchConfig(indexName.ToLower(), new ConnectionSettings());
            }

            return new ElasticSearchConfig(indexName.ToLower());
        }

        public static ElasticSearchConfig GetConfig(string indexName, ConnectionSettings connectionConfiguration)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(indexName));
            if (connectionConfiguration == null)
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionConfiguration));
            return new ElasticSearchConfig(indexName, connectionConfiguration);
        }

        public ElasticSearchConfig(string indexName)
        {
            if (ConnectionConfiguration.ContainsKey(indexName))
            {
                return;
            }

            ConnectionSettings connection;
            CloudConnectionPool pool;
            string id;
            switch (ConfigurationManager.AppSettings[$"examine:ElasticSearch:{indexName}.Authentication"])
            {
                case "cloud":
                    var user = ConfigurationManager.AppSettings[$"examine:ElasticSearch:{indexName}.UserName"];
                    id = ConfigurationManager.AppSettings[$"examine:ElasticSearch:{indexName}.CloudId"];
                    var password = ConfigurationManager.AppSettings[$"examine:ElasticSearch:{indexName}.Password"];

             
                    var basicAuthentication = new BasicAuthenticationCredentials(user
                            , password)
                        ;
                    pool = new CloudConnectionPool(id, basicAuthentication);
                    connection = new ConnectionSettings(pool);

                    break;
                case "CloudApi":
                    id = ConfigurationManager.AppSettings[$"examine:ElasticSearch:{indexName}.CloudId"];
                    var token = ConfigurationManager.AppSettings[$"examine:ElasticSearch:{indexName}.ApiKey"];
                    var auth = new ApiKeyAuthenticationCredentials(token);
                    pool = new CloudConnectionPool(id, auth);
                    connection = new ConnectionSettings(pool);
                    break;
                default:
                    connection = new ConnectionSettings();
                    break;
            }

            ConnectionConfiguration.Add(indexName, connection);
        }

        private ElasticSearchConfig(string indexName, ConnectionSettings connectionConfiguration)
        {
            if (ConnectionConfiguration.ContainsKey(indexName))
            {
                return;
            }

            ConnectionConfiguration.Add(indexName, connectionConfiguration);
        }
    }

    public class DebugData
    {
        [JsonProperty("User")] public string User { get; set; }
        [JsonProperty("Id")] public string Id { get; set; }
        [JsonProperty("IndexName")] public string IndexName { get; set; }
        [JsonProperty("Password")] public string Password { get; set; }
    }

}