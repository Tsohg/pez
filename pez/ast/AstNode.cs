using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pez.lex;

namespace pez.ast
{
    class AstNode
    {
        public Lexeme data;
        public AstNode left;
        public AstNode right;

        public AstNode(Lexeme data)
        {
            this.data = data;
        }
    }
}
