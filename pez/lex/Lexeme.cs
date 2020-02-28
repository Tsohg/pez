using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pez.lex
{
    public enum PezLexType
    {
        id,
        _int,
        op,
        sep,
        termin,
        scoper // \t to help determine scope
    }

    /// <summary>
    /// Metadata for particular tokens
    /// </summary>
    class Lexeme
    {
        public PezLexType LType { get; private set; }
        public string token { get; private set; }
        public int line { get; private set; }
        public int pos { get; private set; }

        public Lexeme(PezLexType type, string token, int pos)
        {
            LType = type;
            this.token = token;
            this.line = line;
            this.pos = pos;
        }

        public override string ToString()
        {
            return "" + Enum.GetName(typeof(PezLexType), LType) + ": " + token;
        }
    }
}
