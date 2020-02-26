using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pez.types
{
    class BaseType
    {
        protected object value;

        public virtual object GetValue()
        {
            return value;
        }
    }
}
