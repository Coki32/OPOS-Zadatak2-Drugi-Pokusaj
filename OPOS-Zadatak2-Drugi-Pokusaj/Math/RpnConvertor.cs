using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPOS_Zadatak2_Drugi_Pokusaj.Math
{
    //Ovo je P1 nivo parsiranja, kopirao sam kod iz starog Java projekta gdje je bio kopiran iz SPA projekta
    //srecom ne boduje se parsiranje u projektu
    class RpnConvertor
    {
        private static Dictionary<string, int> priorities = new Dictionary<string, int>()
        {
            {"+", 2 },
            {"-", 2 },
            {"/", 3 },
            {"*", 3 },
            {"^", 4 },
        };

        public static List<Token> ToRPN(List<Token> tokenList)
        {
            Stack<Token> operatorStack = new Stack<Token>();
            List<Token> output = new List<Token>();//volim array list...

            foreach (Token t in tokenList)
            {
                if (t.Type == Token.TokenType.Operand || t.Type == Token.TokenType.Variable)
                    output.Add(t);
                else if (t.Type == Token.TokenType.Operator)
                {
                    while (!(operatorStack.Count() == 0) && operatorStack.Peek().Type != Token.TokenType.LeftBracket
                            && priorities[t.Value] <= priorities[operatorStack.Peek().Value])
                        output.Add(operatorStack.Pop());//sto je jako kad pop vraca vrijednost
                    operatorStack.Push(t);
                }
                else if (t.Type == Token.TokenType.LeftBracket)
                    operatorStack.Push(t);
                else if (t.Type == Token.TokenType.RightBracket)
                {
                    while (!(operatorStack.Count() == 0) && operatorStack.Peek().Type != Token.TokenType.LeftBracket)
                        output.Add(operatorStack.Pop());
                    //sad je onaj left bracket ili je greska
                    if (operatorStack.Count() == 0)
                    {
                        return null;
                    }
                    else//mora biti zagrada
                        operatorStack.Pop();
                }
            }
            while (!(operatorStack.Count() == 0))
                output.Add(operatorStack.Pop());
            return output;
        }

        public static List<Token> Tokenize(string inputString)
        {
            List<Token> tokens = new List<Token>();
            String op = "/*-+^", number = "1234567890.", bracketLeft = "{[(", bracketRight = ")]}";
            for (int i = 0; i < inputString.Length; i++)
            {
                if (op.Contains(inputString[i]))//pronadjen je
                    tokens.Add(new Token(inputString.Substring(i, 1), Token.TokenType.Operator));
                else if (bracketLeft.Contains(inputString[i]))//pronadjen je
                    tokens.Add(new Token(inputString.Substring(i, 1), Token.TokenType.LeftBracket));
                else if (bracketRight.Contains(inputString[i]))//pronadjen je
                    tokens.Add(new Token(inputString.Substring(i,  1), Token.TokenType.RightBracket));
                else if ((inputString[i] >= 'a' && inputString[i] <= 'z') || (inputString[i] >= 'A' && inputString[i] <= 'Z'))
                {//ovo sad smeta funkcijama al ok
                    tokens.Add(new Token(inputString.Substring(i, 1), Token.TokenType.Variable));
                }
                else if (inputString[i] == ' ')//prekosci razmake jelte
                    continue;
                else
                {//broj je ili greska
                    int start = i, end = i;
                    while ((end < inputString.Length) && number.Contains(inputString[end]))
                        end++;
                    end++;//jer mi fali jedan sad
                    Debug.WriteLine($"Broj token je {inputString.Substring(start, end - 1 - start)}");
                    if (inputString.Substring(start, (end == inputString.Length ? end : end - 1) - start) == "45)")
                        Debug.WriteLine("Stani malo");
                    tokens.Add(new Token(inputString.Substring(start, end - 1 -start), Token.TokenType.Operand));
                    //if (end == inputString.Length)
                    //    break;
                    i = end - 2;//magija
                }
            }
            return tokens;
        }

    }
}
