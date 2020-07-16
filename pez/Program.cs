using System;
using pez.ast;
using System.IO;

namespace pez
{
    class Program
    {
        static void Main(string[] args) //pez filepath -c //for C dispensing on command line.
        {
            //if (!File.Exists(args[0]))
            //   throw new Exception("Invalid file path. Must use a full path to the pez source file.");

            //might want to check to see if args[1] is a valid file path.


            //replace first parameter with args[0] when ready for testing.
            //Parser p = new Parser(args[0], args[1], args[2]);
            //Parser p = new Parser(@"C:\Users\Nathan\source\repos\pez\pez\source\pez1.txt", args[1], args[2]);
            Parser p = new Parser(@"C:\Users\Nathan\source\repos\pez\pez\source\pez1.txt", @"C:\Users\Nathan\source\repos\pez\pez\source\pez2.txt", "c");
        }
    }
}
