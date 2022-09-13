using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Linq.Expressions;
using System.Reflection;

namespace MyORMInMemory.Helpers
{
    public static class InMemoryExpressionsHelpers
    {
        public static ExpressionType GetExpressionType(this Expression exp)
        {
            return exp.NodeType;
        }

        public static bool IsPredicate(this Expression exp)
        {
            if(exp.NodeType == ExpressionType.MemberAccess && exp is MemberExpression mExp)
            {
                return (mExp.Member as PropertyInfo)?.PropertyType == typeof(bool);
            }

            if (exp.NodeType == ExpressionType.Call && exp is MethodCallExpression mtExp)
            {
                return mtExp.Method.ReturnType == typeof(bool);
            }

            if (exp.NodeType == ExpressionType.Lambda && exp is LambdaExpression lExp)
            {
                return lExp.ReturnType == typeof(bool);
            }

            return false;
        }

        public static bool IsMemberAcess(this Expression exp)
        {
            return ((exp as LambdaExpression)?.Body as MemberExpression) != null;
        }
    }
}
