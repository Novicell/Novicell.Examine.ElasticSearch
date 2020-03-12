using System;
using System.Configuration;
using System.Linq;
using HttpWebAdapters;
using Lucene.Net.Documents;
using SolrNet;
using SolrNet.Impl;

namespace Novicell.Examine.Solr
{
    public class SolrConfig
    {
        public int DefaultPageSize { get; private set; } = 20;
        internal string SolrCoreUrl { get; private set; }
        internal string Username { get; private set; }
        internal string Password { get; private set; }
        internal string IndexName { get; private set; }
        public SolrConnection Connection;

        public string SolrCoreIndexUrl
        {
            get
            {
                var coreUrl = SolrCoreUrl.TrimEnd('/');
                return $"{coreUrl}/{IndexName}";
            }
        }

        public static SolrConfig GetConfig(string indexName)
        {
            if (string.IsNullOrWhiteSpace(indexName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(indexName));

            return new SolrConfig(indexName);
        }


        public SolrConfig(string indexName)
        {
            IndexName = indexName;
            if (ConfigurationManager.AppSettings.AllKeys.Contains($"examine:Solr.Url"))
            {
                SolrCoreUrl = ConfigurationManager.AppSettings[$"examine:Solr.Url"];
                Username = ConfigurationManager.AppSettings[$"examine:Solr.Username"];
                Password = ConfigurationManager.AppSettings[$"examine:Solr.Password"];
                if (ConfigurationManager.AppSettings.AllKeys.Contains($"examine:Solr.PageSize"))
                {
                    DefaultPageSize = Convert.ToInt32(ConfigurationManager.AppSettings[$"examine:Solr.PageSize"]);
                }
            }

            else
            {
                SolrCoreUrl = ConfigurationManager.AppSettings[$"examine:Solr[{indexName}].Url"];
                Username = ConfigurationManager.AppSettings[$"examine:Solr[{indexName}].Username"];
                Password = ConfigurationManager.AppSettings[$"examine:Solr[{indexName}].Password"];
                if (ConfigurationManager.AppSettings.AllKeys.Contains($"examine:Solr[{indexName}].PageSize"))
                {
                    DefaultPageSize =
                        Convert.ToInt32(ConfigurationManager.AppSettings[$"examine:Solr[{indexName}].PageSize"]);
                }
            }
            Connection=  new SolrConnection(SolrCoreIndexUrl);
            Connection.HttpWebRequestFactory = new BasicAuthHttpWebRequestFactory(Username, Password);
            Startup.Init<Document>(Connection);
        }
    }
}