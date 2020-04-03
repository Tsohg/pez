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
            while (offset < astAndScope.Count)
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
                    WriteLpf(ast, mScope);
                    break;
                case "else": //if->else
                    WriteElse(mScope);
                    break;

                //operating on an already declared int var.
                case "+":
                case "-":
                case "*":
                case "/":
                    WriteDecOp(ast, mScope);
                    offset++;
                    break;
                default:
                    throw new Exception("TranslateC:Write:: Errorneous first node.");
            }
        }

        private void WriteDecOp(Node ast, int scope)
        {
            AppendScope(scope);
            source.AppendLine(ast.left.data.token + " " + ast.data.token + " " + ast.right.data.token + ";");
        }

        /// <summary>
        /// Writes assignment statements in which the variable is both declared and assigned a value.
        /// </summary>
        /// <param name="ast"></param>
        /// <param name="scope"></param>
        private void WriteAssignment(Node ast, int scope)
        {
            AppendScope(scope);
            source.Append("int " + ast.left.data.token + " " + ast.data.token + " "); //for now, we are only doing integers so this is the type.
            Node exp = ast.right; //This subset tree of the assignment ast is the expression/value.
            InOrderWrite(exp);

            source.AppendLine(";");
        }

        /// <summary>
        /// Writes If and While statements.
        /// </summary>
        /// <param name="ast"></param>
        /// <param name="scope"></param>
        private void WriteConditional(Node ast, int scope) //TODO: pull out the while(offset < ast...) into a method called WriteScoped(scope)
        {
            AppendScope(scope);
            string keyword;
            Node temp = ast;
            //for now, we will throw a fit if the tree is bigger than: X op Y.
            switch (temp.data.token)
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

        private void WriteLpf(Node ast, int scope)
        {
            Node temp = ast.right;
            AppendScope(scope);
            char i = 'a';
            i += (char)scope;
            source.AppendLine("for(int " + i +  " = " + temp.left.data.token + "; " + i + " < " + temp.right.data.token + "; " + i + "++)"); //expression.

            //same as WriteConditional to write within scope.
            offset++;
            while (offset < astAndScope.Count && astAndScope[offset].Item2 + 1 == (scope + 1)) //TODO: while in scope. Note: this is item2 + 1 because we are assuming we are inside the void main func. REMOVE WHEN NOT IN MAIN.
                Write(offset);

            AppendScope(scope);
            source.AppendLine("}");
        }

        private void WriteElse(int scope)
        {
            //TODO: Might want to do some sophisticated checking to ensure that else only ever follows an if statement.

            AppendScope(scope);
            source.AppendLine("else{");

            //same as WriteConditional to write within scope.
            offset++;
            while (offset < astAndScope.Count && astAndScope[offset].Item2 + 1 == (scope + 1)) //TODO: while in scope. Note: this is item2 + 1 because we are assuming we are inside the void main func. REMOVE WHEN NOT IN MAIN.
                Write(offset);

            AppendScope(scope);
            source.AppendLine("}");
        }

        private void InOrderWrite(Node ast)
        {
            if (ast == null) return;

            InOrderWrite(ast.left);

            source.Append(ast.data.token);

            InOrderWrite(ast.right);
        }
    }
}
