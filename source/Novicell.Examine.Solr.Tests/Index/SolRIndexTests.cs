using System.Collections.Generic;
using System.Diagnostics;
using Examine;
using NUnit.Framework;

namespace Novicell.Examine.Solr.Tests.Index
{
    /// <summary>
    /// Tests the standard indexing capabilities
    /// </summary>
    [TestFixture]
    public class SolRIndexTests
    {
        [Test]
        public void Can_Create_Index()
        {
       
                SolrConfig config = new SolrConfig("TestIndex");
                using (var indexer = new TestBaseIndex(config,  new FieldDefinitionCollection()))
                {
                    indexer.CreateIndex();
                 
                    Assert.AreEqual(true, indexer.IndexExists());
                }
            
        }

        /*
        [Test]
        public void Index_Exists()
        {
            using (var elasticsearch = new ElasticsearchInside.Elasticsearch(settings => settings
                .EnableLogging()
                .SetPort(9200)
                .SetElasticsearchStartTimeout(180)).ReadySync())
            {
                ElasticSearchConfig config = new ElasticSearchConfig(new ConnectionSettings(elasticsearch.Url));
                using (var indexer = new TestBaseIndex(config,  new FieldDefinitionCollection(new FieldDefinition("item2", "number"))))
                {
                    indexer.EnsureIndex(true);
                    indexer._client.Value.Indices.Refresh(Indices.Index(indexer.indexAlias));
                    Assert.IsTrue(indexer.IndexExists());
                }
            
            }
        }
        

        [Test]
        public void Can_Add_One_Document()
        {
            using (var elasticsearch = new ElasticsearchInside.Elasticsearch(settings => settings
                .EnableLogging()
                .SetPort(9200)
                .SetElasticsearchStartTimeout(180)).ReadySync())
            {
                ElasticSearchConfig config = new ElasticSearchConfig(new ConnectionSettings(elasticsearch.Url));
                using (var indexer = new TestBaseIndex(config,  new FieldDefinitionCollection()))
                {
                    indexer.IndexItem(new ValueSet(1.ToString(), "content",
                        new Dictionary<string, IEnumerable<object>>
                        {
                            {"item1", new List<object>(new[] {"value1"})},
                            {"item2", new List<object>(new[] {"value2"})}
                        }));

                    indexer._client.Value.Indices.Refresh(Indices.Index(indexer.indexAlias));
                    Assert.AreEqual(1, indexer.DocumentCount);
                }
            }
        }

        [Test]
        public void Can_Add_Same_Document_Twice_Without_Duplication()
        {
            using (var elasticsearch = new ElasticsearchInside.Elasticsearch(settings => settings
                .EnableLogging()
                .SetElasticsearchStartTimeout(180)).ReadySync())
            {
                ElasticSearchConfig config = new ElasticSearchConfig(new ConnectionSettings(elasticsearch.Url));
                using (var indexer = new TestBaseIndex(config,  new FieldDefinitionCollection()))
                {
                    var value = new ValueSet(1.ToString(), "content",
                        new Dictionary<string, IEnumerable<object>>
                        {
                            {"item1", new List<object>(new[] {"value1"})},
                            {"item2", new List<object>(new[] {"value2"})}
                        });

                    indexer.IndexItem(value);
                    indexer.IndexItem(value);

                    indexer._client.Value.Indices.Refresh(Indices.Index(indexer.indexAlias));
                    Assert.AreEqual(1, indexer.DocumentCount);
                }
            }
        }

        [Test]
        public void Can_Add_Multiple_Docs()
        {
            using (var elasticsearch = new ElasticsearchInside.Elasticsearch(settings => settings
                .EnableLogging()

                .SetPort(9200)
                .SetElasticsearchStartTimeout(180)).ReadySync())
            {
                ElasticSearchConfig config = new ElasticSearchConfig(new ConnectionSettings(elasticsearch.Url));
                using (var indexer = new TestBaseIndex(config,  new FieldDefinitionCollection()))
                {
                    for (var i = 0; i < 10; i++)
                    {
                        indexer.IndexItem(new ValueSet(i.ToString(), "content",
                            new Dictionary<string, IEnumerable<object>>
                            {
                                {"item1", new List<object>(new[] {"value1"})},
                                {"item2", new List<object>(new[] {"value2"})}
                            }));
                    }
                    indexer._client.Value.Indices.Refresh(Indices.Index(indexer.indexAlias));
                    Assert.AreEqual(10, indexer.DocumentCount);
                }
            }
        }

        [Test]
        public void Can_Delete()
        {
            using (var elasticsearch = new ElasticsearchInside.Elasticsearch(settings => settings
                .EnableLogging()

                .SetPort(9200)
                .SetElasticsearchStartTimeout(180)).ReadySync())
            {
                ElasticSearchConfig config = new ElasticSearchConfig(new ConnectionSettings(elasticsearch.Url));
                using (var indexer = new TestBaseIndex(config,  new FieldDefinitionCollection()))
                {
                    for (var i = 0; i < 10; i++)
                    {
                        indexer.IndexItem(new ValueSet(i.ToString(), "content",
                            new Dictionary<string, IEnumerable<object>>
                            {
                                {"item1", new List<object>(new[] {"value1"})},
                                {"item2", new List<object>(new[] {"value2"})}
                            }));
                    }

                    indexer.DeleteFromIndex("9");

                    indexer._client.Value.Indices.Refresh(Indices.Index(indexer.indexAlias));
                    Assert.AreEqual(9, indexer.DocumentCount);
                }
            }
        }

/*
        [Test]
        public void Can_Add_Doc_With_Fields()
        {
            using (var elasticsearch = new ElasticsearchInside.Elasticsearch(settings => settings
                .EnableLogging()

                .SetElasticsearchStartTimeout(180)).ReadySync())
            {
                ElasticSearchConfig config = new ElasticSearchConfig(new ConnectionSettings(elasticsearch.Url));
                using (var indexer = new TestBaseIndex(config,  new FieldDefinitionCollection())){
                    indexer.IndexItem(new ValueSet(1.ToString(), "content", "test",
                        new Dictionary<string, IEnumerable<object>>
                        {
                            {"item1", new List<object>(new[] {"value1"})},
                            {"item2", new List<object>(new[] {"value2"})}
                        }));


                    using (var s = (ElasticSearchSearcher) indexer.GetSearcher())
                    {
                        var fields = s.AllFields;
                        Assert.IsNotNull(fields.SingleOrDefault(x => x == "item1"));
                        Assert.IsNotNull(fields.SingleOrDefault(x => x == "item2"));
                        Assert.IsNotNull(fields.SingleOrDefault(x => x == LuceneIndex.ItemTypeFieldName));
                        Assert.IsNotNull(fields.SingleOrDefault(x => x == LuceneIndex.ItemIdFieldName));
                        Assert.IsNotNull(fields.SingleOrDefault(x => x == LuceneIndex.CategoryFieldName));

                        /*Assert.AreEqual("value1", fields.Single(x => x == "item1").StringValue);
                        Assert.AreEqual("value2", fields.Single(x => x == "item2").StringValue);
                        Assert.AreEqual("test",
                            fields.Single(x => x.Name == LuceneIndex.ItemTypeFieldName).StringValue);
                        Assert.AreEqual("1", fields.Single(x => x.Name == LuceneIndex.ItemIdFieldName).StringValue);
                        Assert.AreEqual("content",
                            fields.Single(x => x.Name == LuceneIndex.CategoryFieldName).StringValue);
                            
                    }
                }
            }
        }

        [Test]
        public void Can_Add_Doc_With_Easy_Fields()
        {
            using (var elasticsearch = new ElasticsearchInside.Elasticsearch(settings => settings
                .EnableLogging()

                .SetElasticsearchStartTimeout(180)).ReadySync())
            {
                ElasticSearchConfig config = new ElasticSearchConfig(new ConnectionSettings(elasticsearch.Url));
                using (var indexer = new TestBaseIndex(config, new FieldDefinitionCollection()))
                {
                    indexer.IndexItem(ValueSet.FromObject(1.ToString(), "content",
                        new {item1 = "value1", item2 = "value2"}));

                    using (var s = (ElasticSearchSearcher) indexer.GetSearcher())
                    {
                        var fields = s.AllFields;
                        Assert.IsNotNull(fields.SingleOrDefault(x => x == "item1"));
                        Assert.IsNotNull(fields.SingleOrDefault(x => x == "item2"));
                        Assert.IsNotNull(fields.SingleOrDefault(x => x == LuceneIndex.ItemTypeFieldName));
                        Assert.IsNotNull(fields.SingleOrDefault(x => x == LuceneIndex.ItemIdFieldName));
                        Assert.IsNotNull(fields.SingleOrDefault(x => x == LuceneIndex.CategoryFieldName));
                        /*
                        Assert.AreEqual("value1", fields.Single(x => x.Name == "item1").StringValue);
                        Assert.AreEqual("value2", fields.Single(x => x.Name == "item2").StringValue);
                        
                    }
                }
            }
        }

        [Test]
        public void Can_Have_Multiple_Values_In_Fields()
        {
            using (var elasticsearch = new ElasticsearchInside.Elasticsearch(settings => settings
                .EnableLogging()

                .SetElasticsearchStartTimeout(180)).ReadySync())
            {
                ElasticSearchConfig config = new ElasticSearchConfig(new ConnectionSettings(elasticsearch.Url));
                using (var indexer = new TestBaseIndex(config, new FieldDefinitionCollection()))
                {
                    indexer.IndexItem(new ValueSet(1.ToString(), "content",
                        new Dictionary<string, IEnumerable<object>>
                        {
                            {
                                "item1", new List<object> {"subval1", "subval2"}
                            },
                            {
                                "item2", new List<object> {"subval1", "subval2", "subval3"}
                            }
                        }));

                    using (var s = (ElasticSearchSearcher0) indexer.GetSearcher())
                    {
                        var luceneSearcher = s.GetLuceneSearcher();
                        var fields = luceneSearcher.Doc(0).GetFields().ToArray();
                        Assert.AreEqual(2, fields.Count(x => x.Name == "item1"));
                        Assert.AreEqual(3, fields.Count(x => x.Name == "item2"));

                        Assert.AreEqual("subval1", fields.Where(x => x.Name == "item1").ElementAt(0).StringValue);
                        Assert.AreEqual("subval2", fields.Where(x => x.Name == "item1").ElementAt(1).StringValue);

                        Assert.AreEqual("subval1", fields.Where(x => x.Name == "item2").ElementAt(0).StringValue);
                        Assert.AreEqual("subval2", fields.Where(x => x.Name == "item2").ElementAt(1).StringValue);
                        Assert.AreEqual("subval3", fields.Where(x => x.Name == "item2").ElementAt(2).StringValue);
                    }
                }
            }
        }

        [Test]
        public void Can_Update_Document()
        {
            using (var elasticsearch = new ElasticsearchInside.Elasticsearch(settings => settings
                .EnableLogging()

                .SetElasticsearchStartTimeout(180)).ReadySync())
            {
                ElasticSearchConfig config = new ElasticSearchConfig(new ConnectionSettings(elasticsearch.Url));
                using (var indexer = new TestBaseIndex(config, new FieldDefinitionCollection()))
                {
                    indexer.IndexItem(ValueSet.FromObject(1.ToString(), "content",
                        new {item1 = "value1", item2 = "value2"}));

                    indexer.IndexItem(ValueSet.FromObject(1.ToString(), "content",
                        new {item1 = "value3", item2 = "value4"}));

                    using (var s = (LuceneSearcher) indexer.GetSearcher())
                    {
                        var luceneSearcher = s.GetLuceneSearcher();
                        var fields = luceneSearcher.Doc(luceneSearcher.MaxDoc - 1).GetFields().ToArray();
                        Assert.IsNotNull(fields.SingleOrDefault(x => x.Name == "item1"));
                        Assert.IsNotNull(fields.SingleOrDefault(x => x.Name == "item2"));
                        Assert.AreEqual("value3", fields.Single(x => x.Name == "item1").StringValue);
                        Assert.AreEqual("value4", fields.Single(x => x.Name == "item2").StringValue);
                    }
                }
            }
        }

        [Test]
        public void Number_Field()
        {
            using (var elasticsearch = new ElasticsearchInside.Elasticsearch(settings => settings
                .EnableLogging()

                .SetElasticsearchStartTimeout(180)).ReadySync())
            {
                ElasticSearchConfig config = new ElasticSearchConfig(new ConnectionSettings(elasticsearch.Url));
                using (var indexer = new TestBaseIndex(config, new FieldDefinitionCollection()))
                {
                    indexer.IndexItem(new ValueSet(1.ToString(), "content",
                        new Dictionary<string, IEnumerable<object>>
                        {
                            {"item1", new List<object>(new[] {"value1"})},
                            {"item2", new List<object>(new object[] {123456})}
                        }));

                    using (var s = (LuceneSearcher) indexer.GetSearcher())
                    {
                        var luceneSearcher = s.GetLuceneSearcher();
                        var fields = luceneSearcher.Doc(luceneSearcher.MaxDoc - 1).GetFields().ToArray();

                        var valType = indexer.FieldValueTypeCollection.GetValueType("item2");
                        Assert.AreEqual(typeof(Int32Type), valType.GetType());
                        Assert.IsNotNull(fields.SingleOrDefault(x => x.Name == "item2"));
                    }
                }
            }
        }
       /*
        /// <summary>
        /// Ensures that the cancellation is successful when creating a new index while it's currently indexing
        /// </summary>
        [Test]
        public void Can_Overwrite_Index_During_Indexing_Operation()
        {
            using (var d = new RandomIdRAMDirectory())
            using (var writer = new IndexWriter(d, new CultureInvariantStandardAnalyzer(Version.LUCENE_30),
                IndexWriter.MaxFieldLength.LIMITED))
            using (var customIndexer = new TestIndex(writer))
            using (var customSearcher = (LuceneSearcher) customIndexer.GetSearcher())
            {
                var waitHandle = new ManualResetEvent(false);

                void OperationComplete(object sender, IndexOperationEventArgs e)
                {
                    //signal that we are done
                    waitHandle.Set();
                }

                //add the handler for optimized since we know it will be optimized last based on the commit count
                customIndexer.IndexOperationComplete += OperationComplete;

                //remove the normal indexing error handler
                customIndexer.IndexingError -= IndexInitializer.IndexingError;

                //run in async mode
                customIndexer.RunAsync = true;

                //get a node from the data repo
                var node = _contentService.GetPublishedContentByXPath("//*[string-length(@id)>0 and number(@id)>0]")
                    .Root
                    .Elements()
                    .First();

                //get the id for th node we're re-indexing.
                var id = (int) node.Attribute("id");

                //spawn a bunch of threads to perform some reading
                var tasks = new List<Task>();

                //reindex the same node a bunch of times - then while this is running we'll overwrite below
                for (var i = 0; i < 1000; i++)
                {
                    var indexer = customIndexer;
                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        //get next id and put it to the back of the list
                        int docId = i;
                        var cloned = new XElement(node);
                        Debug.WriteLine("Indexing {0}", docId);
                        indexer.IndexItem(cloned.ConvertToValueSet(IndexTypes.Content));
                    }, TaskCreationOptions.LongRunning));
                }

                Thread.Sleep(100);

                //overwrite!
                customIndexer.EnsureIndex(true);

                try
                {
                    Task.WaitAll(tasks.ToArray());
                }
                catch (AggregateException e)
                {
                    var sb = new StringBuilder();
                    sb.Append(e.Message + ": ");
                    foreach (var v in e.InnerExceptions)
                    {
                        sb.Append(v.Message + "; ");
                    }

                    Assert.Fail(sb.ToString());
                }

                //reset the async mode and remove event handler
                customIndexer.IndexingError += IndexInitializer.IndexingError;
                customIndexer.RunAsync = false;

                //wait until we are done
                waitHandle.WaitOne();

                writer.WaitForMerges();

                //ensure no data since it's a new index
                var results = customSearcher.CreateQuery()
                    .Field("nodeName", (IExamineValue) new ExamineValue(Examineness.Explicit, "Home")).Execute();

                //should be less than the total inserted because we overwrote it in the middle of processing
                Debug.WriteLine("TOTAL RESULTS: " + results.TotalItemCount);
                Assert.Less(results.Count(), 1000);
            }
        }

        /// <summary>
        /// This will create a new index queue item for the same ID multiple times to ensure that the 
        /// index does not end up with duplicate entries.
        /// </summary>
        [Test]
        public void Index_Ensure_No_Duplicates_In_Async()
        {
            using (var d = new RandomIdRAMDirectory())
            using (var writer = new IndexWriter(d, new CultureInvariantStandardAnalyzer(Version.LUCENE_30),
                IndexWriter.MaxFieldLength.LIMITED))
            using (var customIndexer = new TestIndex(writer))
                //using (var customSearcher = (LuceneSearcher)customIndexer.GetSearcher())
            {
                var waitHandle = new ManualResetEvent(false);

                void OperationComplete(object sender, IndexOperationEventArgs e)
                {
                    //signal that we are done
                    waitHandle.Set();
                }

                //add the handler for optimized since we know it will be optimized last based on the commit count
                customIndexer.IndexOperationComplete += OperationComplete;

                //remove the normal indexing error handler
                customIndexer.IndexingError -= IndexInitializer.IndexingError;

                //run in async mode
                customIndexer.RunAsync = true;

                //get a node from the data repo
                var idQueue = new ConcurrentQueue<int>(Enumerable.Range(1, 3));
                var node = _contentService.GetPublishedContentByXPath("//*[string-length(@id)>0 and number(@id)>0]")
                    .Root
                    .Elements()
                    .First();

                //reindex the same nodes a bunch of times
                for (var i = 0; i < idQueue.Count * 20; i++)
                {
                    //get next id and put it to the back of the list
                    int docId;
                    if (idQueue.TryDequeue(out docId))
                    {
                        idQueue.Enqueue(docId);

                        var cloned = new XElement(node);
                        cloned.Attribute("id").Value = docId.ToString(CultureInfo.InvariantCulture);
                        Debug.WriteLine("Indexing {0}", docId);
                        customIndexer.IndexItems(new[] {cloned.ConvertToValueSet(IndexTypes.Content)});
                        Thread.Sleep(100);
                    }
                }

                //reset the async mode and remove event handler
                customIndexer.IndexingError += IndexInitializer.IndexingError;
                customIndexer.RunAsync = false;

                //wait until we are done
                waitHandle.WaitOne();

                writer.WaitForMerges();

                //ensure no duplicates

                var customSearcher = (LuceneSearcher) customIndexer.GetSearcher();
                var results = customSearcher.CreateQuery()
                    .Field("nodeName", (IExamineValue) new ExamineValue(Examineness.Explicit, "Home")).Execute();
                Assert.AreEqual(3, results.Count());
            }
        }

        [Test]
        public void Index_Read_And_Write_Ensure_No_Errors_In_Async()
        {
            using (var d = new RandomIdRAMDirectory())
            using (var writer = new IndexWriter(d, new CultureInvariantStandardAnalyzer(Version.LUCENE_30),
                IndexWriter.MaxFieldLength.LIMITED))
            using (var customIndexer = new TestIndex(writer))
            using (var customSearcher = (LuceneSearcher) customIndexer.GetSearcher())
            {
                var waitHandle = new ManualResetEvent(false);

                void OperationComplete(object sender, IndexOperationEventArgs e)
                {
                    //signal that we are done
                    waitHandle.Set();
                }

                //add the handler for optimized since we know it will be optimized last based on the commit count
                customIndexer.IndexOperationComplete += OperationComplete;

                //remove the normal indexing error handler
                customIndexer.IndexingError -= IndexInitializer.IndexingError;

                //run in async mode
                customIndexer.RunAsync = false;

                //get a node from the data repo
                var node = _contentService.GetPublishedContentByXPath("//*[string-length(@id)>0 and number(@id)>0]")
                    .Root
                    .Elements()
                    .First();

                var idQueue = new ConcurrentQueue<int>(Enumerable.Range(1, 10));
                var searchThreadCount = 500;
                var indexThreadCount = 10;
                var searchCount = 10700;
                var indexCount = 100;
                var searchCountPerThread = Convert.ToInt32(searchCount / searchThreadCount);
                var indexCountPerThread = Convert.ToInt32(indexCount / indexThreadCount);

                //spawn a bunch of threads to perform some reading                              
                var tasks = new List<Task>();

                Action<ISearcher> doSearch = (s) =>
                {
                    try
                    {
                        for (var counter = 0; counter < searchCountPerThread; counter++)
                        {
                            //get next id and put it to the back of the list
                            int docId;
                            if (idQueue.TryDequeue(out docId))
                            {
                                idQueue.Enqueue(docId);
                                var r = s.CreateQuery().Id(docId.ToString()).Execute();
                                Debug.WriteLine("searching thread: {0}, id: {1}, found: {2}",
                                    Thread.CurrentThread.ManagedThreadId, docId, r.Count());
                                Thread.Sleep(50);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ERROR!! {0}", ex);
                        throw;
                    }
                };

                Action<IIndex> doIndex = (ind) =>
                {
                    try
                    {
                        //reindex the same node a bunch of times
                        for (var i = 0; i < indexCountPerThread; i++)
                        {
                            //get next id and put it to the back of the list
                            int docId;
                            if (idQueue.TryDequeue(out docId))
                            {
                                idQueue.Enqueue(docId);

                                var cloned = new XElement(node);
                                cloned.Attribute("id").Value = docId.ToString(CultureInfo.InvariantCulture);
                                Debug.WriteLine("Indexing {0}", docId);
                                ind.IndexItems(new[] {cloned.ConvertToValueSet(IndexTypes.Content)});
                                Thread.Sleep(100);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ERROR!! {0}", ex);
                        throw;
                    }
                };

                //indexing threads
                for (var i = 0; i < indexThreadCount; i++)
                {
                    var indexer = customIndexer;
                    tasks.Add(Task.Factory.StartNew(() => doIndex(indexer), TaskCreationOptions.LongRunning));
                }

                //searching threads
                for (var i = 0; i < searchThreadCount; i++)
                {
                    var searcher = customSearcher;
                    tasks.Add(Task.Factory.StartNew(() => doSearch(searcher), TaskCreationOptions.LongRunning));
                }

                try
                {
                    Task.WaitAll(tasks.ToArray());
                }
                catch (AggregateException e)
                {
                    var sb = new StringBuilder();
                    sb.Append(e.Message + ": ");
                    foreach (var v in e.InnerExceptions)
                    {
                        sb.Append(v.Message + "; ");
                    }

                    Assert.Fail(sb.ToString());
                }

                //reset the async mode and remove event handler
                customIndexer.IndexingError += IndexInitializer.IndexingError;
                customIndexer.RunAsync = false;

                //wait until we are done
                waitHandle.WaitOne();

                writer.WaitForMerges();

                var results = customSearcher.CreateQuery()
                    .Field("nodeName", (IExamineValue) new ExamineValue(Examineness.Explicit, "Home")).Execute();
                Assert.AreEqual(10, results.Count());
            }
        }
*/
 
      //  private readonly TestContentService _contentService = new TestContentService();
    }
   
}