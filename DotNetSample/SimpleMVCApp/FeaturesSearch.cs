using System;
using System.Configuration;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;

namespace SimpleSearchMVCApp
{
    public class FeaturesSearch
    {
        private static SearchIndexClient indexClient;
        private static SearchClient searchClient;

        public static string errorMessage;

        static FeaturesSearch()
        {
            try
            {
                string searchServiceEndPoint = ConfigurationManager.AppSettings["SearchServiceEndPoint"];
                string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];

                // Create an HTTP reference to the catalog index
                indexClient = new SearchIndexClient(new Uri(searchServiceEndPoint), new AzureKeyCredential(apiKey));
                searchClient = indexClient.GetSearchClient("geonames");
            }
            catch (Exception e)
            {
                errorMessage = e.Message.ToString();
            }
        }

        public SearchResults<SearchDocument> Search(string searchText)
        {
            // Execute search based on query string
            try
            {
                SearchOptions searchOptions = new SearchOptions() { SearchMode = SearchMode.All };
                return searchClient.Search<SearchDocument>(searchText, searchOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }

    }
}
