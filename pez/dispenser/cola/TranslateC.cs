using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pez.ast;

namespace pez.dispenser.cola
{
    /// <summary>
    /// Translates a pez program to C.
    /// </summary>
    class TranslateC : BaseTranslate
    {
        public TranslateC(List<Tuple<Node, int>> astAndScope, string outPath) : base(astAndScope, outPath) { }

        public override void Translate()
        {
            throw new NotImplementedException();
        }
    }
}
