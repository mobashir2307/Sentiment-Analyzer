using SentimentAnalyzer.Classes;
using SentimentAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SentimentAnalyzer.Controllers
{
    public class HomeController : Controller
    {

        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Index(RequestParametersModel rpm)
        {
            TwitterCrawl tc = new TwitterCrawl();
            string ConsumerKey = "5ZKfQoFW3kzskgycEoXsqkMoA";
            string ConsumerSecret = "DPyywP2JdABUlAsQLH0tZJWSggsIzsq5kcC4gWcB9PEqwdQE3I";
            string OAuthToken = "1311205441-DRjklH8SukWeaMesKoU8c2ewsUnnddm7rxL98Cn";
            string OAuthTokenSecret = "Erj2ucS2pds7E1dXoy9zV0wxr8eD8Icx8TM1lSck64XSk";
            List<TweetModel> crawledTweets = tc.CrawlTweets(ConsumerKey, ConsumerSecret, OAuthToken, OAuthTokenSecret, rpm.RequestedNumber, rpm.SearchFor, rpm.Keyword);


            Classification cs = new Classification();
            string subjectivityFileLocation = @"E:\ProjectFinal\SentimentAnalyzerFiles\Subjectivity.txt";
            string stopwordsFileLocation = @"E:\ProjectFinal\SentimentAnalyzerFiles\StopWords.txt";
            string SelectedNgramsLocation = @"E:\ProjectFinal\SentimentAnalyzerFiles\svmSigmoidChi62000\selectedNgrams.txt";
            string positiveEmoLocation = @"E:\ProjectFinal\SentimentAnalyzerFiles\PositiveEmo.txt";
            string negativeEmoLocation = @"E:\ProjectFinal\SentimentAnalyzerFiles\NegativeEmo.txt";
            string testMatrixLocation = @"E:\ProjectFinal\SentimentAnalyzerFiles\svmSigmoidChi62000\testMatrix.csv";

            if (System.IO.File.Exists(testMatrixLocation))
            {
                System.IO.File.Delete(testMatrixLocation);
            }

            cs.WriteFeatures(crawledTweets, testMatrixLocation, SelectedNgramsLocation, positiveEmoLocation, negativeEmoLocation, subjectivityFileLocation, stopwordsFileLocation);
            string classifierLocation = "E:/ProjectFinal/SentimentAnalyzerFiles/svmSigmoidChi62000/svmSigmoidChi62000.rda";
            string matrixLocation = testMatrixLocation.Replace("\\", "/");
            string resultLocation = "E:/ProjectFinal/SentimentAnalyzerFiles/svmSigmoidChi62000/result.txt";
            string RfileLocation = @"C:\Program Files\R\R-3.3.1\bin\x64\Rscript.exe";
            string RscripLocation = @"E:\ProjectFinal\SentimentAnalyzerFiles\svmSigmoidChi62000\Script.r";

            List<string> result = cs.SVMClassify(classifierLocation, "svmSigmoidChi62000", matrixLocation, resultLocation, RfileLocation, RscripLocation, "e1071");

            for (int i = 0; i < result.Count; i++)
            {
                crawledTweets[i].TweetSentiment = result[i];
            }

            string id = Guid.NewGuid().ToString();

            String connetionString = "Data Source = DESKTOP-DUAFH99\\SQLEXPRESS; Initial Catalog = SentimentAnalyzer; Integrated Security = True";
            SqlConnection conn = new SqlConnection(connetionString);
            string defa = "Default";
            try
            {
                conn.Open();
                for (int i = 0; i < crawledTweets.Count(); i++)
                {

                    string query = "INSERT INTO Result (AnalysisID, Keyword, Tweet,Sentiment,UserID) VALUES('" + id + "','" + rpm.Keyword + "','" + crawledTweets[i].TweetText.Replace("'", "''") + "','" + crawledTweets[i].TweetSentiment + "','" + defa + "')";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.ExecuteNonQuery();
                }
                conn.Close();


            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            return RedirectToAction("Result", "Home", new { AnalysisId = id });
        }
        public ActionResult Result(string AnalysisId)
        {
            List<TweetModel> tweets = new List<TweetModel>();
            double positive = 0;
            double negative = 0;
            String connetionString = "Data Source = DESKTOP-DUAFH99\\SQLEXPRESS; Initial Catalog = SentimentAnalyzer; Integrated Security = True";
            SqlConnection conn = new SqlConnection(connetionString);
            try
            {
                conn.Open();
                string query = "Select * from Result where AnalysisID='" + AnalysisId + "'";
                SqlCommand cmd = new SqlCommand(query, conn);
                using (SqlDataReader oReader = cmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        TweetModel tweet = new TweetModel();
                        //tweet.TweetID = (ulong)oReader[1];
                        tweet.TweetText = (string)oReader[3];
                        tweet.TweetSentiment = (string)oReader[4];
                        if (tweet.TweetSentiment == "\"Positive\"") positive = positive + 1;
                        else negative = negative + 1;
                        tweets.Add(tweet);
                    }


                }
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            ViewBag.positivePercentage = (positive / (double)tweets.Count) * 100;
            ViewBag.negativePercentage = (negative / (double)tweets.Count) * 100;
            return View(tweets.ToArray());
        }
        /*public ActionResult Crawl()
        {
            TwitterCrawl tc = new TwitterCrawl();
            string ConsumerKey = "5ZKfQoFW3kzskgycEoXsqkMoA";
            string ConsumerSecret = "DPyywP2JdABUlAsQLH0tZJWSggsIzsq5kcC4gWcB9PEqwdQE3I";
            string OAuthToken = "1311205441-DRjklH8SukWeaMesKoU8c2ewsUnnddm7rxL98Cn";
            string OAuthTokenSecret = "Erj2ucS2pds7E1dXoy9zV0wxr8eD8Icx8TM1lSck64XSk";
            int requestedNumber = 10;
            string searchOrStream = "stream";
            
            // List<TweetModel> tweets = tc.CrawlTweets(ConsumerKey,ConsumerSecret,OAuthToken,OAuthTokenSecret,requestedNumber,searchOrStream,keyword);
            Classification cs = new Classification();
            string subjectivityFileLocation = @"E:\Important\SentimentAnalyzerFiles\Subjectivity.txt";
            string stopwordsFileLocation = @"E:\Important\SentimentAnalyzerFiles\StopWords.txt";
            string SelectedNgramsLocation = @"E:\Important\SentimentAnalyzerFiles\selectedNgrams.txt";
            string positiveEmoLocation = @"E:\Important\SentimentAnalyzerFiles\PositiveEmo.txt";
            string negativeEmoLocation = @"E:\Important\SentimentAnalyzerFiles\NegativeEmo.txt";
            string testMatrixLocation = @"E:\Important\SentimentAnalyzerFiles\testMatrix.csv";
            if (System.IO.File.Exists(testMatrixLocation))
            {
                //  System.IO.File.Delete(testMatrixLocation);
            }

            //cs.WriteFeatures(tweets, testMatrixLocation, SelectedNgramsLocation, positiveEmoLocation, negativeEmoLocation, subjectivityFileLocation, stopwordsFileLocation);
            string classifierLocation = "E:/ProjectFinal/Models/svmModel62000.rda";
            string matrixLocation = testMatrixLocation.Replace("\\", "/");
            string resultLocation = "E:/ProjectFinal/SentimentAnalyzerFiles/result.txt";

            //  cs.SVMClassify(classifierLocation, matrixLocation, resultLocation);
            return View();
        }*/

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}