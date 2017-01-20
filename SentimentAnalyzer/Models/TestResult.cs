using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SentimentAnalyzer.Models
{
    public class TestResult
    {
        public string[] classifiers;
        public string[] precisions;
        public string[] recalls;
        public string[] accuracies;
    }
}