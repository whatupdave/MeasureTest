using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace CoverDiff
{
    public class ProgramArgs
    {
        public bool AreValid { get; private set; }
        public string Errors { get; private set; }

        public string TestResultsFile { get; private set; }

        public ProgramArgs(string[] commandLineArgs)
        {
            Errors = "";
            if (commandLineArgs.Length < 1)
            {
                var assemblyName = typeof(Program).Assembly.GetName();
                Errors += string.Format("{0} Version {1}\nUsage: {0} test-results.xml", assemblyName.Name, assemblyName.Version);
            }
            else
            {
                TestResultsFile = commandLineArgs[0];
                if (!File.Exists(TestResultsFile))
                {
                    Errors += string.Format("Couldn't find file {0}", TestResultsFile);
                }
            }
            AreValid = string.IsNullOrEmpty(Errors);
        }
    }

    class Program
    {
        static void Main(string[] commandLineArgs)
        {
            var args = new ProgramArgs(commandLineArgs);

            if (!args.AreValid)
            {
                Console.WriteLine(args.Errors);
                return;
            }

            using (var reader = new XmlTextReader(new FileStream(args.TestResultsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                var doc = XDocument.Load(reader);
                var tests = from suite in doc.Descendants("test-case")
                             select new
                                    {
                                        Name = AttributeValue(suite, "name"), 
                                        Time = decimal.Parse(AttributeValue(suite, "time") ?? "0")
                                    };

                var slowTests = (from t in tests
                                 orderby t.Time descending
                                 select t).Take(10);

                foreach (var test in slowTests)
                {
                    Console.WriteLine("{0}\t{1}", test.Time, test.Name);
                }
            }
        }

        private static string AttributeValue(XElement suite, string attributeName)
        {
            var attribute = suite.Attribute(attributeName);
            return attribute == null ? null : attribute.Value;
        }
    }
}
