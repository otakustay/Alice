using Lucene.Net.Analysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Alice.Web.Infrastructure {
    class PanGuAnalyzer : Analyzer {
        public override TokenStream TokenStream(string fieldName, TextReader reader) {
            TokenStream result = new PanGuTokenizer(reader);
            result = new LowerCaseFilter(result);
            return result;
        }
    }
}