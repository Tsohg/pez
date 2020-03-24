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
        static void Main(string[] args) //pez filepath -c //for C dispensing on command line.
        {
            if (!File.Exists(args[0]))
                throw new Exception("Invalid file path. Must use a full path to the pez source file.");

            //might want to check to see if args[1] is a valid file path.

            Parser p = new Parser(@"C:\Users\Nathan\source\repos\pez\pez\source\pez1.txt", args[1], args[2]);
        }
    }
}
