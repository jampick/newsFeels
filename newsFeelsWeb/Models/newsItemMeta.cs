using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace newsFeelsWeb.Models
{
    class newsItemMeta : TableEntity
    {
        private string _newsDate;
        private double _average;

        public string NewsDate
        {
            get { return _newsDate; }
            set { _newsDate = value; }
        }
        public double Average
        {
            get { return _average; }
            set { _average = value; }
        }


        public newsItemMeta(string newsDate, double average)
        {
            this._newsDate = newsDate;
            this._average = average;
            this.PartitionKey = "newsFeelsMeta";
            this.RowKey = this._newsDate.GetHashCode().ToString(); ;

        }
        public newsItemMeta()
        {
        }
    }
    
}
