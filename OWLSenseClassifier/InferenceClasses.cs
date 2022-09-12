using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OWLSenseClassifier
{
    internal class InferenceClasses
    {
        static List<string> classes = new List<string>() {"ignore","background","GGOW M T", "GGOW F C" };
        public static List<string> GetClasses()
        {
            return classes;
        }
    }
}
