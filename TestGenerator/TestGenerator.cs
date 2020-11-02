using bfc;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace TestGenerator
{
    public class TestGenerator : bfc.ICodeGenerator
    {
        public IEnumerable<char> GenerateCode(IEnumerable<Token> Instructions)
        {
            var Indent = 0;
            var Lines = new List<string>();
            //Add the code that needs to be written before the BF code here
            Lines.Add($";Fake BF assembly generated at {DateTime.Now}");
            Lines.Add("init");

            //Process BF instructions here (or in a separate function if you prefer)
            foreach (var I in Instructions)
            {
                switch (I.Instruction)
                {
                    case '.':
                        for (var i = 0; i < I.Count; i++)
                        {
                            Lines.Add(IndentCode("put", Indent));
                        }
                        break;
                    case ',':
                        for (var i = 0; i < I.Count; i++)
                        {
                            Lines.Add(IndentCode("get", Indent));
                        }
                        break;
                    case '+':
                        Lines.Add(IndentCode($"add {I.Count}", Indent));
                        break;
                    case '-':
                        Lines.Add(IndentCode($"sub {I.Count}", Indent));
                        break;
                    case '>':
                        Lines.Add(IndentCode($"shr {I.Count}", Indent));
                        break;
                    case '<':
                        Lines.Add(IndentCode($"shl {I.Count}", Indent));
                        break;
                    case '[':
                        for (var i = 0; i < I.Count; i++)
                        {
                            Lines.Add(IndentCode("loop", Indent++));
                        }
                        break;
                    case ']':
                        for (var i = 0; i < I.Count; i++)
                        {
                            if (Indent == 0)
                            {
                                throw new ArgumentException("Bracket mismatch: Too many closing brackets");
                            }
                            Lines.Add(IndentCode("retl", --Indent));
                        }
                        break;
                    case '!':
                        //This doesn't needs to be repeated
                        Lines.Add(IndentCode("zero", Indent));
                        break;
                    default:
                        break;
                }
            }

            if (Indent > 0)
            {
                throw new ArgumentException("Bracket mismatch: Too many opening brackets");
            }

            //Add code that needs to run after the BF code
            Lines.Add("exit");
            //Return all lines as a single string
            return string.Join("\r\n", Lines);
        }

        public string GetDefaultExtension()
        {
            return "bfasm";
        }

        public string GetEngineDescription()
        {
            return "Outputs BF assembly (see help)";
        }

        public string GetEngineName()
        {
            return "bfasm";
        }

        public Version GetEngineVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        public void Help()
        {
            Console.WriteLine(@"Test generator that will output fake BF assembly code
It demonstrates:

- Generating code for BF that has been optimized
- Automatic code indentation
- Checking for unbalanced brackets

This extension has no parameters");
        }

        public void SetArguments(string[] Arguments)
        {
            throw new InvalidOperationException("This generator has no arguments");
        }

        private string IndentCode(string Line, int Count)
        {
            return string.Empty.PadRight(Count * 2) + Line;
        }
    }
}
