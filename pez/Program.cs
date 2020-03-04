using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pez.ast;
using System.IO;

namespace pez
{
    class Program
    {
        static void Main(string[] args) //pez file -c //for C dispensing on command line.
        {
            Parser p = new Parser(@"C:\Users\Nathan\source\repos\pez\pez\source\pez1.txt");
        }
    }
}
