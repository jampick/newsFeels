using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Azure; // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage; // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Table; // Namespace for Table storage types
using Microsoft.WindowsAzure;

namespace newsFeelsWeb.Controllers.shared
{
    public class DataAccess
    {
        public static CloudTable GetDataTable(string name)
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
            CloudTable table = tableClient.GetTableReference(name);

            // Construct the query operation for all customer entities where PartitionKey="Smith".
            //TableQuery<NewsItem> query = new TableQuery<NewsItem>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "newsFeels"));
            //TableQuery<NewsItem> query = new TableQuery<NewsItem>().Where(TableQuery.GenerateFilterCondition("Timestamp", QueryComparisons.Equal, "newsFeels"));
            //DateTimeOffset lowerlimit = DateTime.Today.AddDays(-52).ToString("yyyy-MM-dd");
            return table;
        }

    }
}