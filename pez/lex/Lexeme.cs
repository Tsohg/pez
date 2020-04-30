using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pez.lex
{
    public enum PezLexType
    {
        _int, //additional types should be put below int but above double.
        _string,
        _float,
        _double,
        unid, //unidentified lexeme type.
        id, //identifiers can be a lot of different things.
        op,
        sep,
        termin,
        scoper, // \t to help determine scope
        parameter, //for function parameters
    }

    /// <summary>
    /// Metadata for particular tokens
    /// </summary>
    class Lexeme
    {
        public PezLexType LType { get; set; }
        public string token { get; set; }

        public Lexeme(PezLexType type, string token)
        {
            LType = type;
            this.token = token;
        }

        public override string ToString()
        {
            return "{" + Enum.GetName(typeof(PezLexType), LType) + ": " + token + "}";
        }
    }
}
