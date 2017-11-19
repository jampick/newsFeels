using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web;
using Newtonsoft.Json;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage types
using System.Diagnostics;
using Microsoft.Azure.WebJobs;
using System.Text.RegularExpressions;
using System.Configuration;
using System.IO;

namespace feeler
{
    class Program
    {
        public static string NEWSSEARCH_KEY = ConfigurationManager.AppSettings["NEWSSEARCH_KEY"];
        public static string TEXTANALYTICS_KEY = ConfigurationManager.AppSettings["TEXTANALYTICS_KEY"];
        public static string MAXSTORYCOUNT = "10";
        public static double SLEEPTIME = 3600000;//3600000
        public static double UPDATECOUNT = 60;
        static void Main(string[] args)
        {
            Console.WriteLine("*****NEWS FEELING IN PROGRESS******");
            newsItems headlines = new newsItems();
            string thisStoryCount = MAXSTORYCOUNT;

            try
            {
                if (args[0] != "") { thisStoryCount = args[0]; }
            }
            catch { }
            Task<newsItems> t ;
            try
            {
                //get news headlines
                Console.WriteLine("Getting news headlines...");
                t = GetNews("Top News", thisStoryCount);
                t.Wait();
                headlines = t.Result;
            }
            catch (Exception e)
            {
                Console.Write("ERROR: {0}, {1}", e.Message, e.StackTrace);
            }

            try
            {
                //get sentimate - happy, meh, sucky
                Console.WriteLine("Detecting feels...");
                t = GetSentiment(headlines);
                t.Wait();
                headlines = t.Result;
            }
            catch (Exception e)
            {
                Console.Write("ERROR: {0}, {1}", e.Message, e.StackTrace);
            }

            try
            {
                //save data
                Console.WriteLine("Save feeling for later...");
                saveData(headlines);
            }
            catch (Exception e)
            {
                Console.Write("ERROR: {0}, {1}", e.Message, e.StackTrace);
            }

            try
            {
                //list headlines and sentimate
                //Console.WriteLine("Show feels...");
                //showData();
            }
            catch (Exception e)
            {
                Console.Write("ERROR: {0}, {1}", e.Message, e.StackTrace);
            }
            
            try
            {
                //do meta data work
                Console.WriteLine("Calculating...");
                calcAverageSentiment();
            }
            catch (Exception e)
            {
                Console.Write("ERROR: {0}, {1}", e.Message, e.StackTrace);
            }


#if DEBUG
            //Console.ReadLine();
#endif

        }

        private static void showData()
        {
            CloudTable table = getTableStorage("newsItems");

            // Construct the query operation for all customer entities where PartitionKey="Smith".
            TableQuery<newsItem> query = new TableQuery<newsItem>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "newsFeels"));

            // Print the fields for each customer.
            foreach (newsItem entity in table.ExecuteQuery(query))
            {
                Console.WriteLine("{0}, {1}\t{2}\t{3}", entity.PartitionKey, entity.RowKey,
                    entity.Title, entity.Sentiment.ToString());
            }
        }

        private static void calcAverageSentiment()
        {
            CloudTable tableSrc = getTableStorage("newsItems");

            // Construct the query operation for all customer entities where PartitionKey="Smith".
            TableQuery<newsItem> query = new TableQuery<newsItem>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "newsFeels"));

            //create list for sentiment and get averages
            Dictionary<string, List<double>> averageSentiment = new Dictionary<string, List<double>>();
            List<double> avgs = new List<double>();
            foreach (newsItem entity in tableSrc.ExecuteQuery(query))
            {
                if (averageSentiment.ContainsKey(entity.Timestamp.LocalDateTime.ToShortDateString()))
                {
                    //key exists so add value
                    averageSentiment[entity.Timestamp.LocalDateTime.ToShortDateString()].Add(entity.Sentiment);
                }
                else
                {
                    //key does not exist so add it
                    averageSentiment.Add(entity.Timestamp.LocalDateTime.ToShortDateString(), new List<double> {entity.Sentiment});
                }
            }

            CloudTable tableDest = getTableStorage("newsItemsMeta");

            // Create the table if it doesn't exist.
            tableDest.CreateIfNotExists();

            // Create the batch operation.
            TableBatchOperation batchOperation = new TableBatchOperation();

            //daily averages
            foreach (var item in averageSentiment)
            {
                batchOperation.InsertOrReplace(new newsItemMeta(item.Key, item.Value.Average()));
                if (item.Key == DateTime.Now.ToShortDateString())
                {
                    Console.WriteLine("{0} {1}", item.Key, item.Value.Average());
                }
                avgs.Add(item.Value.Average());
            }
            //YTD average
            batchOperation.InsertOrReplace(new newsItemMeta("YTD", avgs.Average()));
            Console.WriteLine("{0}, {1}", "YTD", avgs.Average());

            // Execute the insert operation.
            try
            {
                tableDest.ExecuteBatch(batchOperation);
            }
            catch (Exception e)
            {
                Console.Write("ERROR: {0}, {1}", e.Message, e.StackTrace);
            }

        }

        private static CloudTable getTableStorage(string tableName)
        {
            // Retrieve the storage account from the connection string.
#if DEBUG
            CloudStorageAccount storageAccount = CloudStorageAccount.DevelopmentStorageAccount;

#else
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
#endif
            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "people" table.
            CloudTable table = tableClient.GetTableReference(tableName);
            return table;
        }

        private static void saveData(newsItems headlines)
        {
            //return if no headlines to process
            if (headlines.NewsItems.Count == 0) { return ; }

            CloudTable table = getTableStorage("newsItems");

            // Create the table if it doesn't exist.
            table.CreateIfNotExists();

            // Create the batch operation.
            TableBatchOperation batchOperation = new TableBatchOperation();

            // Create the TableOperation object that inserts the customer entity.
            foreach (newsItem item in headlines.NewsItems)
            {
                batchOperation.InsertOrReplace(item);
            }
            // Execute the insert operation.
            table.ExecuteBatch(batchOperation);
        }

        private static async Task<newsItems> GetNews(string topic, string storyCount)
        {
            //List<string> headlines = new List<string>();
            newsItems headlines = new newsItems();
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", NEWSSEARCH_KEY);

            // Request parameters
            queryString["q"] = topic;
            queryString["count"] = storyCount;
            queryString["offset"] = "0";
            queryString["mkt"] = "en-us";
            queryString["safeSearch"] = "Off";
            queryString["freshness"] = "Day";
            var uri = "https://api.cognitive.microsoft.com/bing/v5.0/news/search?" + queryString;

            dynamic response = await client.GetAsync(uri);
            dynamic content = await response.Content.ReadAsStringAsync();

            searchResult mySearchResult = JsonConvert.DeserializeObject<searchResult>(content);
            foreach(Value value in mySearchResult.value)
            {
                Image image = value.image;
                Thumbnail thumb = image.thumbnail;
                List<Provider> provider = value.provider;
                
#if DEBUG
                Console.WriteLine("Story: {0}: {1}, {2}, {3}", value.name.GetHashCode().ToString(), provider[0].name.ToString(), value.category ,value.name);
#endif
                headlines.Add(new newsItem(value.name, value.url, thumb.contentUrl, thumb.height, thumb.width, value.description, Convert.ToDateTime(value.datePublished), provider[0].name.ToString(), value.category));
            }

            //check to see if we already recorded this headline
            //flag items to remove
            List<newsItem> duplicates = new List<newsItem>();
            CloudTable table = getTableStorage("newsItems");
                foreach (newsItem item in headlines.NewsItems)
            {
                TableOperation retrieveOperation = TableOperation.Retrieve<newsItem>("newsFeels", item.Title.GetHashCode().ToString());

                TableResult query =table.Execute(retrieveOperation);

                //if we have result and result has sentiment > 0 then remove that story from processing
                if (query.Result != null && ((newsItem)query.Result).Sentiment > 0)
                {
                    duplicates.Add(item);
                }
            }
            //remove duplicates
            foreach (newsItem item in duplicates)
            {
                headlines.NewsItems.Remove(item);
            }
            Console.WriteLine("{0} new stories detected", headlines.NewsItems.Count);
            return headlines;
        }

        static async Task<newsItems> GetSentiment(newsItems headlines)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            Console.WriteLine("{0} stories for sentiment calculation", headlines.NewsItems.Count);

            //return if no headlines to process
            if (headlines.NewsItems.Count == 0 ) { return headlines; }

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", TEXTANALYTICS_KEY);

            var uri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment?" + queryString;

            HttpResponseMessage response;

            // Request body. Insert your text data here in JSON format.
            // thank you https://text-analytics-demo.azurewebsites.net/Home/SampleCode
            string mybody1 = "{\"documents\":[";
            string mybody2 = "]}";
            string headlineBody = "";
            foreach (newsItem headline in headlines.NewsItems)
            {
                //add json escape characters
                string tempTitle = HttpUtility.JavaScriptStringEncode(headline.Title);
                string tempContent = HttpUtility.JavaScriptStringEncode(headline.Content);
                
                headlineBody += "{\"id\":\""+ headline.ID +"\",\"text\":\""+ tempTitle + " " + tempContent + "\"},";
            }
            headlineBody = headlineBody.TrimEnd(',');
            string finalBody = mybody1 + headlineBody + mybody2;
            
            //finalBody = HttpUtility.JavaScriptStringEncode(finalBody,true);
#if DEBUG
            Console.WriteLine(finalBody);
#endif
            byte[] byteData = Encoding.UTF8.GetBytes(finalBody);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
            }
            string output = await response.Content.ReadAsStringAsync();
            dynamic jsonObj = JsonConvert.DeserializeObject<dynamic>(output);
            var value = jsonObj.documents;

            foreach (var data in value)
            {
                foreach (newsItem item in headlines.NewsItems)
                {
                    if (data.id == item.ID)
                    {
                        item.Sentiment = data.score;
                    }
                }
            }
            return headlines;

        }
    }
}
