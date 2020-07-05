using System;
using System.Collections.Generic;
using System.Text;

namespace pez.lex
{
    //A more complex lexer will probably involve lexer states.
    //A lexer state would simply alter how tokens are delimited in the middle of parsing tokens.

    //This code heavily refers to Modern Compiler Desing 2nd ed.

    /// <summary>
    /// Turns source into lexemes/tokens.
    /// </summary>
    class Lexer
    {
        private readonly string file;
        private readonly string eof = "EOF"; //end of file token
        private int offset;

        public Lexer(string file)
        {
            this.file = file;
            offset = 0;
        }

        /// <summary>
        /// Converts a file to an array of lexemes.
        /// </summary>
        /// <returns></returns>
        public Lexeme[] GetLexStream()
        {
            List<Lexeme> lexemes = new List<Lexeme>();

            while(true)
            {
                Lexeme lex = Next();
                lexemes.Add(lex);
                if (lex.token == eof)
                    break;
            }
            return lexemes.ToArray();
        }
        
        /// <summary>
        /// Returns the next lexeme/token in the file based on a list of separators defined in PezSym.
        /// </summary>
        /// <returns>The next lexeme/token in a file.</returns>
        private Lexeme Next()
        {
            Lexeme lex; 

            StringBuilder sb = new StringBuilder();

            if (!HasNextChar())
                return new Lexeme(PezLexType.termin, eof);

            //Note: file[offset] is the current character we are looking at.

            //whitespace is first because i expect to read in a stream of \t when if statements pop up to determine scope.
            if (char.IsWhiteSpace(file[offset])) //we count \t as a scope token. \r\n is our terminating token.
            {
                switch (file[offset])
                {
                    case '\t':
                        lex = new Lexeme(PezLexType.scoper, ";t"); // ; will be my marker for escape characters. ;t = \t but easier to recognize in output. ;rn = \r\n
                        offset++;
                        return lex;
                    case '\r': //\r\n
                        lex = new Lexeme(PezLexType.termin, ";rn");
                        if (HasNextChar(1) && file[offset + 1] == '\n')
                            offset++; //skip \n
                        return lex;
                    default:
                        offset++;
                        return Next(); //if bugs happen, it's probably here
                }
            }
            else if (file[offset] == '"') //string literal beginning. anything surrounded in quotations is a string.
            {
                sb.Append(file[offset]); //add quotation
                offset++;
                while (HasNextChar() && file[offset] != '"')
                {
                    sb.Append(file[offset]);
                    offset++;
                }
                sb.Append(file[offset]); //should be a closing quotation.
                offset++;
                //check to see if someone missed a quotation
                if (!HasNextChar() && sb[sb.Length - 1] != '"') throw new Exception("Lexer:StringLiteral:: Missing Quotation.");
                lex = new Lexeme(PezLexType._string, sb.ToString());
                return lex;
            }
            else if (char.IsLetter(file[offset])) //read until space as an identifier or type name. note: ids can be types.
            {
                while (HasNextChar() && char.IsLetterOrDigit(file[offset]))
                {
                    sb.Append(file[offset]);
                    offset++;
                }
                lex = new Lexeme(PezLexType.id, sb.ToString());
                return lex;
            }
            else if (char.IsDigit(file[offset])) //read value until space. test to see if it is an integer or float.
            {
                PezLexType type = PezLexType.unid;
                while (HasNextChar() && (char.IsDigit(file[offset]) || file[offset] == '.'))
                {
                    if (file[offset] == '.')
                        type = PezLexType._float;
                    sb.Append(file[offset]);
                    offset++;
                }
                if (type == PezLexType.unid) //probably an integer so lets test.
                {
                    int val;
                    if (!int.TryParse(sb.ToString(), out val))
                        throw new Exception("Lexer:Digit:: Value is too big for an integer.");

                    lex = new Lexeme(PezLexType._int, sb.ToString());
                    return lex;
                }
                else //type of float OR double. but we assume double here for now.
                {
                    float val;
                    double dval;
                    if (!float.TryParse(sb.ToString(), out val)) //too big for a float
                    {
                        if (!double.TryParse(sb.ToString(), out dval))
                            throw new Exception("Lexer:Digit:: Value is too big to be contained within a double.");

                        //return it as a double
                        lex = new Lexeme(PezLexType._double, sb.ToString());
                        return lex;
                    }
                    //it can fit in a float so return it as float
                    lex = new Lexeme(PezLexType._float, sb.ToString());
                    return lex;
                }
            }
            else if ((39 < file[offset]) && (file[offset] < 48) || file[offset] == '=') //42 - 47 are basic ops 40 and 41 are ( )
            {
                if (file[offset] == 40)
                {
                    lex = new Lexeme(PezLexType.l_paren, file[offset].ToString()); // (
                    offset++;
                    return lex;
                }
                else if (file[offset] == 41)
                {
                    lex = new Lexeme(PezLexType.r_paren, file[offset].ToString()); // )
                    offset++;
                    return lex;
                }
                //theoretically can use .= .+ .- as operators...
                while (HasNextChar() && (41 < file[offset]) && (file[offset] < 48) || file[offset] == '=') //(41 < c < 48) //TODO: Move assignment to boolean lex for expressions later.
                {
                    sb.Append(file[offset]);
                    offset++;
                }
                lex = new Lexeme(PezLexType.op, sb.ToString());
                return lex;
            }
            else if (HasNextChar() && file[offset] == ':') //range operator or colon operators.
            {
                sb.Append(file[offset]);
                offset++;
                if (HasNextChar() && file[offset] == ':') //specifically range operators at the moment.
                {
                    sb.Append(file[offset]);
                    offset++;
                    lex = new Lexeme(PezLexType.op, sb.ToString());
                    return lex;
                }
                else throw new Exception("Invalid range operator at: " + offset);
            }
            else if (file[offset] == '#')//# will be my comment symbol much like R language.
            {
                //ignore the rest of the line by finding \r\n then going offset++ then return next()
                while (true)
                {
                    offset++;
                    if (!HasNextChar())
                        return new Lexeme(PezLexType.termin, eof);
                    if (file[offset] == '\r')
                    {
                        offset++;
                        if (HasNextChar() && file[offset] == '\n')
                        {
                            offset++;
                            return new Lexeme(PezLexType.termin, ";rn");
                        }
                        else throw new Exception("Lexer:Next:: ;r not followed by ;n. Pez requires Windows CRLF line endings to function.");
                    }
                }
            }
            else throw new Exception("Lexer:Next:: Unidentified token encountered at offset: " + offset);
        }
        //40, 41 = ( )   
        //60-62 boolean/assignment:  < = >

        /// <summary>
        /// Checks to see if there is a next lexeme or not based on offset vs file length.
        /// </summary>
        /// <returns></returns>
        private bool HasNextChar(int dist = 0)
        {
            if (offset + dist > file.Length - 1)
                return false;
            else return true;
        }
    }
}
