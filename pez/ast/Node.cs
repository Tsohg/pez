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
        public Node prev;

        public Node(){}
        public Node(Lexeme data){ this.data = data; }
        public override string ToString(){ return data.token; }
    }
}
