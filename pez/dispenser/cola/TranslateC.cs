﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pez.ast;
using System.IO;
using pez.lex;

namespace pez.dispenser.cola
{
    /// <summary>
    /// Translates a pez program to C.
    /// </summary>
    class TranslateC : BaseTranslate
    {
        private StringBuilder functions = new StringBuilder(); //function definitions for C.
        private StringBuilder source = new StringBuilder(); //Currently i will just dump the contents of the code inside the main function of a C program.
        private int offset = 0;

        public TranslateC(List<Tuple<Node, int>> astAndScope, string outPath) : base(astAndScope, outPath) { }

        public override void Translate()
        {
            while (offset < astAndScope.Count)
                WriteState(offset);
            //source.Append("}"); //ending brace for void main()
            System.IO.File.WriteAllText(outPath, source.ToString());
        }

        private void AppendScope(int scope)
        {
            for (int i = 0; i < scope; i++) //proper indentation.
                source.Append("\t");
        }

        /// <summary>
        /// Primary function to write instructions recursively. WriteState's base cases are in the form of unscoped instructions such as basic mathematical operations.
        /// </summary>
        /// <param name="index"></param>
        private void WriteState(int index) //write instructions state.
        {
            Node ast = astAndScope[index].Item1;
            int scope = astAndScope[index].Item2;

            switch (ast.data.token)
            {
                case "=": //assignment
                    WriteAssignmentState(ast, scope);
                    offset++;
                    break;
                case "if": //if statement
                case "lpw": //while loop
                    WriteConditionalState(ast, scope);
                    break;
                case "lpf": //for loops have to be handled separately.      lpf X to Y       will be the syntax.
                    WriteLpfState(ast, scope);
                    break;
                case "else": //if->else
                    WriteElseState(scope);
                    break;

                //operating on an already declared int var.
                case "+":
                case "-":
                case "*":
                case "/":
                    WriteVarOpState(ast, scope);
                    offset++;
                    break;

                //Return statements
                case "return":
                    WriteReturnState(ast, scope);
                    offset++;
                    break;

                case "func":
                    //pos from right to left bitfield:
                    //has parameter info
                    //has return info
                    //parameter info has correct scope
                    //return info has correct scope.
                    short flags = 0;

                    //Should probably make this more efficient.
                    if(astAndScope[offset + 1].Item1.data.token == "fnfo") //parameter info
                    {
                        flags |= 1;
                        if (astAndScope[offset + 2].Item1.data.token == "fret") //return info with parameter info
                        {
                            flags |= 2;
                            if (scope == astAndScope[offset + 2].Item2)
                                flags |= 8;
                        }
                        //check scope of fnfo
                        if (scope == astAndScope[offset + 1].Item2)
                            flags |= 4;
                    }
                    else if(astAndScope[offset + 1].Item1.data.token == "fret") //return info but no params.
                    {
                        flags |= 2;
                        if(astAndScope[offset + 2].Item1.data.token == "fnfo") //parameter info with return info.
                        {
                            flags |= 1;
                            if (scope == astAndScope[offset + 1].Item2)
                                flags |= 4;
                        }
                        //check scope of fret
                        if (scope == astAndScope[offset + 2].Item2)
                            flags |= 8;
                    }

                    WriteFunctionState(ast, scope, flags);
                    break;

                default:
                    throw new Exception("TranslateC:Write:: Errorneous first node.");
            }
        }

        private void WriteReturnState(Node ast, int scope)
        {
            AppendScope(scope);
            source.Append(ast.data.token + " "); //return
            InOrderWrite(ast.right); //right subtree of ast is in-order expression.
            source.AppendLine(";");
        }

        /// <summary>
        /// Returns true if there is a return statement in the ast. else returns false.
        /// </summary>
        /// <param name="ast"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        private void WriteVarOpState(Node ast, int scope) //variable operation. TODO: Replace with InOrderWrite?
        {
            //TODO: implement a way to ensure that the variable has already been declared or defined within a particular scope.
            AppendScope(scope);
            InOrderWrite(ast); //this tree has no key.
            source.AppendLine(";");
            //source.AppendLine(ast.left.data.token + " " + ast.data.token + " " + ast.right.data.token + ";");
        }

        /// <summary>
        /// Writes assignment statements in which the variable is both declared and assigned a value.
        /// </summary>
        /// <param name="ast"></param>
        /// <param name="scope"></param>
        private void WriteAssignmentState(Node ast, int scope)
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
        private void WriteConditionalState(Node ast, int scope) //TODO: pull out the while(offset < ast...) into a method called WriteScoped(scope)
        {
            //TODO: Make it so else statements can only be written here if necessary. If written elsewhere, throw a compiler error.
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

            while (offset < astAndScope.Count && astAndScope[offset].Item2 == (scope + 1))
                WriteState(offset);

            AppendScope(scope);
            source.AppendLine("}");
        }

        private void WriteLpfState(Node ast, int scope)
        {
            Node temp = ast.right;
            AppendScope(scope);
            char i = 'a';
            i += (char)scope;
            source.AppendLine("for(int " + i +  " = " + (int.Parse(temp.left.data.token) - 1) + "; " + i + " < " + temp.right.data.token + "; " + i + "++){"); //expression.

            //same as WriteConditional to write within scope.
            offset++;
            while (offset < astAndScope.Count && astAndScope[offset].Item2 == (scope + 1));
                WriteState(offset);

            AppendScope(scope);
            source.AppendLine("}");
        }

        private void WriteElseState(int scope)
        {
            //TODO: Might want to do some sophisticated checking to ensure that else only ever follows an if statement.

            AppendScope(scope);
            source.AppendLine("else{");

            //same as WriteConditional to write within scope.
            offset++;
            while (offset < astAndScope.Count && astAndScope[offset].Item2 == (scope + 1))
                WriteState(offset);

            AppendScope(scope);
            source.AppendLine("}");
        }

        private void WriteFunctionState(Node func, int scope, short flags)
        {
            //Note: a function's body is expected to be scope+1 and ends @ scope much like the implementation for writing conditional blocks.

            //pos from right to left bitfield:
            //has parameter info
            //has return info
            //parameter info has correct scope
            //return info has correct scope.

            if ((flags & 1) == 1 && (flags & 4) != 4) throw new Exception("Function parameter information (fnfo) is not scoped correctly relative to the function (func).");
            if ((flags & 2) == 2 && (flags & 8) != 8) throw new Exception("Function return information (fret) is not scoped correctly relative to the function (func).");

            Node fnfo = null;
            Node fret = null;
            if ((flags & 1) == 1) //it's either in pos1 or pos2
            {
                if (astAndScope[offset + 1].Item1.data.token == "fnfo")
                    fnfo = astAndScope[offset + 1].Item1;
                else
                    fnfo = astAndScope[offset + 2].Item1;
            }

            if ((flags & 2) == 2) //it's either in pos1 or pos2
            {
                if (astAndScope[offset + 1].Item1.data.token == "fret")
                    fret = astAndScope[offset + 1].Item1;
                else
                    fret = astAndScope[offset + 2].Item1;
            }

            //Only modify offset at end here for clarity.
            if ((flags & 1) == 1)
                offset++;
            if ((flags & 2) == 2)
                offset++;
            offset++; //we are now on the next instruction that *should* be scoped to the function.

            //TODO: write function header to functions stringbuilder.

            //write function declaration
            AppendScope(scope);
            source.Append(LevelOrderReturnWrite(fret)); // void, int, (int, int)
            source.Append(" " + func.left.data.token); //function name
            source.Append(LevelOrderParameterWrite(func, fnfo));

            //writestate loop like conditional writestate loop except outside of main
            while (offset < astAndScope.Count && astAndScope[offset].Item2 == (scope + 1))
                WriteState(offset);

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

        /// <summary>
        /// Writes level order parameters into C. should return something like: (type name, type name, type name){
        /// </summary>
        /// <param name="ast"></param>
        private string LevelOrderParameterWrite(Node func, Node fnfo)
        {
            if (fnfo == null)
                return "(){\n";

            Queue<Node> nq = new Queue<Node>();
            nq.Enqueue(func.right);
            Lexeme[] funcParams = LevelOrderTraversal(nq); //right subtree of func = parameter list.

            nq = new Queue<Node>();
            nq.Enqueue(fnfo.right);
            Lexeme[] funcInfo = LevelOrderTraversal(nq); //same as function declaration

            StringBuilder result = new StringBuilder();

            if (funcParams.Length != funcInfo.Length) throw new Exception("Invalid number of types or parameters. Make sure types and parameters in the func and fnfo declaration are the same.");

            result.Append("(");
            for(int i = 0; i < funcParams.Length; i++)
            {
                result.Append(funcInfo[i].token + " ");
                result.Append(funcParams[i].token + ", ");
            }
            result.Remove(result.Length - 2, 2); //remove extra comma
            result.AppendLine("){");

            return result.ToString();
        }

        /// <summary>
        /// Writes level order function return in C.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="fret"></param>
        private string LevelOrderReturnWrite(Node fret)
        {
            if (fret == null)
                return "void";

            StringBuilder result = new StringBuilder();

            //For C language, we discard all other return types except the first one.
            result.Append(fret.right.data.token);

            return result.ToString();
        }
        /// <summary>
        /// Returns an array of lexemes in level order.
        /// </summary>
        /// <param name="ast"></param>
        /// <returns></returns>
        private Lexeme[] LevelOrderTraversal(Queue<Node> nq)
        {
            List<Lexeme> lexstream = new List<Lexeme>();

            while(nq.Count > 0)
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
    }
}
