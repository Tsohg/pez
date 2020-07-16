using pez.lex;
using System.Collections.Generic;

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
