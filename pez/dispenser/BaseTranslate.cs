using System;
using System.Collections.Generic;
using pez.ast;

namespace pez.dispenser
{
    public enum ExpType
    {
        rDec, //raw declaration: int a; or a;
        cInt, //complete declaration: int a = ??; or a = ??; must return an integer and expression must be consistent. ie: if a double exists, warn user?
        bln //conditional boolean expression return type. only associated with certain keywords/operators. such as in if or loop statements.
    };

    /// <summary>
    /// Parent class for all translations.
    /// </summary>
    abstract class BaseTranslate
    {
        protected List<Tuple<Node, int>> astAndScope;
        protected string outPath;

        public BaseTranslate(List<Tuple<Node, int>> astAndScope, string outPath)
        {
            this.astAndScope = astAndScope;
            this.outPath = outPath;
        }

        public abstract void Translate();

        public ExpType Evaluate(Node node) //Note: if a keyword is present, this may be evaluating a subtree in which the first node is skipped.
        {
            throw new NotImplementedException();
        }
    }
}
