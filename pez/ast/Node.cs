using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pez.lex;

namespace pez.ast
{
    class Node
    {
        public Lexeme data;
        public Node left;
        public Node right;
    }
}
