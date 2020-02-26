using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pez.lex
{
    class PezSym
    {
        public char[] operators = new char[]
        {
            '=', //assign operator
            '+',
            '-',
            '*',
            '/'
        };

        public char[] separators = new char[]
        {
            ' '
        };

        public string[] terminators = new string[]
        {
            "\r\n",
            ";"
        };

        public string[] types = new string[]
        {
            "int"
        };

        public bool ExpectInteger(string token)
        {
            return int.TryParse(token, out int res);
        }

        public bool ExpectType(string token)
        {
            return Compare(token, types);
        }

        public bool ExpectOperator(string token)
        {
            return Compare(token, operators);
        }

        public bool ExpectTerminator(string token)
        {
            return Compare(token, terminators);
        }

        public bool ExpectSeparator(string token)
        {
            return Compare(token, separators);
        }

        public bool ExpectIdentifier(string token)
        {
            //if it is NOT a digit, type, separator, terminator, or operator, then it is an identifier.
            if (char.IsDigit(token[0]) || ExpectTerminator(token) || ExpectOperator(token) || ExpectType(token) || ExpectSeparator(token))
                return false;
            else return true;
        }

        private bool Compare(string token, string[] set)
        {
            foreach (string s in set)
                if (token == s)
                    return true;
            return false;
        }

        private bool Compare(string token, char[] set)
        {
            if (!char.TryParse(token, out char res))
                return false; //if not a char, return false.
            foreach (char s in set)
                if (res == s)
                    return true;
            return false;
        }
    }
}
