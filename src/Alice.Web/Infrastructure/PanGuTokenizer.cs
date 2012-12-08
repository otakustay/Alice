using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using PanGu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;

namespace Alice.Web.Infrastructure {
    class PanGuTokenizer : Tokenizer {
        private readonly ITermAttribute termAttribute;

        private readonly IOffsetAttribute offsetAttribute;

        private WordInfo[] words;

        private int position = -1; // 词汇在缓冲中的位置.

        private string inputText;

        static PanGuTokenizer() {
            //Init PanGu Segment.
            string config = HostingEnvironment.MapPath("~/PanGu.xml");
            Segment.Init(config);
        }

        public PanGuTokenizer(TextReader input)
            : base(input) {
            termAttribute = AddAttribute<ITermAttribute>();
            offsetAttribute = AddAttribute<IOffsetAttribute>();

            inputText = base.input.ReadToEnd();

            if (string.IsNullOrEmpty(inputText)) {
                char[] readBuf = new char[1024];

                int relCount = base.input.Read(readBuf, 0, readBuf.Length);

                StringBuilder inputStr = new StringBuilder(readBuf.Length);

                while (relCount > 0) {
                    inputStr.Append(readBuf, 0, relCount);

                    relCount = input.Read(readBuf, 0, readBuf.Length);
                }

                if (inputStr.Length > 0) {
                    inputText = inputStr.ToString();
                }
            }

            if (string.IsNullOrEmpty(inputText)) {
                words = new WordInfo[0];
            }
            else {
                global::PanGu.Segment segment = new Segment();
                ICollection<WordInfo> wordInfos = segment.DoSegment(inputText);
                words = new WordInfo[wordInfos.Count];
                wordInfos.CopyTo(words, 0);
            }
        }

        public ICollection<WordInfo> SegmentToWordInfos(string str) {
            if (string.IsNullOrEmpty(str)) {
                return new LinkedList<WordInfo>();
            }

            Segment segment = new Segment();
            return segment.DoSegment(str);
        }

        //DotLucene的分词器简单来说，就是实现Tokenizer的Next方法，把分解出来的每一个词构造为一个Token，因为Token是DotLucene分词的基本单位。
        public override bool IncrementToken() {
            ClearAttributes();

            int length = 0;
            int start = 0;

            while (true) {
                position++;
                if (position < words.Length) {
                    WordInfo word = words[position];

                    if (word == null) {
                        return false;
                    }
                    length = word.Word.Length;
                    start = word.Position;

                    termAttribute.SetTermBuffer(word.Word);
                    offsetAttribute.SetOffset(start, start + length);

                    return true;
                }
                else {
                    return false;
                }
            }
        }
    }
}