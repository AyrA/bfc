using System;
using System.Collections.Generic;
using System.Linq;

namespace bfc
{
    /// <summary>
    /// Generates C# source code for a console application
    /// </summary>
    public class CSharpGenerator : ICodeGenerator
    {
        public const int DEFAULT_MEMSIZE = 30000;
        public const DataType DEFAULT_DATATYPE = DataType.Byte;

        private int Indentation;

        public DataType BFDataType
        {
            get; private set;
        }

        public int BFMemorySize
        {
            get; private set;
        }

        public CSharpGenerator() : this(DataType.Byte, 30000)
        {

        }

        public CSharpGenerator(DataType DataType, int MemorySize)
        {
            if (MemorySize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(MemorySize));
            }
            if (!Enum.IsDefined(DataType.GetType(), DataType))
            {
                throw new ArgumentOutOfRangeException(nameof(DataType));
            }
            BFDataType = DataType;
            BFMemorySize = MemorySize;
        }

        public IEnumerable<char> GenerateCode(IEnumerable<Token> Instructions)
        {
            Indentation = 1;
            var Segments = new List<string>() {
                "using System;",
                "public static class BF",
                "{",
                "\tpublic static int Main()",
                "\t{",
                "\t\tint ptr=0;",
                "\t\tvar mem=new " + BFDataType.ToString() + $"[{BFMemorySize}];"
            };
            Segments.AddRange(Instructions.Select(m => ToCode(m)));
            Segments.AddRange(new string[] {
                "\t\treturn (int)mem[ptr];",
                "\t}",
                "}"
            });
            return string.Join("\r\n", Segments);
        }

        public string GetEngineName()
        {
            return "C#";
        }

        public string GetEngineDescription()
        {
            return "C# code generator";
        }

        public Version GetEngineVersion()
        {
            return Common.AssemblyVersion;
        }

        public string GetDefaultExtension()
        {
            return "cs";
        }

        public void Help()
        {
            Console.WriteLine(@"Generates C# compatible output for a console application
Argument 1: data type
Argument 2: virtual memory size (number of memory cells)

Defaults: {0}, {1}

Available data types:", DEFAULT_DATATYPE, DEFAULT_MEMSIZE);
            Console.WriteLine(string.Join(", ", Enum.GetNames(typeof(DataType))));
        }

        public void SetArguments(string[] Arguments)
        {
            if (Arguments.Length != 2)
            {
                throw new InvalidOperationException("This code generator requires exactly two arguments");
            }

            if (!Enum.TryParse(Arguments[0], out DataType type))
            {
                throw new ArgumentException("Data type argument size is invalid");
            }
            if (!int.TryParse(Arguments[1], out int mem) || mem < 1)
            {
                throw new ArgumentException("Memory size argument is invalid");
            }
            BFDataType = type;
            BFMemorySize = mem;
        }

        private string ToCode(Token T)
        {
            switch (T.Instruction)
            {
                case '>':
                    return Indent(T.Count == 1 ? "++ptr;" : $"ptr+={T.Count};", Indentation);
                case '<':
                    return Indent(T.Count == 1 ? "--ptr;" : $"ptr-={T.Count};", Indentation);
                case '+':
                    return Indent(T.Count == 1 ? "++mem[ptr];" : $"mem[ptr]+={T.Count};", Indentation);
                case '-':
                    return Indent(T.Count == 1 ? "--mem[ptr];" : $"mem[ptr]-={T.Count};", Indentation);
                case '[':
                    Indentation += T.Count;
                    return Indent(string.Concat(Enumerable.Repeat("while(mem[ptr]!=0){", T.Count)), Indentation - T.Count);
                case ']':
                    if (Indentation < 2)
                    {
                        throw new ArgumentException("Unbalanced brackets (too many closing brackets)");
                    }
                    return Indent(string.Concat(Enumerable.Repeat("}", T.Count)), Indentation -= T.Count);
                case '.':
                    return Indent(string.Concat(Enumerable.Repeat("Console.Write((char)mem[ptr]);", T.Count)), Indentation);
                case ',':
                    return Indent(string.Concat(Enumerable.Repeat("mem[ptr]=Console.ReadKey(true).KeyCode;", T.Count)), Indentation);
                case '!':
                    return Indent("mem[ptr]=0;", Indentation);
                default:
                    throw new ArgumentException($"Unknown BF instruction: {T.Instruction}");
            }
            throw new NotImplementedException();
        }
        private string Indent(string s, int Levels)
        {
            return string.Empty.PadRight(Levels + 1, '\t') + s;
        }
    }
}
