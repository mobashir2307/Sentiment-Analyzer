using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SentimentAnalyzer.Models
{
    public class RequestParametersModel
    {
        public string Keyword { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please enter a value bigger than zero")]
        
        public int RequestedNumber { get; set; }
        public string SearchFor { get; set; }
    }
}