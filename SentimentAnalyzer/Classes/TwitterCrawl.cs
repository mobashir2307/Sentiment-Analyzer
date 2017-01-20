using LinqToTwitter;
using Newtonsoft.Json.Linq;
using SentimentAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;

namespace SentimentAnalyzer.Classes
{
    public class TwitterCrawl
    {
        public List<TweetModel> CrawlTweets(string ConsumerKey, string ConsumerSecret, string OAuthToken, string OAuthTokenSecret, int requestedNumber, string searchOrStream, string keyword)
        {
            SingleUserAuthorizer auth = new SingleUserAuthorizer
            {

                CredentialStore = new InMemoryCredentialStore()
                {

                    ConsumerKey = ConsumerKey,
                    ConsumerSecret = ConsumerSecret,
                    OAuthToken = OAuthToken,
                    OAuthTokenSecret = OAuthTokenSecret

                }

            };
            var twitterCtx = new TwitterContext(auth);
            List<TweetModel> crawledTweets = new List<TweetModel>();
            if (searchOrStream == "search")
            {
                SearchTweets(twitterCtx, keyword, ref crawledTweets, 0, requestedNumber, DateTime.Now);

            }
            else
            {
                crawledTweets = StreamTweets(twitterCtx, keyword, crawledTweets, 0, requestedNumber, DateTime.Now);
            }


            return crawledTweets;
        }
        private void SearchTweets(TwitterContext twitterCtx, string keyword, ref List<TweetModel> crawledTweets, int count, int requestedNumber, DateTime currentTime)
        {
            if (count >= requestedNumber) return;
            try
            {
                var srch =
                  (from search in twitterCtx.Search
                   where search.Type == SearchType.Search &&
                         search.Query == keyword &&
                         search.SearchLanguage == "en" &&
                         search.Count == 100
                   select search)
                  .SingleOrDefault();
                foreach (var entry in srch.Statuses)
                {

                    var matches = crawledTweets.Where(p => p.TweetID == entry.StatusID);

                    if (matches.Count() == 0 && crawledTweets.Count < requestedNumber)
                    {
                        TweetModel tweet = new TweetModel();
                        tweet.TweetID = entry.StatusID;
                        tweet.TweetText = entry.Text;
                        tweet.TweetSentiment = "ToBeTested";
                        crawledTweets.Add(tweet);
                        count++;
                    }
                }
                SearchTweets(twitterCtx, keyword, ref crawledTweets, count, requestedNumber, currentTime);
            }
            catch (AggregateException e)
            {
                TimeSpan runningTime = DateTime.Now - currentTime;
                TimeSpan window = new TimeSpan(0, 15, 0);
                Thread.Sleep(window - runningTime);
                currentTime = DateTime.Now;
                SearchTweets(twitterCtx, keyword, ref crawledTweets, count, requestedNumber, currentTime);
            }

        }
        private List<TweetModel> StreamTweets(TwitterContext twitterCtx, string keyword, List<TweetModel> crawledTweets, int count, int requestedNumber, DateTime currentTime)
        {


            Console.Write("");

            var a = (from strm in twitterCtx.Streaming
                     where strm.Type == StreamingType.Filter &&
                           strm.Track == keyword &&
                           strm.Language == "en"
                     select strm)
                .StartAsync(async strm =>
                {


                    // TweeetResponse res = (TweeetResponse)new JavaScriptSerializer().Deserialize<object>(s);



                    try
                    {
                        JObject tweetResponse = JObject.Parse(strm.Content);
                        var matches = crawledTweets.Where(p => p.TweetID == (ulong)tweetResponse["id"]);
                        Console.WriteLine(strm.Content + "\n");
                        if (matches.Count() == 0 && crawledTweets.Count < requestedNumber)
                        {
                            TweetModel tweet = new TweetModel();
                            tweet.TweetID = (ulong)tweetResponse["id"];
                            tweet.TweetText = (string)tweetResponse["text"];
                            tweet.TweetSentiment = "ToBeTested";
                            crawledTweets.Add(tweet);
                            count++;
                            if (count == 20)
                            {
                                Console.WriteLine();
                            }
                            if (count == 50)
                            {
                                Console.WriteLine();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Write(count);
                        //Console.WriteLine(tweetResponse);
                        Console.WriteLine(crawledTweets);
                    }



                    if (count >= requestedNumber)
                        strm.CloseStream();
                }).Result;
            return crawledTweets;
        }

    }
    class TweeetResponse
    {
        string id;
        string text;
    }
}