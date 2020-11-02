using System;
using System.Collections.Generic;
using System.Linq;

namespace bfc
{
    /// <summary>
    /// Generates x86 assembly for a single segment DOS executable
    /// </summary>
    public class DOSAssemblyGenerator : ICodeGenerator
    {
        //About the DOS memory layout for single segment executables
        //==========================================================
        //The memory layout is rather simple.
        //The entire application only has a single page to work with (from 0x0000 to 0xFFFF)
        //DOS will write 0xFF bytes to the lower end of the memory.
        //This block of data contains various information for the executable to use.
        //The code starts at offset 0x100.
        //After the code ends, free memory begins immediately up to the end of the page.
        //The end of the memory page also contains the stack.
        //Because everything is in a single page, there are a few things you must consider:
        //- The amount of free memory depends on the code size and the stack size
        //- Writing too close to the end of the page can overwrite values on the stack
        //- Pushing too many bytes onto the stack can overwrite memory and code
        //- Code can be overwritten at runtime because it's not protected
        //
        //About BF
        //========
        //BF does not uses the stack, therefore we are only concerned about the general memory.
        //The original BF compiler provided 30'000 bytes of memory.
        //This means if your *compiled* code size exceeds about 30'000 bytes, you might end up with less memory than optimal.
        //How much memory is really needed depends on your program, but usually less than 100 bytes are used.
        //The size of a memory cell is 8 bits.
        //Memory values wrap around, the memory pointer does not
        //
        //About the generated code
        //========================
        //The generated code will not abort on CTRL+C or CTRL+Z or end of input. Please check for those cases manually.

        private readonly Stack<string> LabelStack;
        private int CurrentLabel = 0;

        public DOSAssemblyGenerator()
        {
            LabelStack = new Stack<string>();
        }

        private string GetLabel()
        {
            return $"l{++CurrentLabel}";
        }

        public IEnumerable<char> GenerateCode(IEnumerable<Token> Instructions)
        {
            LabelStack.Clear();
            var Lines = new List<string>();
            Lines.AddRange(new string[] {
                ";Generated for FASM",
                "org 0100h",
                "; Zero all unused memory (from .mem upwards)",
                "mov bp,.mem",
                ".clear:",
                "mov [bp],byte 0",
                "inc bp",
                WithComment("jz .run", "Exit the loop once the counter overflows", 0),
                "jmp .clear",
                ".run:",
                WithComment("mov bp,.mem", "Initial memory location (ptr=0)", 0)
            });
            Lines.AddRange(Instructions.SelectMany(m => ToCode(m)));
            Lines.AddRange(new string[] {
                ";DOS Exit call",
                "mov ah,4Ch",
                WithComment("mov al,byte [bp]", "return mem[ptr];", 0),
                "int 21h",
                "; == Utility functions ==",
                "",
                ";Write a single character to STDOUT",
                ".putchar:",
                "mov dl,[bp]",
                "mov ah,02h",
                "int 21h",
                "ret",
                "",
                ";Read a single character from STDIN",
                ".getchar:",
                "mov ah,01h",
                "int 21h",
                "mov [bp],al",
                "ret",
                "",
                "; == Start of memory ==",
                "",
                ".mem:",
                $"db \"Compiled by {GetEngineDescription()} {GetEngineVersion()}\""
            });
            if (LabelStack.Count > 0)
            {
                throw new ArgumentException("Unbalanced brackets (too many opening brackets)");
            }
            return Multi(Lines.ToArray());
        }

        public string GetEngineName()
        {
            return "DOS";
        }

        public string GetEngineDescription()
        {
            return "DOS single segment assembly generator";
        }

        public Version GetEngineVersion()
        {
            return Common.AssemblyVersion;
        }

        public string GetDefaultExtension()
        {
            return "asm";
        }

        public void Help()
        {
            Console.WriteLine(@"Generates real mode single page DOS assembly code (for .com file)
This generator has no custom arguments.
Memory cell size is always 8 bits.
The number of bytes depends on the size of the source code.
A single page is 65536 (0x10000) bytes long.

Subtracting the size of the *compiled* assembly code
and the 0x100 long header will return the available memory.

The output is compatible with the flat assembler (FASM).");
        }

        public void SetArguments(string[] Arguments)
        {
            throw new InvalidOperationException("This code generator does not supports custom arguments");
        }

        private IEnumerable<string> ToCode(Token T)
        {
            List<string> instructions = new List<string>();
            string label;
            switch (T.Instruction)
            {
                case '>':
                    if (T.Count > 0xFFFF)
                    {
                        throw new ArgumentException($"BF instruction '{T.Instruction}' repeated too often");
                    }
                    instructions.Add(T.Count == 1 ?
                        WithComment("inc bp", "++ptr;", LabelStack.Count) :
                        WithComment($"add bp,{T.Count}", $"ptr+={T.Count};", LabelStack.Count));
                    break;
                case '<':
                    if (T.Count > 0xFFFF)
                    {
                        throw new ArgumentException($"BF instruction '{T.Instruction}' repeated too often");
                    }
                    instructions.Add(T.Count == 1 ?
                        WithComment("dec bp", "--ptr;", LabelStack.Count) :
                        WithComment($"sub bp,{T.Count}", $"ptr-={T.Count};", LabelStack.Count));
                    break;
                case '+':
                    instructions.Add(T.Count % 256 == 1 ?
                        WithComment("inc byte [bp]", "++mem[ptr];", LabelStack.Count) :
                        WithComment($"add [bp], byte {T.Count}", $"mem[ptr]+={T.Count};", LabelStack.Count));
                    break;
                case '-':
                    instructions.Add(T.Count % 256 == 1 ?
                        WithComment("dec byte [bp]", "--mem[ptr];", LabelStack.Count) :
                        WithComment($"sub [bp], byte {T.Count}", $"mem[ptr]-={T.Count};", LabelStack.Count));
                    break;
                case '[':
                    for (var i = 0; i < T.Count; i++)
                    {
                        label = GetLabel();
                        LabelStack.Push(label);
                        if (i == 0)
                        {
                            instructions.AddRange(new string[] {
                                "",
                                "mov al,byte [bp]",
                                "test al,al",
                                WithComment($"jz .e{label}", "while(mem[ptr]){", LabelStack.Count - 1),
                                $".s{label}:"
                            });
                        }
                        else
                        {
                            //We don't need to test the same register multiple times
                            instructions.Add(WithComment($".s{label}:", "while(mem[ptr]){", LabelStack.Count - 1));
                        }
                    }
                    break;
                case ']':
                    for (var i = 0; i < T.Count; i++)
                    {
                        if (LabelStack.Count == 0)
                        {
                            throw new ArgumentException("Unbalanced brackets (too many closing brackets)");
                        }
                        label = LabelStack.Pop();
                        if (i == 0)
                        {
                            instructions.AddRange(new string[] {
                                "",
                                "mov al,byte [bp]",
                                "test al,al",
                                WithComment($"jnz .s{label}", "}", LabelStack.Count),
                                $".e{label}:" });
                        }
                        else
                        {
                            //We don't need to test the same register multiple times
                            instructions.Add(WithComment($".e{label}:", "}", LabelStack.Count));
                        }
                    }
                    break;
                case '.':
                    instructions.AddRange(Enumerable.Repeat(WithComment("call .putchar", "putchar(mem[ptr]);", LabelStack.Count), T.Count));
                    break;
                case ',':
                    instructions.AddRange(Enumerable.Repeat(WithComment("call .getchar", "mem[ptr]=getchar();", LabelStack.Count), T.Count));
                    break;
                case '!':
                    //No need to ever repeat this instruction
                    instructions.Add(WithComment("mov [bp],byte 0", "mem[ptr]=0; //Original: [-]", LabelStack.Count));
                    break;
                default:
                    throw new ArgumentException($"Unknown BF instruction: {T.Instruction}");
            }
            return instructions;
        }

        private static string WithComment(string Instruction, string Comment, int Padding)
        {
            return Instruction.PadRight(30) + ";" + string.Empty.PadRight(Padding * 4) + Comment;
        }

        private static string Multi(params string[] Instructions)
        {
            return string.Join("\r\n", Instructions);
        }
    }
}
