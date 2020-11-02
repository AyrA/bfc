using System;
using System.Collections.Generic;
using System.Linq;

namespace bfc
{
    public class CCodeGenerator : ICodeGenerator
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

        public CCodeGenerator() : this(DEFAULT_DATATYPE, DEFAULT_MEMSIZE)
        {

        }

        public CCodeGenerator(DataType DataType, int MemorySize)
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
            var Segments = new List<string>(new string[]{
                "#include <stdio.h>",
                "#include <stdint.h>",
                "int main(){",
                "\tint ptr=0;",
                "\t" + GetDataType(BFDataType) + " mem[" + BFMemorySize + "]={0};"
            });
            Segments.AddRange(Instructions.Select(m => ToCode(m)));
            Segments.Add("\treturn (int)mem[ptr];");
            Segments.Add("}");
            if (Indentation > 1)
            {
                throw new ArgumentException("Unbalanced brackets (too many opening brackets)");
            }
            return string.Join("\r\n", Segments);
        }

        private string GetDataType(DataType DT)
        {
            switch (DT)
            {
                case DataType.Byte:
                    return "uint8_t";
                case DataType.Int16:
                    return "int16_t";
                case DataType.Int32:
                    return "int32_t";
                case DataType.Int64:
                    return "int64_t";
                case DataType.UInt16:
                    return "uint16_t";
                case DataType.UInt32:
                    return "uint32_t";
                case DataType.UInt64:
                    return "uint64_t";
                default:
                    throw new ArgumentException($"Invalid data type: {DT}");
            }
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
                    return Indent(string.Concat(Enumerable.Repeat("putchar((char)mem[ptr]);", T.Count)), Indentation);
                case ',':
                    return Indent(string.Concat(Enumerable.Repeat("mem[ptr]=getchar();", T.Count)), Indentation);
                case '!':
                    return Indent("mem[ptr]=0;", Indentation);
                default:
                    throw new ArgumentException($"Unknown BF instruction: {T.Instruction}");
            }
            throw new NotImplementedException();
        }

        private string Indent(string s, int Levels)
        {
            return string.Empty.PadRight(Levels, '\t') + s;
        }

        public string GetEngineName()
        {
            return "C99";
        }

        public string GetEngineDescription()
        {
            return "C99 code generator";
        }

        public Version GetEngineVersion()
        {
            return Common.AssemblyVersion;
        }

        public string GetDefaultExtension()
        {
            return "c";
        }

        public void Help()
        {
            Console.WriteLine(@"Generates C99 compatible output for a console application
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
    }
}
