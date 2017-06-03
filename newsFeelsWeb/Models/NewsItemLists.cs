using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Collections;

namespace newsFeelsWeb.Models
{
    public class NewsItemLists 
    {
        [Key]
        public string Name { get; set; }
        public Dictionary<String, String> Description { get; set; }

    }
    public class NewsItemListsDBContext : DbContext
    {
        public DbSet<NewsItemLists> NewsItemsListss { get; set; }
    }

}