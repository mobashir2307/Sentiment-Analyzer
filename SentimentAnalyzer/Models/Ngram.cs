using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SentimentAnalyzer.Models
{
    public class Ngram
    {
        public string ngram { get; set; }
        public double ChiSquared { get; set; }
    }
}