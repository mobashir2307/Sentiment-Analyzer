using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace SentimentAnalyzer.Models
{
    public class TrainingParameterModel
    {
        public string classifierName { get; set; }
        public HttpPostedFileBase TrainingFile { get; set; }
        public string ChiOrCPD { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Please enter a value bigger than zero")]
        public int NumberOfNgrams { get; set; }
        public string ChoesenModel { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Please enter a value bigger than zero")]
        public int ngramFilterThreshold { get; set; }


    }
}