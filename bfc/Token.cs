using System;
using System.Collections.Generic;

namespace bfc
{
    public class Token : ICloneable
    {
        public char Instruction { get; private set; }
        public int Count { get; private set; }

        public Token(char Instruction)
        {
            this.Instruction = Instruction;
            this.Count = 1;
        }

        public int AddCount()
        {
            return ++Count;
        }

        public object Clone()
        {
            return new Token(Instruction)
            {
                Count = Count
            };
        }

        public IEnumerable<Token> GetRepeated()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return new Token(Instruction);
            }
        }
    }
}
