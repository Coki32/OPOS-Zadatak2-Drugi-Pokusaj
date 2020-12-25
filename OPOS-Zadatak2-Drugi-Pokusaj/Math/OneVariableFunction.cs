using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPOS_Zadatak2_Drugi_Pokusaj.Math
{
    class OneVariableFunction
    {

        public List<Token> Tokens { get; private set; } 
        
        public OneVariableFunction(List<Token> inputTokens) => Tokens = inputTokens;

        private Token ConvertIfVariable(Token t, double value)
        {
            if (t.Type == Token.TokenType.Variable)
                return new Token(value.ToString(), Token.TokenType.Operand);
            else
                return t;
        }
        private double exec(Token a, Token b, Token op)
        {
            switch (op.Value)
            { 
                case "/":
                    return Double.Parse(a.Value) / Double.Parse(b.Value);
                case "*":
                    return Double.Parse(a.Value) * Double.Parse(b.Value);
                case "+":
                    return Double.Parse(a.Value) + Double.Parse(b.Value);
                case "-":
                    return Double.Parse(a.Value) - Double.Parse(b.Value);
                case "^":
                    return System.Math.Pow(Double.Parse(a.Value), Double.Parse(b.Value));
            }
            throw new Exception("Ne bi trebali ovdje doci...");
        }
        public double ValueAtX(double x)
        {
            double result = 0.0;
            List<Token> cTokens = new List<Token>(Tokens);
            while (cTokens.Count > 1)
            {
                int i = 0;
                while (i < cTokens.Count && cTokens[i].Type != Token.TokenType.Operator)
                    i++;
                //sad je i-ti operand, dohvati i-ti, i-1 i i-2
                Token op = cTokens[i],
                        b = cTokens[i - 1],
                        a = cTokens[i - 2];
                b = ConvertIfVariable(b, x);
                a = ConvertIfVariable(a, x);
                cTokens.RemoveAt(i);
                cTokens.RemoveAt(i - 1);
                cTokens.RemoveAt(i - 2);
                cTokens.Insert(i - 2, new Token(exec(a, b, op).ToString(), Token.TokenType.Operand));
            }
            try
            {
                if (double.TryParse(ConvertIfVariable(cTokens[0], x).Value, out result))
                    return result;
                else
                    throw new Exception("Greska pri racunanju izraza!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Greska pri racunanju");
                Debug.WriteLine(ex.Message);
                return x;
            }
        }


    }
}
