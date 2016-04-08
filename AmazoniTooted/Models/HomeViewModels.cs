using AmazoniTooted.Amazon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AmazoniTooted.Models
{
    public class HomeViewModel
    {
        public string Otsing { get; set; }
    }

    public class TulemusViewModel
    {
        public string Keyword { get; set; }

        public int PageCount { get; set; }
        public int AmazonPageCount { get; set; }

        public List<AmazonItem> AmazonItemList { get; set; }
    }

    public class AmazonItem
    {
        public string Nimi { get; set; }
        public string Url { get; set; }
        public string Hind { get; set; }
    }
}