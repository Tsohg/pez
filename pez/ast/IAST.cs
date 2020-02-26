using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pez.ast
{
    interface IAST
    {
        //Inserts a node at the current level of the tree.
        void Insert(PNode node);

        //Translates to a language using breadth first algorithm
        void LevelOrderTranslate();
    }
}
