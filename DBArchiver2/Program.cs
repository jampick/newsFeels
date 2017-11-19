using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBArchiver2
{
    class Program
    {
        static void Main(string[] args)
        {
            int daysAgo = 40;
            try
            {
                if (args[0] != "") { daysAgo = Convert.ToInt16(args[0]); }
            }
            catch { }
            //get news items x days
            var olderThanDate = DateTime.Now.AddDays(-daysAgo);
            newsItems items = new newsItems();

            Console.WriteLine("Archiving News Items older than {0}", olderThanDate.ToShortDateString());

            //get all items we want to move from this DB to other DB
            CloudTable table = getTableStorage("newsItems");
            var query = table.CreateQuery<newsItem>()
                 .Where(d => d.PartitionKey == "newsFeels"
                        && d.Timestamp <= olderThanDate);
            
            var sortedItems = (List<newsItem>)null;
            try
            {
                var newsItems = query.ToList();
                sortedItems = newsItems.OrderByDescending(c => c.Timestamp).ToList();
                foreach (newsItem item in sortedItems)
                {
                    Console.WriteLine("{0}, {1}", item.Timestamp, item.Title);
                    items.Add(item);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e.InnerException);
            }

            //insert items into archive DB
            Console.WriteLine("Archiving {0} items", items.NewsItems.Count());
            try
            {
                saveData(items, "newsItemsArchive");
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: " + e.InnerException);
            }

            //remove items from current datebase
            if (items.NewsItems.Count() > 0)
            {
                Console.WriteLine("Removing {0} items from live DB", items.NewsItems.Count());
                try
                {
                    removeData(items, "newsItems");
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: " + e.InnerException);
                }
            }
            else
            {
                Console.WriteLine("Nothing to remove from live DB");
            }


        }

        private static void removeData(newsItems items, string tableName)
        {
            //return if no headlines to process
            if (items.NewsItems.Count == 0) { return; }

            CloudTable table = getTableStorage(tableName);

            // Create the table if it doesn't exist.
            table.CreateIfNotExists();

 
            // Create the TableOperation object that deletes the customer entity.
            foreach (newsItem item in items.NewsItems)
            {
                // Create a retrieve operation that expects a customer entity.
                TableOperation retrieveOperation = TableOperation.Retrieve<newsItem>(item.PartitionKey, item.RowKey);

                // Execute the operation.
                TableResult retrievedResult = table.Execute(retrieveOperation);

                // Assign the result to a CustomerEntity.
                newsItem deleteEntity = (newsItem)retrievedResult.Result;

                // Create the Delete TableOperation.
                if (deleteEntity != null)
                {
                    TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                    // Execute the operation.
                    table.Execute(deleteOperation);
                }
            }
        }

        private static void saveData(newsItems items, string tableName)
        {
            //return if no items to process
            if (items.NewsItems.Count == 0) { return; }

            CloudTable table = getTableStorage(tableName);

            // Create the table if it doesn't exist.
            table.CreateIfNotExists();

            // Create the batch operation.
            TableBatchOperation batchOperation = new TableBatchOperation();

            // Create the TableOperation object that inserts the customer entity.            
            foreach (newsItem item in items.NewsItems)
            {
                batchOperation.InsertOrReplace(item);
                //update in batches of 100
                if (batchOperation.Count >= 100)
                {
                    table.ExecuteBatch(batchOperation);                    
                    batchOperation.Clear();
                }
            }
            // Execute the insert operation for anything left over
            if (batchOperation.Count > 0)
            {
                table.ExecuteBatch(batchOperation);
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
        

    }
}
