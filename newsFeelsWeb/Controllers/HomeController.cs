using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using newsFeelsWeb.Models;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage types
using newsFeelsWeb.Controllers.shared;
using System.Diagnostics;

namespace newsFeelsWeb.Controllers
{
    public class HomeController : Controller
    {
        private NewsItemDBContext db = new NewsItemDBContext();
        private NewsItemListsDBContext db2 = new NewsItemListsDBContext();

        public ActionResult Index(string providerSearch, string categorySearch)
        {
            //default view is to show last x hours of news
            var lastX = DateTime.Now.AddHours(-24);
            var startDate = DateTime.Now.AddDays(-30);
            var endDate = DateTime.Now;

            CloudTable table = DataAccess.GetDataTable("newsItems");
 
            CloudTable tableMeta = DataAccess.GetDataTable("newsItemsMeta");
            #region getFilterCounts
            var queryFilterAreas = table.CreateQuery<NewsItem>()
                 .Where(d => d.PartitionKey == "newsFeels"
                        && d.DatePublished >= lastX);

            var FilterCounts = queryFilterAreas.ToList();
            Dictionary<string, List<double>> providers = new Dictionary<string, List<double>>();
            Dictionary<string, List<double>> categories = new Dictionary<string, List<double>>();
            
            foreach (var item in FilterCounts)
            {
                string provider = item.Provider;
                if (provider == null) { provider = "Unknown"; }
                string category = item.Category;
                double sentiment = item.Sentiment * 100;
                if (category == null) { category = "General"; }

                if (providers.ContainsKey(provider))
                {
                    providers[provider].Add(sentiment);
                }
                else
                {
                    
                    providers.Add(provider, new List<double> {sentiment});
                }
                if (categories.ContainsKey(category))
                {
                    categories[category].Add(sentiment);
                }
                else
                {
                    categories.Add(category, new List<double> {sentiment});
                }
            }
            ViewBag.providers = providers;
            ViewBag.categories = categories;
            ViewBag.allSentimentAverage = FilterCounts.Average(p => p.Sentiment)*100;
            #endregion
            // Create a query: in this example I use the DynamicTableEntity class
            var query = table.CreateQuery<NewsItem>()
                 .Where(d => d.PartitionKey == "newsFeels"
                        && d.DatePublished >= startDate);

            ViewBag.SearchFilter = null;
            if (!String.IsNullOrEmpty(providerSearch))
            {
                query = query.Where(d => d.Provider == providerSearch);
                ViewBag.SearchFilter = providerSearch;
            }
            if (!String.IsNullOrEmpty(categorySearch))
            {
                query = query.Where(d => d.Category == categorySearch);
                ViewBag.SearchFilter = categorySearch;
            }

            #region getNewsHistory
            var newsItemsHistory = query.ToList();
            Dictionary<DateTime, List<double>> dateSentiment= new Dictionary<DateTime, List<double>>();

            foreach (var item in newsItemsHistory)
            {
                DateTime thisDate = Convert.ToDateTime(item.DatePublished.ToShortDateString());
                double sentiment = item.Sentiment * 100;
                if (dateSentiment.ContainsKey(thisDate))
                {
                    dateSentiment[thisDate].Add(sentiment);
                }
                else
                {

                    dateSentiment.Add(thisDate, new List<double> { sentiment });
                }

            }
            //var sortedData = newsItems.OrderByDescending(c => c.Sentiment).ToList();

            foreach (var item in dateSentiment.OrderBy(c => c.Key))
            {
                Debug.WriteLine("***" + item.Key + ":" + item.Value.Average());
            }
            ViewBag.dateSentiment = dateSentiment;
            #endregion

            query = query.Where(d => d.DatePublished >= lastX);
            var newsItems = query.ToList();

            var sortedData = newsItems.OrderByDescending(c => c.Sentiment).ToList();

            //get YTD
            string YTD = "YTD";
            TableOperation retrieveOperation = TableOperation.Retrieve<newsItemMeta>("newsFeelsMeta", YTD.GetHashCode().ToString());
            // Execute the retrieve operation.
            TableResult retrievedResult = tableMeta.Execute(retrieveOperation);
            ViewBag.YTD = Math.Round(((newsItemMeta)retrievedResult.Result).Average, 2) * 100;

            string today = DateTime.Now.ToShortDateString();
            TableOperation retrieveOperationToday = TableOperation.Retrieve<newsItemMeta>("newsFeelsMeta", today.GetHashCode().ToString());
            // Execute the retrieve operation.
            TableResult retrievedResultToday = tableMeta.Execute(retrieveOperationToday);

            //get today
            try
            {
                //ViewBag.Today = Math.Round(((newsItemMeta)retrievedResultToday.Result).Average, 2) * 100;
                ViewBag.Today = Math.Round(sortedData.Average(p => p.Sentiment), 2) * 100;
            }
            catch
            {
                ViewBag.Today = 0;
            }

            return View(sortedData);
        }


        public ActionResult About()
        {
            ViewBag.Message = "A word from the NewsFeeler...";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "First contact";

            return View();
        }

        public static HtmlString CreateColumn(string text)
        {
            HtmlString output = new HtmlString(text);
            return output;
        }
    }
}