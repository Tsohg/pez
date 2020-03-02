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

        private Dictionary<string, int> precedence = new Dictionary<string, int>()
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

                var linq = lexStream.Skip(offset).TakeWhile(termin => termin.LType != PezLexType.termin);
                Lexeme[] subset = linq.ToArray();

                //set offset to next token in the lex stream.
                offset += subset.Length;

                //ast subset
                //  apply shunting yard
                Queue<Lexeme> postfixNodes = ShuntingYard(subset.ToArray());

                //apply algorithm to transform the output queue to an AST.
                //TODO:  push operators to stack and operands to queue

                //debug
                while (postfixNodes.Count > 0)
                    Console.Out.Write(postfixNodes.Dequeue() + " ");
                Console.Read();

                
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
        private Queue<Lexeme> ShuntingYard(Lexeme[] subset)
        {
            //Dijkstra's Shunting Yard Algorithm
            Queue<Lexeme> output = new Queue<Lexeme>();
            Stack<Lexeme> ops = new Stack<Lexeme>();
            int offset = 0;

            while(offset < subset.Length)
            {
                Lexeme token = subset[offset];
                if (token.LType == PezLexType.id || token.LType == PezLexType._int) //id/type as operands
                {
                    output.Enqueue(token);
                    offset++;
                }
                else if(token.LType == PezLexType.op)
                {
                    //TODO: Parens must be included in this check when we handle parens in the lexer.
                    while(ops.Count > 0 && precedence[ops.Peek().token] >= precedence[token.token]) //top of stack has greater precedence than the current token. 
                        output.Enqueue(ops.Pop());
                    ops.Push(token);
                    offset++;
                }

                if(offset >= subset.Length) //if no more tokens to be read
                {
                    while(ops.Count > 0)
                        output.Enqueue(ops.Pop());
                }
            }

            return output;
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
