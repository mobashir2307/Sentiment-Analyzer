using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SentimentAnalyzer.Models
{
    public class TweetModel
    {
        public ulong TweetID { get; set; }
        public string TweetText { get; set; }
        public string TweetSentiment { get; set; }
    }
}