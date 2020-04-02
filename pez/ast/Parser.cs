﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pez.lex;
using pez.dispenser.cola;
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
     * 	Uses the shunting yard algorithm to put the expression into postfix. then using my own algorithm, convert the output of shunting yard into an ast.
     */

    class Parser
    {
        private Lexer lexer;
        private List<Tuple<Node, int>> astAndScope; //if the first node of the ast = "if", then we expect the scope of the preceding statements to be (if).scope+1. if statement body ends on scope-1.

        private readonly List<string> keywords = new List<string>()
        {
            "if",
            "lpw", //loop while
            "lpf", //loop for
            "else",
        };

        private Dictionary<string, int> precedence = new Dictionary<string, int>()
        {
            { "=", 0 }, //assignment operator

            { "==", 1 }, //boolean operators
            { "!=", 1 },
            { ">", 1 },
            { ">=", 1 },
            { "<", 1 },
            { "<=", 1 },

            { "+", 2 }, //mathematical operators
            { "-", 2 },
            { "*", 3 },
            { "/", 3 }
        };

        //Note: only works with left->right assosiativity. for division/subtraction would probably be a good idea to swap the operands
        public Parser(string filePath, string outPath, string lang)
        {
            lexer = new Lexer(System.IO.File.ReadAllText(filePath));
            astAndScope = new List<Tuple<Node, int>>();

            Lexeme[] lexStream = lexer.GetLexStream();
            int offset = 0;

            while (offset < lexStream.Length) //per statement
            {
                //convert individual stream to ast
                //write ast to C and repeat

                //makes a subset of the lexstream from offset->terminator token. offset is the first token that isn't scoped with \t

                var linq = lexStream.Skip(offset).TakeWhile(termin => termin.LType != PezLexType.termin);
                Lexeme[] subset = linq.ToArray();

                //set offset to next token in the lex stream.
                offset += subset.Length + 1;

                if (subset.Length == 0)
                    continue;

                int scope = GetScope(subset);

                if (scope == subset.Length)
                    continue;

                //ast subset
                //  apply shunting yard
                Queue<Lexeme> postfixNodes = ShuntingYard(subset.ToArray(), scope);

                //apply algorithm to transform the output queue to an AST.

                //my own algorithm for converting a postfix expression to an ast.
                //TODO: bugfix this algorithm.
                Stack<Lexeme> operators = new Stack<Lexeme>();
                Queue<Lexeme> operands = new Queue<Lexeme>();

                while(postfixNodes.Count > 0)
                {
                    Lexeme l = postfixNodes.Dequeue();
                    if (l.LType == PezLexType.op)
                        operators.Push(l);
                    else
                        operands.Enqueue(l);
                }

                Node node = new Node();
                Node root = null;

                while (operands.Count > 0 || operators.Count > 0)
                {
                    if(keywords.Contains(operands.Peek().token)) //if we encounter a keyword like if/while.
                    {
                        node.data = operands.Dequeue();
                        root = node;
                        if (node.data.token == "else") //a 1 line else statement. break loop and return.
                            break;
                        node.right = new Node();
                        node.right.prev = node;
                        node = node.right;
                        continue;
                    }

                    if (node.data == null && operators.Count > 0)
                    {
                        node.data = operators.Pop();
                        if (root == null)
                            root = node;
                    }

                    if(node.left == null && operands.Count > 0)
                    {
                        node.left = new Node();
                        node.left.data = operands.Dequeue();
                        node.left.prev = node;
                    }

                    if(node.right == null && operators.Count > 0)
                    {
                        node.right = new Node();
                        node.right.data = operators.Pop();
                        node.right.prev = node;
                    }
                    else if(node.right == null && operands.Count > 0)
                    {
                        node.right = new Node();
                        node.right.data = operands.Dequeue();
                        node.right.prev = node;
                    }
                    node = node.right;
                }

                if (root == null) throw new Exception("Null root.");

                //search for weird trees with a left node but no right node. there should always be a right node if there is a left and vice versa.
                Node test = root;
                TestAST(test);

                astAndScope.Add(new Tuple<Node, int>(root, scope)); //associates a statement with it's scope.
            }
            //debug
            //foreach (Lexeme lex in lexer.GetLexStream())
            //    Console.Out.WriteLine(lex.ToString());
            //Console.Read();
            //Console.Out.WriteLine("end?");
            switch (lang)
            {
                case "c":
                    TranslateC trc = new TranslateC(astAndScope, outPath);
                    trc.Translate();
                    break;
                default:
                    throw new Exception("Language is either not supported or invalid language argument to pez. Example: pez inFilePath outFilePath c");
            }
        }

        /// <summary>
        /// Returns root node of AST.
        /// </summary>
        /// <param name="subset"></param>
        /// <returns></returns>
        private Queue<Lexeme> ShuntingYard(Lexeme[] subset, int scope)
        {
            //Dijkstra's Shunting Yard Algorithm
            Queue<Lexeme> output = new Queue<Lexeme>();
            Stack<Lexeme> ops = new Stack<Lexeme>();
            int offset = 0 + scope; //skip scopers

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

        private void TestAST(Node root)
        {
            bool error = false;

            //visit self
            if (root.data == null)
                error = true;

            while (true)
            {
                //visit left and right
                if (root.left == null && root.right == null) //if both are null, we hit the last node.
                    break;

                if (root.right == null) //if one is null, we have a weird tree.
                    error = true;

                if (root.left == null && !keywords.Contains(root.data.token)) //another keyword test.
                    error = true;

                if (error) throw new Exception("Weird Tree error on line: " + (astAndScope.Count + 1));

                root = root.right;
            }
            if (error) throw new Exception("Weird Tree error on line: " + astAndScope.Count + 1);
        }

        /// <summary>
        /// Scope sled. Wheeeee!
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private int GetScope(Lexeme[] stream)
        {
            int i = 0;
            for (; i < stream.Length; i++)
                if (!(stream[i].LType == PezLexType.scoper))
                    return i;
            return i;
        }
    }
}
