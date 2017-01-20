using RDotNet;
using SentimentAnalyzer.Classes;
using SentimentAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace SentimentAnalyzer.Classes
{
    public class Classification
    {

        public void Train(string csvFileLocatiion, string positiveNgramLocation, string negativeNgramLocation, string subjectivityFileLocation, string stopwordsFileLocation, int ngramFilterThreshold, string NgramsImportanceOutputLocation, int numberOfNgramsTobeSelected, string SelectedNgramsLocation, string TrainingFileLocation, string positiveEmoLocation, string negativeEmoLocation, string chiOrCPD, string model, string classifierName, string RScriptRunnerLocation)
        {
            CreateMatrix(csvFileLocatiion, positiveNgramLocation, negativeNgramLocation, subjectivityFileLocation, stopwordsFileLocation, ngramFilterThreshold, NgramsImportanceOutputLocation, numberOfNgramsTobeSelected, SelectedNgramsLocation, TrainingFileLocation, positiveEmoLocation, negativeEmoLocation, chiOrCPD);
            if (model != "adaboost")
            {
                string RScriptLocation = Path.GetDirectoryName(csvFileLocatiion);
                string RLibrary = "e1071";
                File.AppendAllText(RScriptLocation + "\\Script.r", "library(" + RLibrary + ")\n");
                File.AppendAllText(RScriptLocation + "\\Script.r", "data<-read.csv(\"" + TrainingFileLocation.Replace("\\", "/") + "\",header=TRUE)\n");
                File.AppendAllText(RScriptLocation + "\\Script.r", "data <- as.data.frame(data, header = TRUE)\n");
                if (model == "SVMRadial") File.AppendAllText(RScriptLocation + "\\Script.r", classifierName + "<- svm(Sentiment~.,data=data,kernel=\"radial\")\n");
                else if (model == "SVMSigmoid") File.AppendAllText(RScriptLocation + "\\Script.r", classifierName + "<- svm(Sentiment~.,data=data,kernel=\"sigmoid\")\n");
                else if (model == "SVMPolynomial") File.AppendAllText(RScriptLocation + "\\Script.r", classifierName + "<- svm(Sentiment~.,data=data,kernel=\"polynomial\")\n");
                File.AppendAllText(RScriptLocation + "\\Script.r", "save(" + classifierName + ",file=\"" + RScriptLocation.Replace("\\", "/") + "/" + classifierName + ".rda\")\n");

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                // startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "\"" + RScriptRunnerLocation + "\""; ;
                //startInfo.FileName = RfileLocation;

                startInfo.Arguments = "\"" + RScriptLocation + "\\Script.r" + "\"";
                process.StartInfo = startInfo;
                process.Start();

                process.WaitForExit();
            }
            else
            {
                string RScriptLocation = Path.GetDirectoryName(csvFileLocatiion);
                string RLibrary = "fastAdaboost";
                File.AppendAllText(RScriptLocation + "\\Script.r", "library(" + RLibrary + ")\n");
                File.AppendAllText(RScriptLocation + "\\Script.r", "data<-read.csv(\"" + TrainingFileLocation.Replace("\\", "/") + "\",header=TRUE)\n");
                File.AppendAllText(RScriptLocation + "\\Script.r", "data <- as.data.frame(data, header = TRUE)\n");
                File.AppendAllText(RScriptLocation + "\\Script.r", classifierName + "<- adaboost(Sentiment~.,data=data,10)\n");
                File.AppendAllText(RScriptLocation + "\\Script.r", "save(" + classifierName + ",file=\"" + RScriptLocation.Replace("\\", "/") + "/" + classifierName + ".rda\")\n");

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                // startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "\"" + RScriptRunnerLocation + "\""; ;
                //startInfo.FileName = RfileLocation;

                startInfo.Arguments = "\"" + RScriptLocation + "\\Script.r" + "\"";
                process.StartInfo = startInfo;
                process.Start();

                process.WaitForExit();
            }
        }

        public void Test(string csvFileLocation, List<string> classifiers, List<string> classifierType, string TestID)
        {
            string TestingDetailsFolderLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"] + "TestingDetails";
            File.AppendAllText(TestingDetailsFolderLocation + "\\" + TestID + ".csv", "Tweet,Label");
            for (int i = 0; i < classifiers.Count; i++)
            {
                File.AppendAllText(TestingDetailsFolderLocation + "\\" + TestID + ".csv", "," + classifiers[i]);
            }
            File.AppendAllText(TestingDetailsFolderLocation + "\\" + TestID + ".csv", "\n");
            List<List<string>> results = new List<List<string>>();

            for (int i = 0; i < classifiers.Count; i++)
            {
                List<string> result = new List<string>();
                string MatrixLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"] + classifiers[i] + "\\testMatrix.csv";
                string SelectedNgramsLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"] + classifiers[i] + "\\selectedNgrams.txt";
                string positiveEmoLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"] + "PositiveEmo.txt";
                string negativeEmoLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"] + "NegativeEmo.txt";
                string subjectivityFileLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"] + "Subjectivity.txt";
                string stopwordsFileLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"] + "StopWords.txt";

                string TestScriptLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"] + classifiers[i] + "\\TestScript.r";
                string resultLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"] + classifiers[i] + "\\TestResult.txt";
                if (System.IO.File.Exists(MatrixLocation))
                {
                    System.IO.File.Delete(MatrixLocation);
                }
                WriteFeatures(csvFileLocation, MatrixLocation, SelectedNgramsLocation, positiveEmoLocation, negativeEmoLocation, subjectivityFileLocation, stopwordsFileLocation);

                if (System.IO.File.Exists(TestScriptLocation))
                {
                    System.IO.File.Delete(TestScriptLocation);
                }
                if (System.IO.File.Exists(resultLocation))
                {
                    System.IO.File.Delete(resultLocation);
                }
                string Rlibrary;
                if (classifierType[i] != "adaboost") Rlibrary = "e1071";
                else Rlibrary = "fastAdaboost";
                string classifierLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"] + classifiers[i] + "\\" + classifiers[i] + ".rda";

                File.AppendAllText(TestScriptLocation, "rm(list=ls())\n");
                File.AppendAllText(TestScriptLocation, "gc()\n");
                File.AppendAllText(TestScriptLocation, "library(" + Rlibrary + ")\n");
                File.AppendAllText(TestScriptLocation, "load(\"" + classifierLocation.Replace("\\", "/") + "\")\n");
                File.AppendAllText(TestScriptLocation, "data<-read.csv(\"" + MatrixLocation.Replace("\\", "/") + "\",header=TRUE)\n");
                File.AppendAllText(TestScriptLocation, "data <- as.data.frame(data, header = TRUE)\n");
                File.AppendAllText(TestScriptLocation, "pred<-predict(" + classifiers[i] + ",data)\n");
                if (classifierType[i] != "adaboost") File.AppendAllText(TestScriptLocation, "write.table(pred,file=\"" + resultLocation.Replace("\\", "/") + "\")\n");
                else File.AppendAllText(TestScriptLocation, "write.table(pred$class,file=\"" + resultLocation.Replace("\\", "/") + "\")\n");

                string RScriptRunnerLocation = @"C:\Program Files\R\R-3.3.1\bin\x64\Rscript.exe";
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                // startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "\"" + RScriptRunnerLocation + "\""; ;
                //startInfo.FileName = RfileLocation;

                startInfo.Arguments = "\"" + TestScriptLocation + "\"";
                process.StartInfo = startInfo;
                process.Start();


                process.WaitForExit();

                using (StreamReader sr = new StreamReader(resultLocation))
                {
                    string line = sr.ReadLine();
                    while ((line = sr.ReadLine()) != null)
                    {
                        result.Add(line.Split(' ')[1]);
                    }
                }

                results.Add(result);
            }

            List<string> labels = new List<string>();

            using (StreamReader sr = new StreamReader(csvFileLocation))
            {
                string line = "";
                string resultLine = "";
                int i = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] words = line.Split(',');
                    string sentiment = words[1];
                    string tweet = "";
                    for (int j = 2; j < words.Count(); j++)
                    {
                        tweet = tweet + words[j];
                    }
                    resultLine = resultLine + tweet;
                    if (sentiment == "0")
                    {
                        resultLine = resultLine + ",\"Negative\"";
                        labels.Add("\"Negative\"");
                    }
                    else
                    {
                        resultLine = resultLine + ",\"Positive\"";
                        labels.Add("\"Positive\"");
                    }
                    for (int j = 0; j < results.Count; j++)
                    {
                        resultLine = resultLine + "," + results[j][i];
                    }
                    resultLine = resultLine + "\n";
                    File.AppendAllText(TestingDetailsFolderLocation + "\\" + TestID + ".csv", resultLine);
                    line = "";
                    resultLine = "";
                    i++;
                }
            }

            string precisions = "";
            string recalls = "";
            string Accuracy = "";
            string classi = "";
            FindPerformance(labels, results, ref precisions, ref recalls, ref Accuracy);
            for (int i = 0; i < classifiers.Count; i++)
            {
                if (i == 0) classi = classi + classifiers[i];
                else classi = classi + "," + classifiers[i];
            }

            String connetionString = "Data Source = DESKTOP-DUAFH99\\SQLEXPRESS; Initial Catalog = SentimentAnalyzer; Integrated Security = True";
            SqlConnection conn = new SqlConnection(connetionString);
            try
            {
                conn.Open();


                string query = "INSERT INTO TestResults (TestID, ClassifiersUsed, Precisions,Recalls,Accuracies,TestSetSize) VALUES('" + TestID + "','" + classi + "','" + precisions + "','" + recalls + "','" + Accuracy + "','" + labels.Count + "')";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.ExecuteNonQuery();

                conn.Close();


            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }


        }
        public void CreateMatrix(String csvFileLocatiion, string positiveNgramLocation, string negativeNgramLocation, string subjectivityFileLocation, string stopwordsFileLocation, int ngramFilterThreshold, string NgramsImportanceOutputLocation, int numberOfNgramsTobeSelected, string SelectedNgramsLocation, string TrainingFileLocation, string positiveEmoLocation, string negativeEmoLocation, string chiOrCPD)
        {
            int positive = 0;
            int negative = 0;
            FindNgrams(csvFileLocatiion, positiveNgramLocation, negativeNgramLocation, subjectivityFileLocation, stopwordsFileLocation, ref positive, ref negative);
            if (chiOrCPD == "chi") CalculateChiSquared(positiveNgramLocation, negativeNgramLocation, ngramFilterThreshold, positive, negative, NgramsImportanceOutputLocation);
            else CalculateProportionalDifference(positiveNgramLocation, negativeNgramLocation, ngramFilterThreshold, positive, negative, NgramsImportanceOutputLocation);
            Filter(numberOfNgramsTobeSelected, NgramsImportanceOutputLocation, SelectedNgramsLocation);
            WriteFeatures(csvFileLocatiion, TrainingFileLocation, SelectedNgramsLocation, positiveEmoLocation, negativeEmoLocation, subjectivityFileLocation, stopwordsFileLocation);
        }
        
        //find positive and negative ngrams
        private void FindNgrams(String csvFileLocatiion, string positiveLocation, string negativeLocation, string subjectivityFileLocation, string stopwordsFileLocation,ref int positive, ref int negative)
        {

            //List<string> tweets = new List<string>();
            PreProcessing pr = new PreProcessing();

            List<LexiconItem> lexicon = pr.GetSubjectivityLexicon(subjectivityFileLocation);
            List<string> stopWords = pr.GetStopWords(lexicon, stopwordsFileLocation);
            using (StreamReader sr = new StreamReader(csvFileLocatiion))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    string[] words = line.Split(',');
                    string sentiment = words[1];
                    if (sentiment == "1") positive++;
                    else if (sentiment == "0") negative++;
                    else throw new Exception();
                    String tweet = "";
                    for (int i = 2; i < words.Count(); i++)
                    {
                        tweet = tweet + words[i];
                    }
                    tweet = pr.PreProcess(tweet, stopWords);
                    List<String> ngrams = CreateNGrams(tweet, 1);
                    if (ngrams != null)
                    {
                        if (sentiment == "0")
                        {
                            for (int i = 0; i < ngrams.Count; i++)
                            {

                                File.AppendAllText(negativeLocation, ngrams[i] + ",");
                            }
                        }
                        else
                        {
                            for (int i = 0; i < ngrams.Count; i++)
                            {

                                File.AppendAllText(positiveLocation, ngrams[i] + ",");
                            }
                        }

                    }
                    ngrams = new List<string>();
                    ngrams = CreateNGrams(tweet, 2);
                    if (ngrams != null)
                    {

                        if (sentiment == "0")
                        {
                            for (int i = 0; i < ngrams.Count; i++)
                            {

                                File.AppendAllText(negativeLocation, ngrams[i] + ",");
                            }
                        }
                        else
                        {
                            for (int i = 0; i < ngrams.Count; i++)
                            {

                                File.AppendAllText(positiveLocation, ngrams[i] + ",");
                            }
                        }
                    }
                }
            }



        }
        private void CalculateChiSquared(string positiveLocation, string negativeLocation, int filterThreshold, int totalPositiveTweets, int totalNegativeTweets, string outputLocation)
        {
            string[] PositiveNgrams;
            using (StreamReader sr = new StreamReader(positiveLocation))
            {
                PositiveNgrams = sr.ReadLine().Split(',');
            }
            string[] NegativeNgrams;
            using (StreamReader sr = new StreamReader(negativeLocation))
            {
                NegativeNgrams = sr.ReadLine().Split(',');
            }

            List<string> allNgrams = new List<string>();
            var Positivengrams = PositiveNgrams.GroupBy(v => v);
            var Negativengrams = NegativeNgrams.GroupBy(v => v);

            foreach (var ngram in Positivengrams)
            {
                if ((ngram.Count() > filterThreshold) && !(allNgrams.Contains(ngram.Key)))
                {
                    allNgrams.Add(ngram.Key);
                }
            }
            foreach (var ngram in Negativengrams)
            {
                if ((ngram.Count() > filterThreshold) && !(allNgrams.Contains(ngram.Key)))
                {
                    allNgrams.Add(ngram.Key);
                }
            }
            for (int i = 0; i < allNgrams.Count; i++)
            {
                int positiveCount = 0;
                int negativeCount = 0;
                Ngram ngram = new Ngram();
                ngram.ngram = allNgrams[i];
                if (Positivengrams.SingleOrDefault(item => item.Key == allNgrams[i]) != null)
                {
                    positiveCount = Positivengrams.SingleOrDefault(item => item.Key == allNgrams[i]).Count();
                }
                if (Negativengrams.SingleOrDefault(item => item.Key == allNgrams[i]) != null)
                {
                    negativeCount = Negativengrams.SingleOrDefault(item => item.Key == allNgrams[i]).Count();
                }

                double A = positiveCount;
                double B = negativeCount;
                double C = totalPositiveTweets - positiveCount;
                double D = totalNegativeTweets - negativeCount;
                double N = A + B + C + D;
                double positiveChiSquared = (N * Math.Pow(((A * D) - (B * C)), 2)) / ((A + C) * (B + D) * (A + B) * (C + D));
                A = negativeCount;
                B = positiveCount;
                C = totalNegativeTweets - negativeCount;
                D = totalPositiveTweets - positiveCount;
                N = A + B + C + D;
                double negativeChiSquared = (N * Math.Pow(((A * D) - (B * C)), 2)) / ((A + C) * (B + D) * (A + B) * (C + D));
                if (positiveChiSquared > negativeChiSquared) ngram.ChiSquared = positiveChiSquared;
                else ngram.ChiSquared = negativeChiSquared;

                File.AppendAllText(outputLocation, ngram.ngram + "," + ngram.ChiSquared + "\n");
            }

        }
        private void CalculateProportionalDifference(string positiveLocation, string negativeLocation, int filterThreshold, int totalPositiveTweets, int totalNegativeTweets, string outputLocation)
        {
            string[] PositiveNgrams;
            using (StreamReader sr = new StreamReader(positiveLocation))
            {
                PositiveNgrams = sr.ReadLine().Split(',');
            }
            string[] NegativeNgrams;
            using (StreamReader sr = new StreamReader(negativeLocation))
            {
                NegativeNgrams = sr.ReadLine().Split(',');
            }

            List<string> allNgrams = new List<string>();
            var Positivengrams = PositiveNgrams.GroupBy(v => v);
            var Negativengrams = NegativeNgrams.GroupBy(v => v);

            foreach (var ngram in Positivengrams)
            {
                if ((ngram.Count() > filterThreshold) && !(allNgrams.Contains(ngram.Key)))
                {
                    allNgrams.Add(ngram.Key);
                }
            }
            foreach (var ngram in Negativengrams)
            {
                if ((ngram.Count() > filterThreshold) && !(allNgrams.Contains(ngram.Key)))
                {
                    allNgrams.Add(ngram.Key);
                }
            }
            for (int i = 0; i < allNgrams.Count; i++)
            {
                int positiveCount = 0;
                int negativeCount = 0;
                Ngram ngram = new Ngram();
                ngram.ngram = allNgrams[i];
                if (Positivengrams.SingleOrDefault(item => item.Key == allNgrams[i]) != null)
                {
                    positiveCount = Positivengrams.SingleOrDefault(item => item.Key == allNgrams[i]).Count();
                }
                if (Negativengrams.SingleOrDefault(item => item.Key == allNgrams[i]) != null)
                {
                    negativeCount = Negativengrams.SingleOrDefault(item => item.Key == allNgrams[i]).Count();
                }

                double A = positiveCount;
                double B = negativeCount;

                double positiveChiSquared = (A - B) / (A + B);
                A = negativeCount;
                B = positiveCount;

                double negativeChiSquared = (A - B) / (A + B);
                if (positiveChiSquared > negativeChiSquared) ngram.ChiSquared = positiveChiSquared;
                else ngram.ChiSquared = negativeChiSquared;

                File.AppendAllText(outputLocation, ngram.ngram + "," + ngram.ChiSquared + "\n");
            }

        }
        private void Filter(int number, string InputFile, string OutputFile)
        {
            try {
                List<Ngram> ngramsWithCPDValue = new List<Ngram>();
                
                using (StreamReader sr = new StreamReader(InputFile))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {

                        Ngram ng = new Ngram();
                        ng.ngram = line.Split(',')[0];
                        ng.ChiSquared = Convert.ToDouble(line.Split(',')[1]);
                        ngramsWithCPDValue.Add(ng);
                    }
                }

                List<Ngram> sortedList = ngramsWithCPDValue.OrderByDescending(si => si.ChiSquared).ToList();
                if (number > sortedList.Count) number = sortedList.Count;
                for (int i = 0; i < number; i++)
                {
                    File.AppendAllText(OutputFile, sortedList[i].ngram + ",");
                }
            }
            catch (Exception e) { 
            
            }
            



        }
        public void WriteFeatures(List<TweetModel> tweets, string outputFile, string ngramsLocation, string positiveEmoLocation, string negativeEmoLocation, string subjectivityLexiconLocation, string stopWordsLocation)
        {
            List<string>temp = new List<string>();
            string[] ngrams= temp.ToArray();
            try {
                using (StreamReader sr = new StreamReader(ngramsLocation))
                {
                    ngrams = sr.ReadLine().Split(',');
                    ngrams = ngrams.Take(ngrams.Count() - 1).ToArray();
                    for (int i = 0; i < ngrams.Count(); i++)
                    {
                        File.AppendAllText(outputFile, ngrams[i] + ",");
                    }
                }
            }catch(Exception e){}
            
            
            File.AppendAllText(outputFile, "PriorPositive,");
            File.AppendAllText(outputFile, "PriorNegative,");
            File.AppendAllText(outputFile, "PositiveEmoticon,");
            File.AppendAllText(outputFile, "NegativeEmoticon,");
            File.AppendAllText(outputFile, "Intensifier,");
            File.AppendAllText(outputFile, "Sentiment\n");

            string[] PositiveEmoticons;
            using (StreamReader sr = new StreamReader(positiveEmoLocation))
            {
                PositiveEmoticons = sr.ReadLine().Split(',');
            }
            string[] NegativeEmoticons;
            using (StreamReader sr = new StreamReader(negativeEmoLocation))
            {
                NegativeEmoticons = sr.ReadLine().Split(',');
            }
            PreProcessing pr = new PreProcessing();
            List<LexiconItem> lexicon = pr.GetSubjectivityLexicon(subjectivityLexiconLocation);
            List<string> stopWords = pr.GetStopWords(lexicon, stopWordsLocation);

            for (int i = 0; i < tweets.Count; i++)
            {
                string tweet = tweets[i].TweetText;
                string sentiment = "";
                if (tweets[i].TweetSentiment == "0") sentiment = "Negative";
                else if(tweets[i].TweetSentiment == "1")sentiment = "Positive";
                tweet = pr.RemoveURLs(tweet);
                int positiveEmo = 0;
                int negativeEmo = 0;
                findEmoticons(tweet, ref positiveEmo, ref negativeEmo, PositiveEmoticons, NegativeEmoticons);
                tweet = pr.PreProcess(tweet, stopWords);

                List<String> tweetngrams = new List<string>();
                List<string> bigrams = CreateNGrams(tweet, 2);
                tweetngrams = CreateNGrams(tweet, 1);
                try
                {
                    tweetngrams.AddRange(bigrams);
                }
                catch (Exception e)
                {

                }
                List<int> ngramPresence = findPresenceofNgrams(ngrams, tweetngrams);

                int priorPositive = 0;
                int priorNegative = 0;

                getPriorPolarity(tweet, lexicon, ref priorPositive, ref priorNegative);

                int intensifier;
                if (findPresenceofIntensifiers(tweet)) intensifier = 1;
                else intensifier = 0;

                for (int j = 0; j < ngramPresence.Count; j++)
                {
                    File.AppendAllText(outputFile, ngramPresence[j] + ",");
                }
                File.AppendAllText(outputFile, priorPositive + ",");
                File.AppendAllText(outputFile, priorNegative + ",");
                File.AppendAllText(outputFile, positiveEmo + ",");
                File.AppendAllText(outputFile, negativeEmo + ",");
                File.AppendAllText(outputFile, intensifier + ",");
                File.AppendAllText(outputFile, sentiment + "\n");
            }




        }
        
        
        public void WriteFeatures(string inputFile, string outputFile, string ngramsLocation, string positiveEmoLocation, string negativeEmoLocation, string subjectivityLexiconLocation, string stopWordsLocation)
        {

            List<string> temp = new List<string>();
            string[] ngrams = temp.ToArray();
            try {
                using (StreamReader sr = new StreamReader(ngramsLocation))
                {
                    ngrams = sr.ReadLine().Split(',');
                }
                ngrams = ngrams.Take(ngrams.Count() - 1).ToArray();
                for (int i = 0; i < ngrams.Count(); i++)
                {
                    File.AppendAllText(outputFile, ngrams[i] + ",");
                }
            }
            catch (Exception e) { }
            
            File.AppendAllText(outputFile, "PriorPositive,");
            File.AppendAllText(outputFile, "PriorNegative,");
            File.AppendAllText(outputFile, "PositiveEmoticon,");
            File.AppendAllText(outputFile, "NegativeEmoticon,");
            File.AppendAllText(outputFile, "Intensifier,");
            File.AppendAllText(outputFile, "Sentiment\n");

            string[] PositiveEmoticons;
            using (StreamReader sr = new StreamReader(positiveEmoLocation))
            {
                PositiveEmoticons = sr.ReadLine().Split(',');
            }
            string[] NegativeEmoticons;
            using (StreamReader sr = new StreamReader(negativeEmoLocation))
            {
                NegativeEmoticons = sr.ReadLine().Split(',');
            }
            PreProcessing pr = new PreProcessing();
            List<LexiconItem> lexicon = pr.GetSubjectivityLexicon(subjectivityLexiconLocation);
            List<string> stopWords = pr.GetStopWords(lexicon, stopWordsLocation);
            using (StreamReader sr = new StreamReader(inputFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] lineElements = line.Split(',');
                    string sentiment = lineElements[1];
                    if (lineElements[1] == "0") sentiment = "Negative";
                    else if (lineElements[1] == "1") sentiment = "Positive";
                    else throw new Exception();
                    string tweet = "";
                    for (int i = 2; i < lineElements.Count(); i++)
                    {
                        tweet = tweet + lineElements[i];
                    }
                    tweet = pr.RemoveURLs(tweet);
                    int positiveEmo = 0;
                    int negativeEmo = 0;
                    findEmoticons(tweet, ref positiveEmo, ref negativeEmo, PositiveEmoticons, NegativeEmoticons);
                    tweet = pr.PreProcess(tweet, stopWords);

                    List<String> tweetngrams = new List<string>();
                    List<string> bigrams = CreateNGrams(tweet, 2);
                    tweetngrams = CreateNGrams(tweet, 1);
                    try
                    {
                        tweetngrams.AddRange(bigrams);
                    }
                    catch (Exception e)
                    {

                    }
                    List<int> ngramPresence = findPresenceofNgrams(ngrams, tweetngrams);

                    int priorPositive = 0;
                    int priorNegative = 0;

                    getPriorPolarity(tweet, lexicon, ref priorPositive, ref priorNegative);

                    int intensifier;
                    if (findPresenceofIntensifiers(tweet)) intensifier = 1;
                    else intensifier = 0;

                    for (int j = 0; j < ngramPresence.Count; j++)
                    {
                        File.AppendAllText(outputFile, ngramPresence[j] + ",");
                    }
                    File.AppendAllText(outputFile, priorPositive + ",");
                    File.AppendAllText(outputFile, priorNegative + ",");
                    File.AppendAllText(outputFile, positiveEmo + ",");
                    File.AppendAllText(outputFile, negativeEmo + ",");
                    File.AppendAllText(outputFile, intensifier + ",");
                    File.AppendAllText(outputFile, sentiment + "\n");
                }
            }

        }

        private List<String> CreateNGrams(String tweet, int n)
        {
            if (tweet == "") return null;
            List<String> ngrams = new List<string>();
            String[] tokens = tweet.Split(' ');
            String tokenToRemove = "";
            tokens = tokens.Where(val => val != tokenToRemove).ToArray();
            tokenToRemove = "\n";
            tokens = tokens.Where(val => val != tokenToRemove).ToArray();
            for (int i = 0; i < tokens.Count(); i++)
            {
                tokens[i] = Regex.Replace(tokens[i], @"\t|\n|\r", "");
            }

            if (n > tokens.Count()) return null;
            int indexFrom = 0;

            while (indexFrom <= tokens.Count() - n)
            {

                if (tokens[indexFrom] == "no" || tokens[indexFrom] == "not")
                {
                    indexFrom = indexFrom + 1;
                    continue;
                }
                string ngram = "";

                int i = indexFrom;
                int number = 0;
                while (number != n)
                {
                    int flag = 0;
                    try
                    {
                        if (tokens[i] == "no" || tokens[i] == "not")
                        {
                            i++;
                            continue;
                        }

                        if (i != 0)
                        {
                            if (tokens[i - 1] == "no" || tokens[i - 1] == "not" && (!ngram.Contains("no") || !ngram.Contains("not")))
                            {
                                ngram = ngram + tokens[i - 1];
                                flag = 1;
                            }
                        }


                        if (flag == 0)
                        {
                            if (number != 0) ngram = ngram + " " + tokens[i];
                            else ngram = ngram + tokens[i];
                        }
                        else ngram = ngram + tokens[i];

                        if (tokens[i + 1] == "no" || tokens[i + 1] == "not" && (!ngram.Contains("no") || !ngram.Contains("not"))) ngram = ngram + tokens[i + 1];


                    }
                    catch (IndexOutOfRangeException e) { }
                    number++;
                    i++;

                }
                // Console.WriteLine(ngram);               
                if (ngram != "") ngrams.Add(ngram);
                indexFrom = indexFrom + 1;

            }

            return ngrams;
        }
        private void findEmoticons(string tweet, ref int positive, ref int negative, string[] positiveEmo, string[] negativeEmo)
        {
            for (int i = 0; i < positiveEmo.Count(); i++)
            {
                if (tweet.Contains(positiveEmo[i]))
                {
                    positive++;
                    Console.WriteLine(positiveEmo[i]);
                }
            }
            for (int i = 0; i < negativeEmo.Count(); i++)
            {
                if (tweet.Contains(negativeEmo[i]))
                {
                    negative++;
                    Console.WriteLine(negativeEmo[i]);
                }
            }

        }
        private List<int> findPresenceofNgrams(string[] ngrams, List<string> ngramsIntweet)
        {
            List<int> vector = new List<int>();
            for (int i = 0; i < ngrams.Count(); i++)
            {
                vector.Add(0);
            }
            if (ngramsIntweet != null)
            {
                for (int i = 0; i < ngramsIntweet.Count; i++)
                {
                    int index;
                    if (ngrams.Contains(ngramsIntweet[i]))
                    {
                        index = Array.IndexOf(ngrams, ngramsIntweet[i]);
                        vector[index] = 1;
                    }
                }
            }

            return vector;
        }
        private bool findPresenceofIntensifiers(string tweet)
        {
            char[] delimiters = new char[]
            {
            ' ',
            ',',
            ';',
            '.'
            };
            int flag = 0;
            string[] words = tweet.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Count(); i++)
            {
                if (Regex.IsMatch(words[i], @"(.)\1{2,}", RegexOptions.IgnoreCase) == true) return true;
                for (int j = 0; j < words[i].Length; j++)
                {
                    if (!Char.IsUpper(words[i][j]))
                    {
                        flag = 0;
                        break;
                    }
                    else flag = 1;
                }
                if (flag == 1) return true;
            }
            return false;
        }
        private void getPriorPolarity(String tweet, List<LexiconItem> lexicon, ref int positiveScore, ref int negativeScore)
        {

            char[] delimiters = new char[]
            {
            ' ',
            ',',
            ';',
            '.'
            };
            string[] words = tweet.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Count(); i++)
            {
                LexiconItem lex = new LexiconItem();

                if ((lex = lexicon.SingleOrDefault(item => item.word == words[i])) != null)
                {
                    if (lex.polarity == "positive")
                    {
                        if (i >= 1 && (words[i - 1] == "no" || words[i - 1] == "not"))
                        {
                            negativeScore++;
                        }
                        else positiveScore++;
                    }
                    else if (lex.polarity == "negative")
                    {
                        if (i >= 1 && (words[i - 1] == "no" || words[i - 1] == "not"))
                        {
                            positiveScore++;
                        }
                        else negativeScore++;
                    }

                }
            }
            
        }


        private void FindPerformance(List<string> labels, List<List<string>> results, ref string precisions, ref string recalls,ref string Accuracy)
        {
            for (int i = 0; i < results.Count; i++)
            {
                int rightCount = 0;
                int truePositive = 0;
                int falsePositive = 0;
                int trueNegative = 0;
                int falseNegative = 0;
                for (int j = 0; j < results[i].Count; j++)
                {
                    if (labels[j] == results[i][j]) rightCount++;
                    if (labels[j] == "\"Positive\"" && results[i][j] == "\"Positive\"") truePositive++;
                    if (labels[j] == "\"Negative\"" && results[i][j] == "\"Positive\"") falsePositive++;
                    if (labels[j] == "\"Negative\"" && results[i][j] == "\"Negative\"") trueNegative++;
                    if (labels[j] == "\"Positive\"" && results[i][j] == "\"Negative\"") falseNegative++;
                }
                double acc = ((double)rightCount / (double)labels.Count)*100;
                double precision = (double)truePositive / (double)(truePositive + falsePositive);
                double recall = (double)truePositive / (double)(truePositive + falseNegative);
                if (i == 0) { 
                    Accuracy = Accuracy + acc;
                    precisions = precisions + precision;
                    recalls = recalls + recall;
                }
                else
                {
                    Accuracy = Accuracy + "," + acc;
                    precisions = precisions + "," + precision;
                    recalls = recalls + "," + recall;
                }
                
            }
        }
        public List<string> SVMClassify(string classifierLocation, string classifierName, string matrixLocation, string resultLocation, string RfileLocation, string RScriptLocation, string RLibrary)
        {

            List<string> classes = new List<string>();
            /*var x = Environment.GetEnvironmentVariables();
            REngine.SetEnvironmentVariables();
            x = Environment.GetEnvironmentVariables();
            string path = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH",path+";"+ @"D:\Program Files\R-3.3.1\bin\i386");
            path = Environment.GetEnvironmentVariable("PATH");
            // There are several options to initialize the engine, but by default the following suffice:
            REngine engine = REngine.GetInstance();
            engine.Initialize();
            engine.Evaluate(@"source('C:/Users/User/Desktop/Script1.r')");
            
            engine.Dispose();*/
            if (System.IO.File.Exists(RScriptLocation))
            {
                System.IO.File.Delete(RScriptLocation);
            }
            if (System.IO.File.Exists(resultLocation))
            {
                System.IO.File.Delete(resultLocation);
            }
            // File.AppendAllText(RScriptLocation,"rm(list=ls())\n");
            //File.AppendAllText(RScriptLocation, "gc()\n");
            File.AppendAllText(RScriptLocation, "library(" + RLibrary + ")\n");
            File.AppendAllText(RScriptLocation, "load(\"" + classifierLocation + "\")\n");
            File.AppendAllText(RScriptLocation, "data<-read.csv(\"" + matrixLocation + "\",header=TRUE)\n");
            File.AppendAllText(RScriptLocation, "data <- as.data.frame(data, header = TRUE)\n");
            File.AppendAllText(RScriptLocation, "pred<-predict(" + classifierName + ",data)\n");
            File.AppendAllText(RScriptLocation, "write.table(pred,file=\"" + resultLocation + "\")\n");


            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            // startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "\"" + RfileLocation + "\""; ;
            //startInfo.FileName = RfileLocation;

            startInfo.Arguments = "\"" + RScriptLocation + "\"";
            process.StartInfo = startInfo;
            process.Start();

            process.WaitForExit();
            using (StreamReader st = new StreamReader(resultLocation))
            {
                string line;
                line = st.ReadLine();
                while ((line = st.ReadLine()) != null)
                {
                    classes.Add(line.Split(' ')[1]);
                }
            }


            return classes;
        }
    }
}