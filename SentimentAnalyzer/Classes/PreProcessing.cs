using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace SentimentAnalyzer.Classes
{
    class PreProcessing
    {
        private string RemoveStopwords(String tweet, List<string> stopWords)
        {

            char[] _delimiters = new char[]
            {
            ' ',
            ',',
            ';',
            '.'
            };
            var words = tweet.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries);


            StringBuilder builder = new StringBuilder();

            foreach (string currentWord in words)
            {

                string lowerWord = currentWord.ToLower();

                if (!stopWords.Contains(lowerWord))
                {
                    builder.Append(currentWord).Append(' ');
                }
            }

            return builder.ToString().Trim();

        }
        public List<string> GetStopWords(List<LexiconItem> lexicon, string file)
        {
            List<string> stopwords = new List<string>();
            using (StreamReader sr = new StreamReader(file))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    line.Replace(Environment.NewLine, "");
                    if ((lexicon.SingleOrDefault(item => item.word == line)) == null)
                    {
                        stopwords.Add(line);
                    }
                }
            }

            return stopwords;
        }
        public List<LexiconItem> GetSubjectivityLexicon(string file)
        {
            List<LexiconItem> lexicon = new List<LexiconItem>();
            using (StreamReader sr = new StreamReader(file))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if ((lexicon.SingleOrDefault(item => item.word == line.Split(' ')[2].Split('=')[1])) == null)
                    {
                        LexiconItem lex = new LexiconItem();
                        lex.word = line.Split(' ')[2].Split('=')[1];
                        lex.polarity = line.Split(' ')[5].Split('=')[1];
                        lexicon.Add(lex);
                    }
                }
            }
            return lexicon;
        }
        public String RemoveURLs(String tweet)
        {
            tweet = Regex.Replace(tweet, @"http[^\s]+", "");
            return tweet;

        }
        private String RemoveUserNames(String tweet)
        {
            tweet = Regex.Replace(tweet, @"@[^\s]+", "");
            return tweet;

        }
        private String RemovePunctuations(String tweet)
        {
            tweet = Regex.Replace(tweet, @"[^\w\s]", "");
            return tweet;
        }
        private string EleaborateShortenedNegatives(string tweet)
        {
            tweet = tweet.Replace("dont", "do not");
            tweet = tweet.Replace("didnt", "did not");
            tweet = tweet.Replace("doesnt", "does not");
            tweet = tweet.Replace("cant", "can not");
            tweet = tweet.Replace("cannot", "can not");
            tweet = tweet.Replace("havent", "have not");
            tweet = tweet.Replace("hadnt", "had not");
            tweet = tweet.Replace("shant", "shall not");
            tweet = tweet.Replace("shouldnt", "should not");
            tweet = tweet.Replace("couldnt", "could not");
            tweet = tweet.Replace("wouldnt", "would not");
            tweet = tweet.Replace("arent", "are not");
            tweet = tweet.Replace("aint", "am not");
            tweet = tweet.Replace("isnt", "is not");
            tweet = tweet.Replace("wasnt", "was not");
            tweet = tweet.Replace("werent", "were not");
            return tweet;
        }
        public string PreProcess(string tweet, List<string> stopWords)
        {
            tweet = RemoveURLs(tweet);
            tweet = RemoveUserNames(tweet);
            tweet = tweet.ToLower();
            tweet = RemovePunctuations(tweet);
            tweet = EleaborateShortenedNegatives(tweet);
            tweet = RemoveStopwords(tweet, stopWords);
            return tweet;
        }



    }
    class LexiconItem
    {
        public string word { get; set; }
        public string polarity { get; set; }
    }
}