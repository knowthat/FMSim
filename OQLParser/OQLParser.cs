using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OQLParser
{
    enum Expressionkind { Condition, Wrapped, Member, NamedOperation, ParamOperation, OperatorOperation, Undefined };

    public abstract class Expression
    {
    }

    public abstract class ChainableExpression : Expression
    {
        public ChainableExpression Next;
    }

    public class ConditionalExpression : Expression
    {
        public Expression Expression;
        public Expression If;
        public Expression Else;
    }

    public class MemberExpression : ChainableExpression
    {
        public string Member;
    }

    public abstract class OperationalExpression : ChainableExpression
    {
    }

    public class OperatorOperationalExpression : OperationalExpression
    {
        public List<Expression> Expressions = new List<Expression>()
            ;
        public List<string> Operators = new List<string>();
    }

    public class NamedOperationalExpression : OperationalExpression
    {
        public String Operation;
    }

    public class ParamOperationExpression : NamedOperationalExpression
    {
        public Expression Parameter;
    }


    static class Parser
    {
        public static Expression ParseOQLExpression(string aExpression)
        {
            Expressionkind vKind = Expressionkind.Undefined;
            string vExpr = aExpression.Trim();
            if (vExpr.Length == 0) return null;
            int vParenthesisDepth = 0;
            bool vParenthesisDepthReachedZero = false;
            // Analyze expression kind

            // Check for conditional expression
            if ((vExpr.Length > 1) && (vExpr[0] == 'i') && (vExpr[1] == 'f'))
                vKind = Expressionkind.Condition;

            if (vKind == Expressionkind.Undefined)
            {
                for (int I = 0; I < vExpr.Length; I++)
                {
                    char c = vExpr[I];
                    if (c == '(')
                        vParenthesisDepth++;
                    else if (c == ')')
                        vParenthesisDepth--;

                    if (((c == '+') || (c == '-') || (c == '*') || (c == '/') || (c == '<') || (c == '>') || (c == '=')) && (vParenthesisDepth == 0))
                    {
                        if (vExpr[I + 1] != '>' && (vExpr[I - 1] != '-'))
                        {
                            vKind = Expressionkind.OperatorOperation;
                            break;
                        }
                    }
                    // Check for wrapped expression
                    if (vParenthesisDepth == 0)
                        vParenthesisDepthReachedZero = true;
                    if ((I == vExpr.Length - 1) && (!vParenthesisDepthReachedZero))
                        vKind = Expressionkind.Wrapped;
                }
            }

            if (vKind == Expressionkind.Undefined)
            {
                if ((vExpr.Length > 1) && vExpr[1] == '>')
                {
                    int vDotIndex = vExpr.IndexOf('.');
                    if (vDotIndex == -1) vDotIndex = 1000;
                    int vArrowIndex = vExpr.IndexOf("->", 2);
                    if (vArrowIndex == -1) vArrowIndex = 1000;
                    int vParenthesisIndex = vExpr.IndexOf("(");
                    if ((vParenthesisIndex != -1) && (vParenthesisIndex < vDotIndex) && (vParenthesisIndex < vArrowIndex))
                        vKind = Expressionkind.ParamOperation;
                    else
                        vKind = Expressionkind.NamedOperation;
                }
                else if (vExpr.Length > 0)
                    vKind = Expressionkind.Member;
            }

            Expression Result = null;
            // Parse current expression step
            switch (vKind)
            {
                case Expressionkind.Condition:
                    Result = new ConditionalExpression();
                    string vIfExpression, vElseExpression, vConditionExpression;
                    int vConditionStart = 0, vConditionStop = 0, vIfStart = 0, vIfStop = 0, vElseStart = 0, vElseStop = 0;

                    int vConditionDepth = 0;
                    for (int I = 0; I < vExpr.Length; I++)
                    {
                        if (I > 0)
                        {
                            if ((vExpr[I - 1] == 'i') && (vExpr[I] == 'f'))
                            {
                                if (I <= 1 || (I > 1 && (vExpr[I - 2] != 'd')))
                                {
                                    vConditionDepth++;
                                    if (vConditionDepth == 1)
                                        vConditionStart = I + 1;
                                }
                            }
                        }
                        if (I > 3)
                        {
                            if ((vExpr[I - 4] == 'e') && (vExpr[I - 3] == 'n') && (vExpr[I - 2] == 'd') && (vExpr[I - 1] == 'i') && (vExpr[I] == 'f'))
                            {
                                if (vConditionDepth == 1)
                                    vElseStop = I - 5;
                                vConditionDepth--;
                            }

                            if ((vExpr[I - 3] == 't') && (vExpr[I - 2] == 'h') && (vExpr[I - 1] == 'e') && (vExpr[I] == 'n'))
                                if (vConditionDepth == 1)
                                {
                                    vConditionStop = I - 4;
                                    vIfStart = I + 1;
                                }

                            if ((vExpr[I - 3] == 'e') && (vExpr[I - 2] == 'l') && (vExpr[I - 1] == 's') && (vExpr[I] == 'e'))
                                if (vConditionDepth == 1)
                                {
                                    vIfStop = I - 4;
                                    vElseStart = I + 1;
                                }
                        }
                    }

                    vConditionExpression = vExpr.Substring(vConditionStart, vConditionStop - vConditionStart);
                    vIfExpression = vExpr.Substring(vIfStart, vIfStop - vIfStart);
                    vElseExpression = vExpr.Substring(vElseStart, vElseStop - vElseStart);

                    (Result as ConditionalExpression).Expression = ParseOQLExpression(vConditionExpression);
                    (Result as ConditionalExpression).If = ParseOQLExpression(vIfExpression);
                    (Result as ConditionalExpression).Else = ParseOQLExpression(vElseExpression);

                    break;
                case Expressionkind.Wrapped:
                    break;
                case Expressionkind.Member:
                    Result = new MemberExpression();
                    int vDotIndex = vExpr.IndexOf('.');
                    int vArrowIndex = (vExpr.Length > 1) ? vExpr.IndexOf("->", 2) : -1;
                    int vEndIndex = vExpr.Length;
                    if (vDotIndex != -1 && vDotIndex < vEndIndex) vEndIndex = vDotIndex;
                    if (vArrowIndex != -1 && vArrowIndex < vEndIndex) vEndIndex = vArrowIndex;
                    (Result as MemberExpression).Member = vExpr.Substring(0, vEndIndex);
                    Expression vExpression = ParseOQLExpression(vExpr.Substring((vDotIndex == vEndIndex) ? vEndIndex + 1 : vEndIndex));
                    if ((vExpression != null) && !(vExpression is ChainableExpression))
                        throw new Exception("the expression after a member have to be chainable in the current implementation of object query parser");
                    else
                        (Result as MemberExpression).Next = (vExpression as ChainableExpression);
                    break;
                case Expressionkind.NamedOperation:
                    Result = new NamedOperationalExpression();
                    int vDotIndex1 = vExpr.IndexOf('.');
                    int vArrowIndex1 = vExpr.IndexOf("->", 2);
                    int vEndIndex1 = vExpr.Length;
                    if (vDotIndex1 != -1 && vDotIndex1 < vEndIndex1) vEndIndex1 = vDotIndex1;
                    if (vArrowIndex1 != -1 && vArrowIndex1 < vEndIndex1) vEndIndex1 = vArrowIndex1;
                    (Result as NamedOperationalExpression).Operation = vExpr.Substring(0, vEndIndex1);
                    Expression vExpression1 = ParseOQLExpression(vExpr.Substring((vDotIndex1 == vEndIndex1) ? vEndIndex1 + 1 : vEndIndex1));
                    if ((vExpression1 != null) && !(vExpression1 is ChainableExpression))
                        throw new Exception("the expression after a operation have to be chainable in the current implementation of object query parser");
                    else
                        (Result as NamedOperationalExpression).Next = (vExpression1 as ChainableExpression);
                    break;
                case Expressionkind.ParamOperation:
                    Result = new ParamOperationExpression();
                    int vParenthesisStartIndex = vExpr.IndexOf('(');
                    int vParenthesisEndIndex = vExpr.IndexOf(')');
                    int vDotIndex2 = vExpr.IndexOf('.');
                    int vArrowIndex2 = vExpr.IndexOf("->", 2);
                    int vEndIndex2 = vExpr.Length;
                    if (vDotIndex2 != -1 && vDotIndex2 < vEndIndex2) vEndIndex2 = vDotIndex2;
                    if (vArrowIndex2 != -1 && vArrowIndex2 < vEndIndex2) vEndIndex2 = vArrowIndex2;
                    (Result as ParamOperationExpression).Operation = vExpr.Substring(0, vParenthesisStartIndex);
                    (Result as ParamOperationExpression).Parameter = ParseOQLExpression(vExpr.Substring(vParenthesisStartIndex + 1, vParenthesisEndIndex - vParenthesisStartIndex - 1));
                    Expression vExpression2 = ParseOQLExpression(vExpr.Substring((vDotIndex2 == vEndIndex2) ? vEndIndex2 + 1 : vEndIndex2));
                    if ((vExpression2 != null) && !(vExpression2 is ChainableExpression))
                        throw new Exception("the expression after a operation have to be chainable in the current implementation of object query parser");
                    else
                        (Result as ParamOperationExpression).Next = (vExpression2 as ChainableExpression);
                    break;
                case Expressionkind.OperatorOperation:
                    Result = new OperatorOperationalExpression();
                    int vPreviousOperatorIndex = -1;
                    for (int I = 0; I < vExpr.Length; I++)
                    {
                        char c = vExpr[I];
                        if (c == '(')
                            vParenthesisDepth++;
                        else if (c == ')')
                            vParenthesisDepth--;

                        if (((c == '+') || (c == '-') || (c == '*') || (c == '/') || (c == '<') || (c == '>') || (c == '=')) && (vParenthesisDepth == 0))
                        {
                            if (vExpr[I + 1] != '>' && (vExpr[I - 1] != '-'))
                            {
                                (Result as OperatorOperationalExpression).Expressions.Add(ParseOQLExpression(vExpr.Substring(vPreviousOperatorIndex + 1, I - vPreviousOperatorIndex - 1)));
                                (Result as OperatorOperationalExpression).Operators.Add(c.ToString());
                                vPreviousOperatorIndex = I;
                            }
                        }
                    }
                    (Result as OperatorOperationalExpression).Expressions.Add(ParseOQLExpression(vExpr.Substring(vPreviousOperatorIndex + 1, vExpr.Length - vPreviousOperatorIndex - 1)));
                    break;
            }
            return Result;
        }
    }

}
