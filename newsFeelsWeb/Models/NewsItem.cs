using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using Microsoft.WindowsAzure.Storage.Table;

namespace newsFeelsWeb.Models
{
    public class NewsItem : TableEntity
    {
        public string Title { get; set; }
        public string StoryURL { get; set; }
        public string ImageURL { get; set; }
        public int ImageThumbHeight { get; set; }
        public int ImageThumbWidth { get; set; }
        public string Content { get; set; }
        public string ID { get; set; }
        public double Sentiment { get; set; }
        public DateTime DatePublished { get; set; }
        public string Provider { get; set; }
        public string Category { get; set; }
    }

    public class NewsItemDBContext : DbContext
    {
        public DbSet<NewsItem> NewsItems { get; set; }
    }
}