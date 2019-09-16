# Novicell.Examine.ElasticSearch
[![Build status](https://ci.appveyor.com/api/projects/status/qrkvmx8jnxg8n2up/branch/master?svg=true)](https://ci.appveyor.com/project/bielu/novicell-examine-elasticsearch/branch/master)
[![Build status](https://img.shields.io/nuget/vpre/Novicell.Examine.ElasticSearch)](https://www.nuget.org/packages/Novicell.Examine.ElasticSearch/)

# Introduction:
Umbraco comes with Examine.  This is an abstraction around Lucene.net and it makes indexing and searching with Lucene alot easier.  However there are limitations:



Umbraco 7 uses Lucene.net 2.9.4 and Umbraco 8 uses Lucene.net 3.0.1 both released before 2012 
Examine does not have ability to support replication for resiliance. Elastic does.
Examine indexes more specifically these older lucene.net indexes cannot currently be put into blob storage (there is unsupported azure provider).  This can cause issues when trying to load balance sites as you have to have separate index for each site
You need zero downtime during rebuild. Examine when rebuilding will delete the index then rebuild, this means there is a catch up time during which documents will be missing from the index. Elastic has concept of aliases to allow zero downtime on rebuilds.
Do you require additional media indexing eg word / powerpoint / excel? Currenty there is for v8 pdf indexer this uses pdfsharp, pdfsharp cannot handle all encoding and sometimes you can end up with junk in your index.
The lucene version of current examine cannot handle CJK (chinese, japanese and korean) languages very well.  There is better multilingual support in lucene >4.8 (35+analysers including morphological analysis)
Want latest version of Lucene.  The java version of lucene is currently at 8.2.0 and latest elastic uses lucene > 7
Keeping in mind the above, we needed something to index into elastic using examine.  Hence the creation of this provider.



# Basic Information:
Dependencies:
Umbraco 8.1.2

Elasticsearch 6.7

Nest 6.7

Demo solution:
Umbraco Site:
https://umbracoelasticsearchdemo.novicell.london/

user: demo@novicell.co.uk
pass: Au3%!vRJ$I

Kibana:
https://kibana.novicell.london/app/kibana#/dev_tools/console?_g=()


# Installation:
Prerequiments:
Installed Umbraco v8 in version

Instance of ElasticSearch

# Steps:
Step 1: Install Nuget Package
(Screenshots here)

Step 2: Add ElasticSearch configuration to web config
(Screenshots here)

Step 3: Reindex Umbraco Indexes
(Screenshots here)

Step 4: Be Happy with using Elasticsearch instead of LuceneIndexes (smile) 
(Screenshots here)

# Configuration:
```<add key="examine:ElasticSearch[InternalIndex].Url" value="http://localhost:9200" />``` //Used only when Debug is false

```<add key="examine:ElasticSearch[ExternalIndex].Url" value="http://localhost:9200" />``` //Used only when Debug is false

```<add key="examine:ElasticSearch[MemberIndex].Url" value="http://localhost:9200" />``` //Used only when Debug is false

```<add key="examine:ElasticSearch.Debug" value="True" />``` //Determine if package should use Embed ElasticSearch or no
