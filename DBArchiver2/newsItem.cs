using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBArchiver2
{
    class newsItem : TableEntity
    {
        private string _title;
        private string _storyURL;
        private string _imageURL;
        private int _imageThumbHeight;
        private int _imageThumbWidth;
        private string _content;
        private double _sentiment;
        private int _id;
        private DateTime _datePublished;
        private string _provider;
        private string _category;

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                this._id = this._title.GetHashCode();
            }

        }
        public string StoryURL
        {
            get { return _storyURL; }
            set { _storyURL = value; }
        }
        public string ImageURL
        {
            get { return _imageURL; }
            set { _imageURL = value; }
        }
        public int ImageThumbHeight
        {
            get { return _imageThumbHeight; }
            set { _imageThumbHeight = value; }
        }
        public int ImageThumbWidth
        {
            get { return _imageThumbWidth; }
            set { _imageThumbWidth = value; }
        }
        public string Content
        {
            get { return _content; }
            set { _content = value; }
        }
        public double Sentiment
        {
            get { return _sentiment; }
            set { _sentiment = value; }
        }
        public int ID
        {
            get { return _id; }
        }
        public DateTime DatePublished
        {
            get { return _datePublished;  }
            set { _datePublished = value;  }
        }
        public string Provider
        {
            get { return _provider; }
            set { _provider = value;}

        }
        public string Category
        {
            get { return _category; }
            set { _category = value; }
        }

        public newsItem(string title, string storyURL, string imageURL, int imageThumbHeight, int imageThumbWidth, string content, DateTime datePublished, string provider, string category)
        {
            this._title = title;
            this._storyURL = storyURL;
            this.ImageURL = imageURL;
            this._imageThumbHeight = imageThumbHeight;
            this._imageThumbWidth = imageThumbWidth;
            this._content = content;
            this._datePublished = datePublished; 
            this._id = this._title.GetHashCode();
            this._sentiment = 0;
            this.PartitionKey = "newsFeels";
            this.RowKey = this._id.ToString();

            //set defaults
            if (category == null || category == "")
            {
                this._category = "General";
            }
            else
            {
                this._category = category;
            }

            if (provider == null || provider == "")
            {
                this._provider = "Unknown";
            }
            else
            {
                this._provider = provider;
            }

        }
        public newsItem()
        {
        }
    }

    class newsItems
    {
        private List<newsItem> _newsItems;

        public List<newsItem> NewsItems
        {
            get { return _newsItems; }
            set { _newsItems = value; }
        }

        public newsItems()
        {
            this._newsItems = new List<newsItem>();
        }

        public void Add (newsItem thisNewsItem)
        {
            this._newsItems.Add(thisNewsItem);
        }

    }

}
