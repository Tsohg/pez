using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pez.lex;
using System.IO;

namespace pez.ast
{

    /*
     * https://www.crockford.com/javascript/tdop/tdop.html
     * https://en.wikipedia.org/wiki/Recursive_descent_parser
     * Suggested looking into by a friend to help with the project.
     * It seems plenty of compilers use this method of parsing.
     * I should define lexemes as symbols to expect a certain lexical structure.
     * 
     * 	https://en.wikipedia.org/wiki/Shunting-yard_algorithm
     * 	Modifying the shunting yard algorithm to create an AST.
     */

    class Parser
    {
        private Lexer lexer;

        private Dictionary<string, int> bindPowers = new Dictionary<string, int>()
        {
            { "=", 0 }, //assignment operator
            { "+", 1 },
            { "-", 1 },
            { "*", 2 },
            { "/", 2 }
        };

        //Note: only works with left->right assosiativity. for division/subtraction would probably be a good idea to swap the operands
        public Parser(string filePath)
        {
            lexer = new Lexer(System.IO.File.ReadAllText(filePath));
            Lexeme[] lexStream = lexer.GetLexStream();
            int offset = 0;

            while (offset < lexStream.Length)
            {
                //convert individual stream to ast
                //write ast to C and repeat

                int scope = GetScope(lexStream);
                offset += scope;

                //makes a subset of the lexstream from offset->terminator token. offset is the first token that isn't scoped with \t
                Lexeme[] subset = (Lexeme[])lexStream.Skip(offset).TakeWhile(termin => termin.LType != PezLexType.termin);

                //set offset to next token in the lex stream.
                offset += subset.Length;

                //ast subset
                //  apply shunting yard
                AstNode root = ShuntingYard(subset);
            }
            //debug
            //foreach (Lexeme lex in lexer.GetLexStream())
            //    Console.Out.WriteLine(lex.ToString());
            //Console.Read();
        }

        /// <summary>
        /// Returns root node of AST.
        /// </summary>
        /// <param name="subset"></param>
        /// <returns></returns>
        private AstNode ShuntingYard(Lexeme[] subset)
        {

        }

        /// <summary>
        /// Scope sled. Wheeeee!
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private int GetScope(Lexeme[] stream)
        {
            for (int i = 0; i < stream.Length; i++)
                if (!(stream[i].LType == PezLexType.scoper))
                    return i;
            return 0;
        }
    }
}
