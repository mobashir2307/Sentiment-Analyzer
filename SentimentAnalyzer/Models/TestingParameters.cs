using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SentimentAnalyzer.Models
{
    public class TestingParameters
    {
        [Required(ErrorMessage="Please select a file for testing.")]
        public HttpPostedFileBase TestingFile { get; set; }
        public List<string> classifiers {get; set; }
        public List<bool> selected { get; set; }
        public List<string> ClassifierType { get; set; }

    }
}