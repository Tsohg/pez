using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pez.ast;
using System.IO;

namespace pez.dispenser.cola
{
    /// <summary>
    /// Translates a pez program to C.
    /// </summary>
    class TranslateC : BaseTranslate
    {
        private StringBuilder source = new StringBuilder("void main(){\n"); //Currently i will just dump the contents of the code inside the main function of a C program.
        private int offset = 0;

        public TranslateC(List<Tuple<Node, int>> astAndScope, string outPath) : base(astAndScope, outPath) { }
        
        public override void Translate()
        {
            while(offset < astAndScope.Count)
                Write(offset);
            source.Append("}"); //ending brace for void main()
            System.IO.File.WriteAllText(outPath, source.ToString());
        }

        private void AppendScope(int scope)
        {
            for (int i = 0; i < scope; i++) //proper indentation.
                source.Append("\t");
        }

        private void Write(int index)
        {
                Node ast = astAndScope[index].Item1;
                int mScope = astAndScope[index].Item2 + 1; //necessary scope for the main function of C. we use scope for tab indentations.

                switch (ast.data.token)
                {
                    case "=": //assignment
                        WriteAssignment(ast, mScope);
                        offset++;
                        break;

                    case "if": //if statement
                    case "lpw": //while loop
                        WriteConditional(ast, mScope);
                        break;
                    case "lpf": //for loops have to be handled separately.      lpf X to Y       will be the syntax.
                        break;
                    case "else": //if->else
                        //source.AppendLine("else\n{\n"); //TODO: Finish the block with a function call.
                        break;

                    default:
                        throw new Exception("TranslateC:Write:: Errorneous first node.");
                }
        }

        /// <summary>
        /// Writes assignment statements in which the variable is both declared and assigned a value.
        /// </summary>
        /// <param name="ast"></param>
        /// <param name="scope"></param>
        private void WriteAssignment(Node ast, int scope)
        {
            AppendScope(scope);
            source.Append("int "); //for now, we are only doing integers so this is the type.
            source.Append(ast.left.data.token + " "); //var name
            source.Append(ast.data.token + " "); //assignment operator

            //simplify the expression if it is some kind of static formula to its represented value. (This is a luajit 2.0 optimization as well).

            #region deprecated
            Node exp = ast.right; //This subset tree of the assignment ast is the expression/value.
            exp.prev = null;
            Stack<int> stack = new Stack<int>();
            int result;
            while (!int.TryParse(exp.data.token, out result))
            {
                while (exp.right.data.LType == lex.PezLexType.op) //navigate to end of tree
                    exp = exp.right;

                int a;
                int b;
                int c;
                stack.Push(int.Parse(exp.left.data.token));
                stack.Push(int.Parse(exp.right.data.token));
                exp.left = null;
                exp.right = null;
                //apply operation and set current node to its value.
                switch (exp.data.token)
                {
                    case "+":
                        c = stack.Pop() + stack.Pop();
                        exp.data.token = c.ToString();
                        //stack.Push(c);
                        break;
                    case "-":
                        b = stack.Pop();
                        a = stack.Pop();
                        c = b - a;
                        exp.data.token = c.ToString();
                        //stack.Push(c);
                        break;
                    case "*":
                        c = stack.Pop() * stack.Pop();
                        exp.data.token = c.ToString();
                        //stack.Push(c);
                        break;
                    case "/":
                        b = stack.Pop();
                        a = stack.Pop();
                        c = b / a;
                        exp.data.token = c.ToString();
                        //stack.Push(c);
                        break;
                    default:
                        break;
                }
                exp.data.LType = lex.PezLexType._int;
                if(exp.prev != null)
                    exp = exp.prev;
            }
            #endregion

            source.AppendLine(result + ";");
        }

        /// <summary>
        /// Writes If and While statements.
        /// </summary>
        /// <param name="ast"></param>
        /// <param name="scope"></param>
        private void WriteConditional(Node ast, int scope)
        {
            AppendScope(scope);
            string keyword;
            Node temp = ast;
            //for now, we will throw a fit if the tree is bigger than: X op Y.
            switch(temp.data.token)
            {
                case "if":
                    keyword = "if(";
                    break;
                case "lpw":
                    keyword = "while(";
                    break;
                default: throw new Exception("TranslateC:WriteConditional:: Errorneous first node.");
            }
            source.Append(keyword);
            temp = temp.right;
            source.Append(temp.left.data.token + " " + temp.data.token + " " + temp.right.data.token); //expression
            source.Append("){\n");
            offset++; //next line after conditional expression.
            //write until out of scope.
            while (offset < astAndScope.Count && astAndScope[offset].Item2 + 1 == (scope + 1)) //TODO: while in scope. Note: this is item2 + 1 because we are assuming we are inside the void main func. REMOVE WHEN NOT IN MAIN.
                Write(offset);
            AppendScope(scope);
            source.AppendLine("}");
        }
    }
}
