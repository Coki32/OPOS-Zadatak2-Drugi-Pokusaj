using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPOS_Zadatak2_Drugi_Pokusaj.Math
{
    class Token
    {
        public enum TokenType { Operand, Operator, LeftBracket, RightBracket, Variable };


        public string Value { get; private set; }
        public TokenType Type { get; private set; }

        public Token(String value, TokenType type) => (Value, Type) = (value, type);

    }
}
