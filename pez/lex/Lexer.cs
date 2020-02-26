using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pez.lex
{
    //A more complex lexer will probably involve lexer states.
    //A lexer state would simply alter how tokens are delimited in the middle of parsing tokens.

    /// <summary>
    /// Turns source into lexemes/tokens.
    /// </summary>
    class Lexer
    {
        private string file;
        private PezSym sym;

        public int Offset { get; private set; }
        public int LineNum { get; private set; }
        public string Current { get; private set; } //Last lexeme found by Next.
        public Lexeme[] LexStream { get; private set; }

        public Lexer(string file)
        {
            this.file = file;
            sym = new PezSym();
            Offset = 0;
            LineNum = 1;
            LexStream = Start();
        }

        /// <summary>
        /// Converts a file to an array of lexemes.
        /// </summary>
        /// <returns></returns>
        private Lexeme[] Start()
        {
            List<Lexeme> lexemes = new List<Lexeme>();

            while(HasNext())
            {
                string token = Next();

                while (token == "") //skip blanks. kinda dangerous.
                    if (HasNext())
                        token = Next();

                Lexeme l = null;

                if (sym.ExpectType(token))
                    l = new Lexeme(PezLexType.type, token);
                else if (sym.ExpectIdentifier(token))
                    l = new Lexeme(PezLexType.id, token);
                else if (sym.ExpectSeparator(token))
                    l = new Lexeme(PezLexType.sep, token);
                else if (sym.ExpectTerminator(token))
                    l = new Lexeme(PezLexType.termin, token);
                else if (sym.ExpectOperator(token))
                    l = new Lexeme(PezLexType.op, token);
                else if (sym.ExpectInteger(token))
                    l = new Lexeme(PezLexType._int, token);

                if (l == null) throw new Exception("Lexer:Start:: Null Lexeme.");

                lexemes.Add(l);
            }
            if (lexemes == null) throw new Exception("Lexer:Start:: Null Lexemes List.");
            Lexeme addedTermin = new Lexeme(PezLexType.termin, ";"); //adding a terminator here to signify end of file.
            lexemes.Add(addedTermin);
            return lexemes.ToArray();
        }
        
        /// <summary>
        /// Returns the next lexeme/token in the file based on a list of separators defined in PezSym.
        /// </summary>
        /// <returns>The next lexeme/token in a file.</returns>
        private string Next() //remove \r\n terminators.
        {
            StringBuilder result = new StringBuilder();

            if(file[Offset] == sym.terminators[0][0])
            {
                result.Append(';'); //replace \r\n with something easier to work with and notice, but still retain the style of \r\n being a terminator.
                Offset += 2;
                return result.ToString();
            }

            //sep[0] = ' '
            while (HasNext() && file[Offset] != sym.separators[0]) //lex on spacebar, \r\n for now. perhaps full array of characters later.
            {
                if (file[Offset] == sym.terminators[0][0]) // \r then return result for now.
                    return result.ToString();
                else
                {
                    result.Append(file[Offset]);
                    Offset++;
                }
            }

            Offset++; //on spacebar so we go to next character.
            Current = result.ToString();
            return result.ToString();
        }

        /// <summary>
        /// Checks to see if there is a next lexeme or not based on offset vs file length.
        /// </summary>
        /// <returns></returns>
        private bool HasNext()
        {
            if (Offset > file.Length - 1)
                return false;
            else return true;
        }
    }
}
