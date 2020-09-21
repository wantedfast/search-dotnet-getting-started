using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;


namespace DataIndexer
{
    class Program
    {
        private const string GeoNamesIndex = "geonames";
        private const string UsgsDataSource = "usgs-datasource";
        private const string UsgsIndexer = "usgs-indexer";

        private static SearchIndexClient indexClient;
        private static SearchClient searchClient;
        private static SearchIndexerClient indexerClient;

        // This Sample shows how to delete, create, upload documents and query an index
        static void Main(string[] args)
        {
            string searchServiceEndPoint = ConfigurationManager.AppSettings["SearchServiceEndPoint"];
            string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];

            // Create an HTTP reference to the catalog index
            indexClient = new SearchIndexClient(new Uri(searchServiceEndPoint), new AzureKeyCredential(apiKey));
            indexerClient = new SearchIndexerClient(new Uri(searchServiceEndPoint), new AzureKeyCredential(apiKey));
            searchClient = indexClient.GetSearchClient(GeoNamesIndex);

            Console.WriteLine("{0}", "Deleting index, data source, and indexer...\n");
            if (DeleteIndexingResources())
            {
                Console.WriteLine("{0}", "Creating index...\n");
                CreateIndex();
                Console.WriteLine("{0}", "Sync documents from Azure SQL...\n");
                SyncDataFromAzureSQL();
            }
            Console.WriteLine("{0}", "Complete.  Press any key to end application...\n");
            Console.ReadKey();
        }

        private static bool DeleteIndexingResources()
        {
            // Delete the index, data source, and indexer.
            try
            {
                indexClient.DeleteIndex(GeoNamesIndex);
                indexerClient.DeleteDataSourceConnection(UsgsDataSource);
                indexerClient.DeleteIndexer(UsgsIndexer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting indexing resources: {0}\r\n", ex.Message);
                Console.WriteLine("Did you remember to add your SearchServiceName and SearchServiceApiKey to the app.config?\r\n");
                return false;
            }

            return true;
        }

        private static void CreateIndex()
        {
            // Create the Azure Search index based on the included schema
            try
            {
                List<SearchField> searchFields = new List<SearchField>();
                searchFields.Add(new SearchField("FEATURE_ID", SearchFieldDataType.String) { IsKey = true, IsSearchable = false, IsFilterable = false, IsSortable = false, IsFacetable = false });
                searchFields.Add(new SearchField("FEATURE_NAME", SearchFieldDataType.String) { IsKey = false, IsSearchable = true, IsFilterable = true, IsSortable = true, IsFacetable = false });
                searchFields.Add(new SearchField("FEATURE_CLASS", SearchFieldDataType.String) { IsKey = false, IsSearchable = true, IsFilterable = true, IsSortable = true, IsFacetable = false });
                searchFields.Add(new SearchField("STATE_ALPHA", SearchFieldDataType.String) { IsKey = false, IsSearchable = true, IsFilterable = true, IsSortable = true, IsFacetable = false });
                searchFields.Add(new SearchField("STATE_NUMERIC", SearchFieldDataType.Int32) { IsKey = false, IsSearchable = false, IsFilterable = true, IsSortable = true, IsFacetable = true });
                searchFields.Add(new SearchField("COUNTY_NAME", SearchFieldDataType.String) { IsKey = false, IsSearchable = true, IsFilterable = true, IsSortable = true, IsFacetable = false });
                searchFields.Add(new SearchField("COUNTY_NUMERIC", SearchFieldDataType.Int32) { IsKey = false, IsSearchable = false, IsFilterable = true, IsSortable = true, IsFacetable = true });
                searchFields.Add(new SearchField("ELEV_IN_M", SearchFieldDataType.Int32) { IsKey = false, IsSearchable = false, IsFilterable = true, IsSortable = true, IsFacetable = true });
                searchFields.Add(new SearchField("ELEV_IN_FT", SearchFieldDataType.Int32) { IsKey = false, IsSearchable = false, IsFilterable = true, IsSortable = true, IsFacetable = true });
                searchFields.Add(new SearchField("MAP_NAME", SearchFieldDataType.String) { IsKey = false, IsSearchable = true, IsFilterable = true, IsSortable = true, IsFacetable = false });
                searchFields.Add(new SearchField("DESCRIPTION", SearchFieldDataType.String) { IsKey = false, IsSearchable = true, IsFilterable = false, IsSortable = false, IsFacetable = false });
                searchFields.Add(new SearchField("HISTORY", SearchFieldDataType.String) { IsKey = false, IsSearchable = true, IsFilterable = false, IsSortable = false, IsFacetable = false });
                searchFields.Add(new SearchField("DATE_CREATED", SearchFieldDataType.DateTimeOffset) { IsKey = false, IsSearchable = false, IsFilterable = true, IsSortable = true, IsFacetable = true });
                searchFields.Add(new SearchField("DATE_EDITED", SearchFieldDataType.DateTimeOffset) { IsKey = false, IsSearchable = false, IsFilterable = true, IsSortable = true, IsFacetable = true });

                var definition = new SearchIndex(GeoNamesIndex, searchFields);

                indexClient.CreateIndex(definition);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating index: {0}\r\n", ex.Message);
            }

        }

        private static void SyncDataFromAzureSQL()
        {
            // This will use the Azure Search Indexer to synchronize data from Azure SQL to Azure Search
            Console.WriteLine("{0}", "Creating Data Source...\n");
            var dataSource =
                new SearchIndexerDataSourceConnection(
                    UsgsDataSource,
                    SearchIndexerDataSourceType.AzureSql,
                    "",
                    new SearchIndexerDataContainer("GeoNamesRI"));
            try
            {
                indexerClient.CreateDataSourceConnection(dataSource);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating data source: {0}", ex.Message);
                return;
            }

            Console.WriteLine("{0}", "Creating Indexer and syncing data...\n");

            var indexer = new SearchIndexer(UsgsIndexer, dataSource.Name, GeoNamesIndex)
            {
                Description = "USGS data indexer",
            };

            try
            {
                indexerClient.CreateIndexer(indexer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating and running indexer: {0}", ex.Message);
                return;
            }

            bool running = true;
            Console.WriteLine("{0}", "Synchronization running...\n");
            while (running)
            {
                SearchIndexerStatus status = null;

                try
                {
                    status = indexerClient.GetIndexerStatus(indexer.Name);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error polling for indexer status: {0}", ex.Message);
                    return;
                }

                IndexerExecutionResult lastResult = status.LastResult;
                if (lastResult != null)
                {
                    switch (lastResult.Status)
                    {
                        case IndexerExecutionStatus.InProgress:
                            Console.WriteLine("{0}", "Synchronization running...\n");
                            Thread.Sleep(1000);
                            break;

                        case IndexerExecutionStatus.Success:
                            running = false;
                            Console.WriteLine("Synchronized {0} rows...\n", lastResult.ItemCount);
                            break;

                        default:
                            running = false;
                            Console.WriteLine("Synchronization failed: {0}\n", lastResult.ErrorMessage);
                            break;
                    }
                }
            }
        }
    }
}
