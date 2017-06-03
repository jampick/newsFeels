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
using Microsoft.WindowsAzure;
using newsFeelsWeb.Controllers.shared;

namespace newsFeelsWeb.Controllers
{
    public class NewsItemsController : Controller
    {
        private NewsItemDBContext db = new NewsItemDBContext();

        // GET: NewsItems
        public ActionResult Index()
        {
            CloudTable table = DataAccess.GetDataTable("newsItems");
            DateTimeOffset lowerlimit = DateTime.Today.AddDays(-1);

            string dateRangeFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "newsFeels"),
                TableOperators.And,
                TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThanOrEqual, DateTime.Today.AddDays(-1)));
            TableQuery<NewsItem> query = new TableQuery<NewsItem>().Where(dateRangeFilter);

            var newsItems = table.ExecuteQuery(query).ToList();
            var sortedData = newsItems.OrderByDescending(c => c.Sentiment).ToList();
            //return View(db.NewsItems.ToList());
            return View(sortedData);

            //return View(db.NewsItems.ToList());
        }


        // GET: NewsItems/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //NewsItem newsItem = db.NewsItems.Find(id);
            CloudTable table = DataAccess.GetDataTable("newsItems");
            DateTimeOffset lowerlimit = DateTime.Today.AddDays(-1);

            string dateRangeFilter = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "newsFeels"),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, id));
            TableQuery<NewsItem> query = new TableQuery<NewsItem>().Where(dateRangeFilter);

            var newsItems = table.ExecuteQuery(query).ToList();
            var sortedData = newsItems.OrderByDescending(c => c.Sentiment).ToList();
            //return View(db.NewsItems.ToList());
            //return View(sortedData);
            NewsItem newsItem = newsItems[0];

            if (newsItem == null)
            {
                return HttpNotFound();
            }

            return View(newsItem);
        }

        // GET: NewsItems/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: NewsItems/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,Title,StoryURL,ImageURL,Content,Sentiment,PartitionKey,RowKey,Timestamp,ETag")] NewsItem newsItem)
        {
            if (ModelState.IsValid)
            {
                db.NewsItems.Add(newsItem);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(newsItem);
        }

        // GET: NewsItems/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NewsItem newsItem = db.NewsItems.Find(id);
            if (newsItem == null)
            {
                return HttpNotFound();
            }
            return View(newsItem);
        }

        // POST: NewsItems/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,Title,StoryURL,ImageURL,Content,Sentiment,PartitionKey,RowKey,Timestamp,ETag")] NewsItem newsItem)
        {
            if (ModelState.IsValid)
            {
                db.Entry(newsItem).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(newsItem);
        }

        // GET: NewsItems/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NewsItem newsItem = db.NewsItems.Find(id);
            if (newsItem == null)
            {
                return HttpNotFound();
            }
            return View(newsItem);
        }

        // POST: NewsItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            NewsItem newsItem = db.NewsItems.Find(id);
            db.NewsItems.Remove(newsItem);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
