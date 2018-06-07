using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace DramaPPTGenerator
{
    class Program
    {
        static readonly char[] Seperators =
        {
            //'，',
            '。',
            //'！',
            //'？',
            //'；',
            '.',
            //',',
            //'!',
            //'?'
        };
        static readonly char[] Periods =
        {
            '，',
            '。',
            '！',
            '？',
            '；',
            '.',
            ',',
            '!',
            '?',
            '—'
        };

        static List<string> CharactorName = new List<string>();
        static List<Dialogue> PlotDialogues = new List<Dialogue>();

        public static void PrepareCharactorName()
        {
            FileStream namelist = new FileStream("namelist.txt", FileMode.Open, FileAccess.Read);
            StreamReader readnamelist = new StreamReader(namelist);
            do
            {
                string chinesename = readnamelist.ReadLine();
                string englishname = readnamelist.ReadLine();
                CharactorName.Add(chinesename);
                CharactorName.Add(englishname.ToLower());
            } while (!readnamelist.EndOfStream);
            readnamelist.Close();
        }

        public static void ReadPlot(StreamReader chinese, StreamReader english)
        {
            string currentCharactorEnglish = english.ReadLine();
            string currentCharactorChinese = chinese.ReadLine();
            do
            {
                Dialogue currentDialogue = new Dialogue();
                currentDialogue.Charactor = new Charactor(currentCharactorChinese, currentCharactorEnglish);
                currentDialogue.Chinese = ReadPlot(chinese, out currentCharactorChinese);
                currentDialogue.English = ReadPlot(english, out currentCharactorEnglish);
                PlotDialogues.Add(currentDialogue);
            } while (!(chinese.EndOfStream && english.EndOfStream));
        }

        public static List<string> ReadPlot(StreamReader reader, out string nextCharactor)
        {
            List<string> dialogues = new List<string>();
            string dialogue = string.Empty;
            while ((dialogue = reader.ReadLine()) != null && !IsCharactorName(dialogue))
            {
                dialogues.Add(dialogue);
            }
            dialogues.RemoveAll((line) => (line == string.Empty));
            nextCharactor = dialogue;
            return dialogues;
        }

        public static void ProcessPlot(StreamWriter writer)
        {
            writer.WriteLine(LaTeX.LaTeXBeginning);
            foreach (Dialogue dialogue in PlotDialogues)
            {
                if (dialogue.Chinese.Count != dialogue.English.Count)
                {
                    dialogue.Balance();
                }
                for (int i = 0; i < dialogue.Chinese.Count; i++)
                {
                    string chinesedialogue = dialogue.Chinese[i];
                    string englishdialogue = dialogue.English[i];
                    string content = chinesedialogue + "&" + englishdialogue + LaTeX.LaTeXEOL;
                    writer.WriteLine(LaTeX.GenerateFrameWithTable(dialogue.Charactor.Chinese, dialogue.Charactor.English, content));
                }
            }
            writer.WriteLine(LaTeX.LaTeXEnd);
        }

        public static bool IsCharactorName(string line)
        {
            return CharactorName.Contains(line.ToLower());
        }

        public static bool IsEndWithPeriod(string sentence)
        {
            for (int i = 0; i < Periods.Length; i++)
            {
                if (sentence[sentence.Length - 1] == Periods[i])
                {
                    return true;
                }
            }
            return false;
        }

        class Dialogue
        {
            private List<string> _chinese;
            private List<string> _english;

            public Charactor Charactor { get; set; }
            public List<string> Chinese { get => _chinese; set => _chinese = value; }
            public List<string> English { get => _english; set => _english = value; }
            public void DivideIntoSentences()
            {
                Chinese = DivideIntoSentences(Chinese, "。", maxCharactorNumber: 30);
                English = DivideIntoSentences(English, ". ", maxCharactorNumber: 200);
            }
            private static List<string> DivideIntoSentences(List<string> dialogue, string period, int maxCharactorNumber)
            {
                List<string> divided = new List<string>();
                foreach (string line in dialogue)
                {
                    Queue<string> nextlines = new Queue<string>(line.Split(Seperators, StringSplitOptions.RemoveEmptyEntries).ToList());
                    List<string> lineDivided = DivideIntoSentences(nextlines, maxCharactorNumber, period);
                    divided.AddRange(lineDivided);
                }
                return divided;
            }
            private static List<string> DivideIntoSentences(Queue<string> nextLines, int maxCharactorNumber, string period)
            {
                List<string> linesDivided = new List<string>();
                StringBuilder lineDivided = new StringBuilder();
                while (nextLines.Count != 0)
                {
                    if (lineDivided.Length >= maxCharactorNumber)
                    {
                        linesDivided.Add(lineDivided.ToString());
                        lineDivided.Clear();
                        continue;
                    }
                    lineDivided.Append(AddPeriod(nextLines.Dequeue().Trim(' '), period));
                }
                linesDivided.Add(lineDivided.ToString());
                return linesDivided;
            }
            private static string AddPeriod(string sentence, string period)
            {
                if (!IsEndWithPeriod(sentence))
                {
                    sentence += period;//string is a value type, cannot use foreach.
                }
                else
                {
                    sentence += " ";
                }
                return sentence;
            }
            private static void FindLarger
                (ref List<string> first, ref List<string> second, out List<string> larger, out List<string> smaller)
            {
                if (first.Count > second.Count)
                {
                    larger = first;
                    smaller = second;
                }
                else
                {
                    larger = second;
                    smaller = first;
                }
            }
            public void Balance()
            {
                List<string> larger, smaller;
                FindLarger(ref _english, ref _chinese, out larger, out smaller);
                int last = smaller.Count;
                StringBuilder lastdialogue = new StringBuilder(larger[last - 1]);
                larger.GetRange(last, larger.Count - last).ForEach((sentence) => lastdialogue.Append(sentence));
                larger[last - 1] = lastdialogue.ToString();
                larger.RemoveRange(last, larger.Count - last);
            }
        }

        static class LaTeX
        {
            static public readonly string LaTeXBeginning = "\\documentclass{beamer}\n\\usepackage[UTF8,noindent]{ctexcap}\n\\usepackage{tabularx}\n\\begin{document}\n";
            static public readonly string LaTeXEnd = "\\end{document}";
            static public readonly string LaTeXFrameBeginning = "\\begin{frame}{CHINESE}{ENGLISH}\n\\begin{tabularx}{\\textwidth}{XX}\n";
            static public readonly string LaTexFrameEnd = "\\end{tabularx}\n\\end{frame}";
            static private readonly string ChinesePlaceholder = "CHINESE";
            static private readonly string EnglishPlaceholder = "ENGLISH";
            static public readonly string LaTeXEOL = "\\\\";

            static public string GenerateFrameWithTable(string chineseName, string englishName, string content)
            {
                StringBuilder output = new StringBuilder(LaTeXFrameBeginning);
                output.Replace(ChinesePlaceholder, chineseName);
                output.Replace(EnglishPlaceholder, englishName);
                output.AppendLine(content);
                output.Append(LaTexFrameEnd);
                return output.ToString();
            }
        }

        class Charactor
        {
            public string Chinese { get; private set; }
            public string English { get; private set; }
            public Charactor(string chinese, string english)
            {
                Chinese = chinese;
                English = english;
            }
        }

        static void Main(string[] args)
        {
            FileStream cnplot = new FileStream("cnplot.txt", FileMode.Open, FileAccess.Read);
            FileStream enplot = new FileStream("enplot.txt", FileMode.Open, FileAccess.Read);
            FileStream output = new FileStream("main.tex", FileMode.Create, FileAccess.Write);
            StreamReader cnread = new StreamReader(cnplot);
            StreamReader enread = new StreamReader(enplot);
            StreamWriter write = new StreamWriter(output);

            PrepareCharactorName();

            ReadPlot(cnread, enread);

            PlotDialogues.ForEach((dialogue) => dialogue.DivideIntoSentences());

            ProcessPlot(write);

            write.Close();
        }
    }
}
