using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pez.types;
using System.IO;

namespace pez.lex
{

/*
 * https://www.crockford.com/javascript/tdop/tdop.html
 * https://en.wikipedia.org/wiki/Recursive_descent_parser
 * Suggested looking into by a friend to help with the project.
 * It seems plenty of compilers use this method of parsing.
 * I should define lexemes as symbols to expect a certain lexical structure.
 */

    class Parser
    {
        private Lexer lex;

        public Parser(string filePath)
        {
            lex = new Lexer(System.IO.File.ReadAllText(filePath));

            //debug
            foreach (Lexeme l in lex.GetLexStream())
                Console.Out.WriteLine(l.ToString());
            Console.Read();
        }
    }
}
