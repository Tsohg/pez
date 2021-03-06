﻿using System;
using System.Collections.Generic;
using System.Linq;
using pez.lex;
using pez.dispenser.cola;
using System.Text;
using System.Text.RegularExpressions;

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
            "return" //unary op keyword.
        };

        private readonly List<string> loKeys = new List<string>() //level order tree keywords.
        {
            "func", //function declaration which is followed by fnfo and fret respectively.
            "fnfo",
            "fret"
        };

        private Dictionary<string, int> precedence = new Dictionary<string, int>()
        {
            { ")", -2}, //debugging at lowest prec
            { "(", -1},

            { "=", 0 }, //assignment operator

            { "::", 1 }, //range operator. x :: y = from x to y.

            { "==", 2 }, //boolean operators
            { "!=", 2 },
            { ">", 2 },
            { ">=", 2 },
            { "<", 2 },
            { "<=", 2 },

            { "-", 3 },
            { "+", 3 }, //mathematical operators
            { "/", 4 },
            { "*", 5 }
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

                offset += subset.Length + 1;

                if (subset.Length == 0)
                    continue;

                int scope = GetScope(subset);

                if (scope == subset.Length)
                    continue;

                Queue<Lexeme> postfixNodes = ShuntingYard(subset.ToArray(), scope);

                //apply algorithm to transform the output queue to an AST.
                Node root = null;
                Stack<Node> trees = new Stack<Node>();
                Lexeme lex = null;
                while(postfixNodes.Count > 0)
                {
                    lex = postfixNodes.Dequeue();
                    if (lex.LType != PezLexType.op)
                        trees.Push(new Node(lex));
                    else if(lex.LType == PezLexType.op)
                    {
                        Node n2 = trees.Pop();
                        Node n1 = trees.Pop();
                        Node n3 = new Node(lex);
                        n3.left = n1;
                        n3.right = n2;
                        n2.prev = n3;
                        n1.prev = n3;
                        trees.Push(n3);
                    }
                }
                if (trees.Count > 1) //keyword handling. should be the last in the input stream if not a function. if it is a function, it is at bottom of stack so we flip the stack.
                {
                    trees = FlipStack(trees);

                    if (loKeys.Contains(trees.Peek().data.token)) //identifier only tree handling. currently only the func keyword results in identifiers only.
                    {
                        //write it to the tree in order with func being at the top attatched to the tree and name of function to the left. Right subtree of func key node are parameter names.
                        //functions are handled in level order with nodes: root = "func", root.left = "name" root.right = level ordered parameter subtree.
                        Node top = new Node();
                        if (trees.Peek().data.token == "func")
                        {
                            top = trees.Pop(); //func
                            top.left = trees.Pop(); //name of function
                            top.right = BuildLevelOrderParameterTree(trees, top.right); //parameters
                        }
                        else if (trees.Peek().data.token == "fnfo" || trees.Peek().data.token == "fret")
                        {
                            top = trees.Pop(); //fnfo/fret
                            top.right = BuildLevelOrderParameterTree(trees, top.right); //level order expression tree for parameters
                        }
                        trees.Push(top);
                    }
                    else //a different kind of keyword.
                    {
                        //reflip stack
                        trees = FlipStack(trees);
                        Node topExp = trees.Pop();
                        Node key = trees.Pop();
                        key.right = topExp;
                        topExp.prev = key;
                        trees.Push(key);
                    }
                }
                root = trees.Pop();

                if (root == null) throw new Exception("Null root.");

                //search for weird trees with a left node but no right node. there should always be a right node if there is a left and vice versa.
                //Node test = root;
                //TestAST(test);

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
                Lexeme lex = subset[offset];
                if (lex.LType == PezLexType.id || IsType(lex.LType)) //id/type as operands
                {
                    output.Enqueue(lex);
                    offset++;
                }
                else if(lex.LType == PezLexType.op)
                {
                    //TODO: Parens must be included in this check when we handle parens in the lexer.
                    while(ops.Count > 0 && precedence[ops.Peek().token] >= precedence[lex.token]) //top of stack has greater precedence than the current token. 
                        output.Enqueue(ops.Pop());
                    ops.Push(lex);
                    offset++;
                }

                if (lex.LType == PezLexType.l_paren)
                {
                    ops.Push(lex);
                    offset++;
                }
                if(lex.LType == PezLexType.r_paren)
                {
                    offset++; //skip ) for next iteration
                    while(ops.Peek().LType != PezLexType.l_paren)
                        output.Enqueue(ops.Pop());
                    if (ops.Peek().LType == PezLexType.l_paren)
                        ops.Pop(); //discard (
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
        /// Inverts stack order. [a b c d e] -> [e d c b a]
        /// </summary>
        /// <param name="stack"></param>
        /// <returns></returns>
        private Stack<Node> FlipStack(Stack<Node> stack)
        {
            Stack<Node> flip = new Stack<Node>();
            while (stack.Count > 0)
                flip.Push(stack.Pop());
            return flip;
        }

        /// <summary>
        /// Builds function tree given the right node of the function node as a null root.
        /// </summary>
        /// <param name="trees"></param>
        /// <param name="ast"></param>
        /// <returns></returns>
        private Node BuildLevelOrderParameterTree(Stack<Node> trees, Node ast)
        {
            //everything left in the tree right now should be a parameter.
            Stack<Node> temp = new Stack<Node>();
            trees = FlipStack(trees);
            while (trees.Count > 0)
            {
                Node n = trees.Pop();
                n.data.LType = PezLexType.parameter;
                temp.Push(n);
            }
            trees = temp;

            Queue<Node> astq = new Queue<Node>();
            if (ast == null) ast = new Node();
            astq.Enqueue(ast);
            while(astq.Count > 0)
            {
                Node t = astq.Dequeue();
                while(trees.Count > 0)
                {
                    if (t.data == null)
                        t.data = trees.Pop().data;
                    else if (t.left == null)
                        t.left = trees.Pop();
                    else if (t.right == null)
                        t.right = trees.Pop();
                    else
                    {
                        astq.Enqueue(t.left);
                        astq.Enqueue(t.right);
                        break;
                    }
                }
            }
            return ast;
        }

        public static bool IsType(PezLexType type)
        {
            string[] en = Enum.GetNames(typeof(PezLexType));
            int t = (int)type;
            int r = -1; //max range

            for(int i = 0; i < en.Length; i++)
            {
                if (en[i] == Enum.GetName(typeof(PezLexType), PezLexType._double))
                {
                    r = i;
                    break;
                }
            }
            if (t < r) return true; //_int should ALWAYS be @ 0 in PezLexType.
            else return false;
        }

        public static PezLexType FindLexDataType(Node root)
        {
            PezLexType type = PezLexType.unid;
            Queue<Node> nq = new Queue<Node>();
            nq.Enqueue(root);
            int idTally = 0;
            while (nq.Count > 0)
            {
                Node n = nq.Dequeue();

                if (n.data.LType == PezLexType.id)
                    idTally++;

                if (IsType(n.data.LType))
                    return n.data.LType;

                if (n.left != null)
                    nq.Enqueue(n.left);

                if (n.right != null)
                    nq.Enqueue(n.right);
            }
            if (idTally > 1) //identifier in the case that only variables are used in operations rather than types. should be > 1 because assignment ops always have at least 1 identifier.
                return PezLexType.id;
            Console.Out.WriteLine(type.ToString());
            return type;
        }

        /// <summary>
        /// Gets the data type of the very first variable in the expression. Might want to replace with a more sophisticated type checking system later.
        /// Right now it finds the type of first variable and assume the rest of the expression lines up properly.
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="vars"></param>
        /// <returns></returns>
        public static PezLexType GetTypeOfFirstVar(Node exp, List<Lexeme> vars) //exp is right subtree of assignment operation ast.
        {
            PezLexType type = PezLexType.unid;
            Queue<Node> nq = new Queue<Node>();
            nq.Enqueue(exp);
            while (nq.Count > 0)
            {
                Node n = nq.Dequeue();

                for (int i = 0; i < vars.Count; i++)
                    if (vars[i].token == n.data.token)
                        return vars[i].LType;

                if (n.left != null)
                    nq.Enqueue(n.left);

                if (n.right != null)
                    nq.Enqueue(n.right);
            }
            return type;
        }

        /// <summary>
        /// Returns heavily parenthesized mathematical expression from given assignment operation subtree.
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public static string GetInfixExpression(Node exp)
        {
            if (exp.left == null && exp.right == null) //1 element referenced. expression is itself.
                return exp.data.token;
            if ((exp.left == null && exp.right != null) || exp.left != null && exp.right == null)
                throw new Exception("Expression does not have the correct number of operators/operands.");
            //TODO: might want to do more tree checking...

            string template = "(?L op ?R)";
            string result = template;
            Regex r = new Regex("op");
            result = r.Replace(result, exp.data.token, 1); //*should be an op
            //go left and right
            GetInfixRecursive(ref result, template, "[?][L]", exp.left);
            GetInfixRecursive(ref result, template, "[?][R]", exp.right);

            return result.ToString();
        }

        private static void GetInfixRecursive(ref string result, string template, string where, Node n)
        {
            Regex r = new Regex(where);
            if (n.data.LType != PezLexType.op)
            {
                result = r.Replace(result, n.data.token, 1);
                return;
            }
            result = r.Replace(result, template, 1);
            r = new Regex("op");
            result = r.Replace(result, n.data.token, 1);
            GetInfixRecursive(ref result, template, "[?][L]", n.left);
            GetInfixRecursive(ref result, template, "[?][R]", n.right);
        }

        /// <summary>
        /// Deprecated. Only here for debug purposes.
        /// </summary>
        /// <param name="ast"></param>
        public static void InOrderWrite(Node ast, ref StringBuilder source) //TODO: Fix InOrderWrite for parenthesis expressions. Move InOrderWrite to BaseTranslate if possible.
        {
            if (ast == null) return;

            InOrderWrite(ast.left, ref source);

            source.Append(ast.data.token);

            InOrderWrite(ast.right, ref source);
        }

        /// <summary>
        /// Returns an array of lexemes in level order using the given tree.
        /// </summary>
        /// <param name="ast"></param>
        /// <returns></returns>
        public static Lexeme[] LevelOrderTraversal(Node ast)
        {
            Queue<Node> nq = new Queue<Node>();
            nq.Enqueue(ast);

            List<Lexeme> lexstream = new List<Lexeme>();

            while (nq.Count > 0)
            {
                Node n = nq.Dequeue();
                lexstream.Add(n.data);

                if (n.left != null)
                    nq.Enqueue(n.left);

                if (n.right != null)
                    nq.Enqueue(n.right);
            }

            return lexstream.ToArray();
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
