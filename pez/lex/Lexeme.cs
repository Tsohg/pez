using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pez.lex
{
    public enum PezLexType
    {
        type,
        id,
        _int,
        op,
        sep,
        termin
    }

    /// <summary>
    /// Metadata for particular tokens
    /// </summary>
    class Lexeme
    {
        public PezLexType LType { get; private set; }
        public string token { get; private set; }

        public Lexeme(PezLexType type, string token)
        {
            LType = type;
            this.token = token;
        }

        public override string ToString()
        {
            return "" + Enum.GetName(typeof(PezLexType), LType) + ": " + token;
        }
    }
}
