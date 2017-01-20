using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SentimentAnalyzer.Models
{
    public class TestResultDetails
    {
        public string tweet { get; set; }
        public string label { get; set; }
        public List<string> results { get; set; }
    }
}