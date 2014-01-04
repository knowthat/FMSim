using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OCLParser;


namespace FMSim.ORM
{
    public delegate Object NamedOperation(Object aContext);
    public delegate Object ParamOperation(Object aContext, Object aContextParamterResult);
    public delegate Object OperatorOperation(Object aLeftContext, Object aRightContext);

    public class FMExpressionHandler
    {
        public Dictionary<string, Delegate> Operations;

        FMObjectSpace objectSpace;
        public FMExpressionHandler(FMObjectSpace aObjectSpace)
        {
            objectSpace = aObjectSpace; 
            Operations = new Dictionary<string, Delegate>();
            Operations.Add("->select", (ParamOperation)Select);
        }

        public Object Evaluate(Object aContext, string aExpression)
        {
            string vExpression = aExpression.Replace(".AllInstances", "_allinstances");
            Expression vContext = Parser.ParseOCLExpression(vExpression);
            return _evaluate(aContext, vContext);
        }

        private Object _evaluate(Object aContext, Expression aExpression)
        {
            if (aExpression is ConditionalExpression)
            {
                ConditionalExpression vExp = aExpression as ConditionalExpression;
                Object vConditionResult = _evaluate(aContext,vExp.Expression);
                if (vConditionResult is Boolean)
                {
                    if ((Boolean)vConditionResult)
                        return _evaluate(aContext, vExp.If);
                    else
                        return _evaluate(aContext, vExp.Else);
                }
                else
                    throw new Exception("Result for the condition has to be Boolean. Now it's " + vConditionResult.GetType());
            }
            else if (aExpression is MemberExpression)
            {
                Object vResult = null;
                string vMember = (aExpression as MemberExpression).Member;
                bool vIsInvertedBoolean = false;
                if (vMember[0] == '!')
                {
                    vIsInvertedBoolean = true;
                    vMember = vMember.Substring(1);
                }
                if (char.IsUpper(vMember[0]))
                {
                    int vAllInstancesIndex = vMember.ToLower().IndexOf("_allinstances");
                    if (vAllInstancesIndex > -1)
                    {
                        string vClassName = vMember.Substring(0, vAllInstancesIndex);
                        vResult = new List<Object>();
                        foreach (FMObject O in objectSpace.AllObjects)
                            if (O.FMClass == vClassName)
                                (vResult as List<Object>).Add(O);
                    }
                }
                else if (aContext is List<Object>)
                {
                    vResult = new List<Object>();
                    foreach (FMObject O in aContext as List<Object>)
                        (vResult as List<Object>).Add(GetMemberOrConst((aExpression as MemberExpression).Member, O));
                }
                else if (aContext is FMObject)
                    vResult = (GetMemberOrConst((aExpression as MemberExpression).Member, aContext as FMObject));
                else
                    throw new Exception("Can not evaluate member from " + aContext.GetType());

                if ((aExpression as MemberExpression).Next != null)
                    vResult = _evaluate(vResult, (aExpression as MemberExpression).Next);
                if (vIsInvertedBoolean && vResult is Boolean)
                    vResult = !(Boolean)vResult;
                return vResult;
            }
            else if (aExpression.GetType() == typeof(NamedOperationalExpression))
            {
                Object vResult = null;
                string vOperation = (aExpression as NamedOperationalExpression).Operation;
                NamedOperation vOperationDelegate = Operations[vOperation.ToLower()] as NamedOperation;
                vResult = vOperationDelegate.Invoke(aContext);
                if ((aExpression as NamedOperationalExpression).Next != null)
                    vResult = _evaluate(vResult, (aExpression as NamedOperationalExpression).Next);
                return vResult;
            }
            else if (aExpression is ParamOperationExpression)
            {
                Object vResult = null;
                Object vParameterResult = null;
                string vOperation = (aExpression as ParamOperationExpression).Operation;
                ParamOperation vOperationDelegate = Operations[vOperation] as ParamOperation;

                if (aContext is List<Object>)
                {
                    vParameterResult = new List<Object>();
                    foreach (FMObject O in aContext as List<Object>)
                    {
                        Object vOParameterResult = _evaluate(O, (aExpression as ParamOperationExpression).Parameter);
                        (vParameterResult as List<Object>).Add(vOParameterResult);
                    }
                }
                else
                {
                    vParameterResult = _evaluate(aContext, (aExpression as ParamOperationExpression).Parameter);
                }
                vResult = vOperationDelegate.Invoke(aContext, vParameterResult);

                if ((aExpression as ParamOperationExpression).Next != null)
                    vResult = _evaluate(vResult, (aExpression as ParamOperationExpression).Next);
                return vResult;
            }
            else if (aExpression is OperatorOperationalExpression)
            {
                Object vResult = null;
                OperatorOperationalExpression vOperatOperExp = (aExpression as OperatorOperationalExpression);
                List<object> vObjectList = new List<object>();
                foreach (Expression E in vOperatOperExp.Expressions)
                {
                    vObjectList.Add(_evaluate(aContext, E));
                }
                
                for (int I=0; I< vObjectList.Count; I++)
                {
                    if (vObjectList[0] is Int32)
                    {
                        if (I==0)
                            vResult = vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "+")
                            vResult = (Int32)vResult + (Int32)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "-")
                            vResult = (Int32)vResult - (Int32)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "*")
                            vResult = (Int32)vResult * (Int32)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "/")
                            vResult = (Int32)vResult / (Int32)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == ">")
                            vResult = (Int32)vResult > (Int32)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "<")
                            vResult = (Int32)vResult < (Int32)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "=")
                            vResult = (Int32)vResult == (Int32)vObjectList[I];
                    }
                    else if (vObjectList[0] is Decimal)
                    {
                        if (I==0)
                            vResult = vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "+")
                            vResult = (Decimal)vResult + (Decimal)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "-")
                            vResult = (Decimal)vResult - (Decimal)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "*")
                            vResult = (Decimal)vResult * (Decimal)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "/")
                            vResult = (Decimal)vResult / (Decimal)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == ">")
                            vResult = (Decimal)vResult > (Decimal)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "<")
                            vResult = (Decimal)vResult < (Decimal)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "=")
                            vResult = (Decimal)vResult == (Decimal)vObjectList[I];
                    }
                    else if (vObjectList[0] is String)
                    {
                        if (I==0)
                            vResult = vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "+")
                            vResult = (String)vResult + (String)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "=")
                            vResult = (String)vResult == (String)vObjectList[I];
                    }
                    else if (vObjectList[0] is DateTime)
                    {
                        if (I==0)
                            vResult = vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == ">")
                            vResult = (DateTime)vResult > (DateTime)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "<")
                            vResult = (DateTime)vResult < (DateTime)vObjectList[I];
                        else if  (vOperatOperExp.Operators[I - 1] == "=")
                            vResult = (DateTime)vResult == (DateTime)vObjectList[I];
                    }
                    else if (vObjectList[0] is Boolean)
                    {
                        if (I == 0)
                            vResult = vObjectList[I];
                        else if (vOperatOperExp.Operators[I - 1] == "&")
                            vResult = (Boolean)vResult && (Boolean)vObjectList[I];
                        else if (vOperatOperExp.Operators[I - 1] == "|")
                            vResult = (Boolean)vResult || (Boolean)vObjectList[I];
                    }
                }
                if ((aExpression as OperatorOperationalExpression).Next != null)
                    vResult = _evaluate(vResult, (aExpression as ParamOperationExpression).Next);
                return vResult;
            }
            return null;
        }

        Object Select(Object aContext, Object aContextParamterResult)
        {
            List<Object> vResult = new List<Object>();
            if (aContext is List<Object>)
            {
                for (int I = 0; I < (aContext as List<Object>).Count; I++)
                    if ((Boolean)(aContextParamterResult as List<Object>)[I])
                        vResult.Add((aContext as List<Object>)[I]);
                return vResult;
            }
            else throw new Exception("Select has to be evaluated on a list");
        }

        Object GetMemberOrConst(string aMember, FMObject aCtxObject)
        {
            if (aCtxObject.attributes.ContainsKey(aMember))
                return aCtxObject.attributes[aMember].FMValue;
            else if ((aMember[0] == '\'') && (aMember[aMember.Length - 1] == '\''))
                return aMember.Substring(1, aMember.Length - 2);
            else if (aMember.ToLower() == "true")
                return true;
            else if (aMember.ToLower() == "false")
                return false;
            else if (char.IsNumber(aMember[0]))
            {
                if (aMember.IndexOf('.') > 0)
                    return decimal.Parse(aMember);
                else
                    return int.Parse(aMember);
            }
            else
                throw new Exception("Cannot evaluate member or constant " + aMember);
        }
    }
}
