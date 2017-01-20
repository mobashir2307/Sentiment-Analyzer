using SentimentAnalyzer.Models;
using SentimentAnalyzer.Classes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;

namespace SentimentAnalyzer.Controllers
{
    public class EvaluationController : Controller
    {
        // GET: Evaluation
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Test()
        {
            TestingParameters model = new TestingParameters();
            model.classifiers = new List<string>();
            model.ClassifierType = new List<string>();
            model.selected = new List<bool>();
            String connetionString = "Data Source = DESKTOP-DUAFH99\\SQLEXPRESS; Initial Catalog = SentimentAnalyzer; Integrated Security = True";
            SqlConnection conn = new SqlConnection(connetionString);
            try
            {
                conn.Open();
                string query = "Select ClassifierName,Type from Classifiers";
                SqlCommand cmd = new SqlCommand(query, conn);
                using (SqlDataReader oReader = cmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        model.classifiers.Add((string)oReader[0]);
                        model.selected.Add(false);
                        model.ClassifierType.Add((string)oReader[1]);
                    }


                }
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            return View(model);
        }
        [HttpPost]
        public ActionResult Test(TestingParameters model)
        {

            ViewBag.fileErrMsg = "";
            ViewBag.classifierErr = "";
            if (Path.GetExtension(model.TestingFile.FileName).ToLower() != ".csv")
            {
                ViewBag.fileErrMsg = "CSV file required!";
                return View(model);
            }
            
            Classification cs = new Classification();
            string folderLoaction = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"];
            var fileName = Path.GetFileName(model.TestingFile.FileName);
            model.TestingFile.SaveAs(Path.Combine(folderLoaction, fileName));
            List<string> classifiers = new List<string>();
            List<string> classifierType = new List<string>();
            for (int i = 0; i < model.selected.Count; i++)
            {
                if (model.selected[i] == true)
                {
                    classifiers.Add(model.classifiers[i]);
                    classifierType.Add(model.ClassifierType[i]);
                }
            }
            if (classifiers.Count == 0)
            {
                ViewBag.classifierErr = "At least one classifier needs to be selected to perform testing";
                return View(model);
            }
            string TestId = Guid.NewGuid().ToString();
            string csvLocation = folderLoaction + model.TestingFile.FileName;
            try {
                cs.Test(csvLocation, classifiers, classifierType, TestId);
            }
            catch (Exception e) {
                ViewBag.fileErrMsg = "Wrong data format!!Please enter a CSV file in format: \"TweetID,Sentiment,Text\"";
                return View(model);
            }
            
            return RedirectToAction("TestResult", "Evaluation", new { TestID = TestId });
        }
        public ActionResult Train()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Train(TrainingParameterModel model)
        {
            ViewBag.errMsg = "";
            if (model.NumberOfNgrams < 1 || model.ngramFilterThreshold < 1) return View(model);
            String connetionString = "Data Source = DESKTOP-DUAFH99\\SQLEXPRESS; Initial Catalog = SentimentAnalyzer; Integrated Security = True";
            SqlConnection conn = new SqlConnection(connetionString);
            try
            {
                conn.Open();
                string query = "Select * from Classifiers where ClassifierName='" + model.classifierName + "'";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows == true)
                {
                    reader.Close();
                    ViewBag.errMsg = "A classifier already exist with this name! Pease enter a different name.";
                    return View(model);
                }
                
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            ViewBag.fileErrMsg = "";
            if (Path.GetExtension(model.TrainingFile.FileName).ToLower() != ".csv") {
                ViewBag.fileErrMsg = "CSV file required!";
                return View(model);
            }

            string folderLoaction=ConfigurationManager.AppSettings["SentimentAnalyzerFolder"];
            folderLoaction = folderLoaction + model.classifierName;
            Directory.CreateDirectory(folderLoaction);
            var fileName = Path.GetFileName(model.TrainingFile.FileName);
           
            model.TrainingFile.SaveAs(Path.Combine(folderLoaction, fileName));

            string csvFileLocatiion = folderLoaction+"\\"+fileName;
            string positiveNgramLocation = folderLoaction+@"\positiveNgrams.txt";
            string negativeNgramLocation = folderLoaction+@"\negativeNgrams.txt";
            string subjectivityFileLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"]+"Subjectivity.txt";
            string stopwordsFileLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"]+"StopWords.txt";
            int ngramFilterThreshold = model.ngramFilterThreshold;
            string NgramsImportanceOutputLocation = folderLoaction+@"\nGramsImportance.txt";
            int numberOfNgramsTobeSelected = model.NumberOfNgrams;
            string SelectedNgramsLocation = folderLoaction+@"\selectedNgrams.txt";
            string TrainingFileLocation = folderLoaction+@"\trainMatrix.csv";
            string positiveEmoLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"]+"PositiveEmo.txt";
            string negativeEmoLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"]+"NegativeEmo.txt";
            string RScriptRunnerLocation = @"C:\Program Files\R\R-3.3.1\bin\x64\Rscript.exe";

            Classification cs = new Classification();
            try
            {
                cs.Train(csvFileLocatiion, positiveNgramLocation, negativeNgramLocation, subjectivityFileLocation, stopwordsFileLocation, ngramFilterThreshold, NgramsImportanceOutputLocation, numberOfNgramsTobeSelected, SelectedNgramsLocation, TrainingFileLocation, positiveEmoLocation, negativeEmoLocation, model.ChiOrCPD, model.ChoesenModel, model.classifierName, RScriptRunnerLocation);
            }
            catch (Exception e)
            {
                var directory = new DirectoryInfo(folderLoaction);
                directory.Delete(true);
                ViewBag.fileErrMsg = "Wrong data format!Please enter a CSV file in format: \"TweetID,Sentiment,Text\"";
                return View(model);
            }
            
            connetionString = "Data Source = DESKTOP-DUAFH99\\SQLEXPRESS; Initial Catalog = SentimentAnalyzer; Integrated Security = True";
            conn = new SqlConnection(connetionString);
            string user = "Default";
            try
            {
                conn.Open();
                string query = "INSERT INTO Classifiers (ClassifierName, Type, Location,NgramSelectionMethod,UserID) VALUES('" + model.classifierName + "','" + model.ChoesenModel + "','" + model.classifierName + "','" + model.ChiOrCPD + "','" + user + "')";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.ExecuteNonQuery();
                conn.Close();


            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            return RedirectToAction("Classifiers","Evaluation");;
        }

        public ActionResult TestResult(string TestID)
        {
            ViewBag.TestID = TestID;
            TestResult testResult = new TestResult();
            String connetionString = "Data Source = DESKTOP-DUAFH99\\SQLEXPRESS; Initial Catalog = SentimentAnalyzer; Integrated Security = True";
            SqlConnection conn = new SqlConnection(connetionString);
            try
            {
                conn.Open();
                string query = "Select * from TestResults where TestID='" + TestID + "'";
                SqlCommand cmd = new SqlCommand(query, conn);
                
                using (SqlDataReader oReader = cmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        string classifiersUsed = (string)oReader[1];
                        string precisions = (string)oReader[2];
                        string recalls = (string)oReader[3];
                        string accuracies = (string)oReader[4];
                        
                        if (classifiersUsed.Contains(","))
                        {
                            testResult.classifiers = classifiersUsed.Split(',');
                            testResult.precisions = precisions.Split(',');
                            testResult.recalls = recalls.Split(',');
                            testResult.accuracies = accuracies.Split(',');
                        }
                        else
                        {
                            testResult.classifiers = new string[1];
                            testResult.precisions = new string[1];
                            testResult.recalls = new string[1];
                            testResult.accuracies = new string[1];

                            testResult.classifiers[0] = classifiersUsed;
                            testResult.precisions[0] = precisions;
                            testResult.recalls[0] = recalls;
                            testResult.accuracies[0] = accuracies;
                        }

                    }


                }
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            return View(testResult);
        }
        public ActionResult TestResultDetails(string TestID)
        {
            List<TestResultDetails> testDetails = new List<Models.TestResultDetails>();
            string fileLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"] +"TestingDetails\\"+ TestID + ".csv";
            using (StreamReader sr = new StreamReader(fileLocation))
            {
                
                TestResultDetails heading = new TestResultDetails();
                string line = sr.ReadLine();
                string []lineElements = line.Split(',');
                heading.tweet = lineElements[0];
                heading.label = lineElements[1];
                heading.results = new List<string>();
                for (int i = 2; i < lineElements.Count(); i++)
                {
                    heading.results.Add(lineElements[i]);
                }
                testDetails.Add(heading);
                int numberOfClassifiers = heading.results.Count;
                while ((line = sr.ReadLine()) != null)
                {
                    lineElements = line.Split(',');
                    TestResultDetails details = new TestResultDetails();
                    details.tweet = "";
                    for (int i = 0; i < lineElements.Count() - numberOfClassifiers-1; i++)
                    {
                        details.tweet = details.tweet+lineElements[i];
                    }
                    details.label = lineElements[lineElements.Count() - numberOfClassifiers-1];
                    details.results = new List<string>();
                    for (int i = lineElements.Count() - numberOfClassifiers; i < lineElements.Count(); i++)
                    {
                        details.results.Add(lineElements[i]);
                    }
                    testDetails.Add(details);
                }
            }
            
            return View(testDetails.ToArray());
        }
        public ActionResult Classifiers()
        {
            List<Classifier> classifiers = new List<Classifier>();
            String connetionString = "Data Source = DESKTOP-DUAFH99\\SQLEXPRESS; Initial Catalog = SentimentAnalyzer; Integrated Security = True";
            SqlConnection conn = new SqlConnection(connetionString);
            try
            {
                conn.Open();
                string query = "Select * from Classifiers";
                SqlCommand cmd = new SqlCommand(query, conn);

                using (SqlDataReader oReader = cmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        Classifier classifier = new Classifier();
                        classifier.classifierName = (string)oReader[1];
                        classifier.Type = (string)oReader[2];
                        classifier.Location = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"] + (string)oReader[3];
                        classifier.NgramSelectionMethod = (string)oReader[4];
                        classifiers.Add(classifier);
                    }


                }
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            return View(classifiers.ToArray());
        }
        public ActionResult DeleteClassifier(string classifierName)
        {
            string classifierLocation = ConfigurationManager.AppSettings["SentimentAnalyzerFolder"] + classifierName;
            if (Directory.Exists(classifierLocation))
            {
                //Directory.Delete(classifierLocation);
                var directory = new DirectoryInfo(classifierLocation);
                directory.Delete(true);
            }
            String connetionString = "Data Source = DESKTOP-DUAFH99\\SQLEXPRESS; Initial Catalog = SentimentAnalyzer; Integrated Security = True";
            SqlConnection conn = new SqlConnection(connetionString);
            try
            {
                conn.Open();
                string query = "Delete FROM Classifiers WHERE ClassifierName='"+classifierName+"'";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.ExecuteNonQuery();
                
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            return RedirectToAction("Classifiers","Evaluation");
        }

        public ActionResult TestHistory()
        {

            List<TestHistoryModel> TestHistory = new List<TestHistoryModel>();
            String connetionString = "Data Source = DESKTOP-DUAFH99\\SQLEXPRESS; Initial Catalog = SentimentAnalyzer; Integrated Security = True";
            SqlConnection conn = new SqlConnection(connetionString);
            try
            {
                conn.Open();
                string query = "Select * from TestResults";
                SqlCommand cmd = new SqlCommand(query, conn);

                using (SqlDataReader oReader = cmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        TestHistoryModel test = new TestHistoryModel();
                        test.TestID = (string)oReader[0];
                        test.classifiersUsed = (string)oReader[1];
                        test.TestSetSize = oReader.GetInt32(6);
                        TestHistory.Add(test);
                    }


                }
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
            return View(TestHistory.ToArray());
        }
    }
}