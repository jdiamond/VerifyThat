// <copyright file="VerifyThat.cs" company="Jason Diamond">
//
// Copyright (c) 2010 Jason Diamond
//
// This source code is released under the MIT License.
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//
// </copyright>

namespace VerifyThat
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class Verify
    {
        public static void That(Expression<Func<bool>> expression)
        {
            That(expression, DefaultReporter);
        }

        public static void That(Expression<Func<bool>> expression, MessageReporter reporter)
        {
            That(expression, reporter, DefaultFormatter);
        }

        public static void That(Expression<Func<bool>> expression, MessageReporter reporter, MessageFormatter formatter)
        {
            if (expression == null) throw new ArgumentNullException("expression");

            EvaluateExpression(expression.Body, reporter, formatter);
        }

        private static void DefaultReporter(string message)
        {
            throw new VerificationException(Environment.NewLine + message);
        }

        private static string DefaultFormatter(string expression, string be, string expected, string was, string actual)
        {
            const string expectedText = "Expected:";
            string relationText = "to " + be + ":";
            const string butWasText = "but was:";

            int length = Math.Max(expectedText.Length, Math.Max(relationText.Length, butWasText.Length)) + 2;

            return expectedText.PadLeft(length) + " " + expression + Environment.NewLine +
                   relationText.PadLeft(length) + " " + expected + Environment.NewLine +
                   butWasText.PadLeft(length) + " " + actual;
        }

        private static void EvaluateExpression(Expression expression, MessageReporter reporter, MessageFormatter formatter)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                    EvaluateAndAlsoExpression(expression, reporter, formatter);
                    break;
                default:
                    TestExpression(expression, reporter, formatter);
                    break;
            }
        }

        private static void EvaluateAndAlsoExpression(Expression expression, MessageReporter reporter, MessageFormatter formatter)
        {
            var andAlsoExpression = (BinaryExpression)expression;
            EvaluateExpression(andAlsoExpression.Left, reporter, formatter);
            EvaluateExpression(andAlsoExpression.Right, reporter, formatter);
        }

        private static void TestExpression(Expression expression, MessageReporter reporter, MessageFormatter formatter)
        {
            if (expression is BinaryExpression)
            {
                TestBinaryExpression((BinaryExpression)expression, reporter, formatter);
            }
            else if (expression is TypeBinaryExpression)
            {
                TestTypeBinaryExpression((TypeBinaryExpression)expression, reporter, formatter);
            }
            else
            {
                TestNonBinaryExpression(expression, reporter, formatter);
            }
        }

        private static void TestBinaryExpression(BinaryExpression binaryExpression, MessageReporter reporter, MessageFormatter formatter)
        {
            object actual = DynamicEvaluate(binaryExpression.Left);

            var newBinaryExpression = Expression.MakeBinary(
                binaryExpression.NodeType,
                Expression.Constant(actual),
                binaryExpression.Right);

            bool result = EvaluateBoolean(newBinaryExpression);

            if (!result)
            {
                string left = ExpressionStringBuilder.GetExpressionString(binaryExpression.Left);
                string operation = GetBinaryOperatorText(binaryExpression.NodeType);
                string right = ExpressionStringBuilder.GetExpressionString(binaryExpression.Right);

                reporter(formatter(left, operation, right, "was", ExpressionStringBuilder.GetValue(actual)));
            }
        }

        private static string GetBinaryOperatorText(ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.Equal:
                    return "be";

                case ExpressionType.GreaterThan:
                    return "be greater than";

                case ExpressionType.GreaterThanOrEqual:
                    return "be greater than or equal to";

                case ExpressionType.LessThan:
                    return "be less than";

                case ExpressionType.LessThanOrEqual:
                    return "be less than or equal to";

                case ExpressionType.NotEqual:
                    return "not be";
            }

            return null;
        }

        private static void TestTypeBinaryExpression(TypeBinaryExpression typeBinaryExpression, MessageReporter reporter, MessageFormatter formatter)
        {
            object actual = DynamicEvaluate(typeBinaryExpression.Expression);

            var newTypeBinaryExpression = Expression.TypeIs(
                Expression.Constant(actual),
                typeBinaryExpression.TypeOperand);

            bool result = EvaluateBoolean(newTypeBinaryExpression);

            if (!result)
            {
                string left = ExpressionStringBuilder.GetExpressionString(typeBinaryExpression.Expression);
                string right = ExpressionStringBuilder.GetValue(typeBinaryExpression.TypeOperand);
                string actualType = ExpressionStringBuilder.GetValue(actual.GetType());

                reporter(formatter(left, "be", right, "was", actualType));
            }
        }

        private static void TestNonBinaryExpression(Expression expression, MessageReporter reporter, MessageFormatter formatter)
        {
            try
            {
                bool result = EvaluateBoolean(expression);

                if (!result)
                {
                    string description = ExpressionStringBuilder.GetExpressionString(expression);
                    reporter(formatter(description, "be", "true", "was", "false"));
                }
            }
            catch (CustomExtensionMethodVerificationException e)
            {
                if (expression.NodeType == ExpressionType.Call)
                {
                    var methodCallExpression = (MethodCallExpression)expression;

                    if (IsExtensionMethod(methodCallExpression.Method) &&
                        methodCallExpression.Arguments.Count > 0)
                    {
                        expression = methodCallExpression.Arguments[0];
                    }
                }

                string description = ExpressionStringBuilder.GetExpressionString(expression);
                reporter(formatter(description, e.BeText, e.ExpectedText, e.WasText, e.ActualText));
            }
        }

        private static object DynamicEvaluate(Expression expression)
        {
            var delegateType = typeof(Func<>).MakeGenericType(expression.Type);
            var lambda = Expression.Lambda(delegateType, expression);
            var @delegate = lambda.Compile();
            return @delegate.DynamicInvoke();
        }

        private static bool EvaluateBoolean(Expression expression)
        {
            var lambda = Expression.Lambda<Func<bool>>(expression);
            var @delegate = lambda.Compile();
            return @delegate();
        }

        private static bool IsExtensionMethod(MethodInfo method)
        {
            return method.IsStatic &&
                   Attribute.IsDefined(method, typeof(ExtensionAttribute));
        }

        private abstract class ExpressionVisitor
        {
            protected ExpressionVisitor()
            {
            }

            protected virtual Expression Visit(Expression exp)
            {
                if (exp == null)
                    return exp;
                switch (exp.NodeType)
                {
                    case ExpressionType.Negate:
                    case ExpressionType.NegateChecked:
                    case ExpressionType.Not:
                    case ExpressionType.Convert:
                    case ExpressionType.ConvertChecked:
                    case ExpressionType.ArrayLength:
                    case ExpressionType.Quote:
                    case ExpressionType.TypeAs:
                        return this.VisitUnary((UnaryExpression)exp);
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                    case ExpressionType.Divide:
                    case ExpressionType.Modulo:
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                    case ExpressionType.Or:
                    case ExpressionType.OrElse:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                    case ExpressionType.Coalesce:
                    case ExpressionType.ArrayIndex:
                    case ExpressionType.RightShift:
                    case ExpressionType.LeftShift:
                    case ExpressionType.ExclusiveOr:
                        return this.VisitBinary((BinaryExpression)exp);
                    case ExpressionType.TypeIs:
                        return this.VisitTypeIs((TypeBinaryExpression)exp);
                    case ExpressionType.Conditional:
                        return this.VisitConditional((ConditionalExpression)exp);
                    case ExpressionType.Constant:
                        return this.VisitConstant((ConstantExpression)exp);
                    case ExpressionType.Parameter:
                        return this.VisitParameter((ParameterExpression)exp);
                    case ExpressionType.MemberAccess:
                        return this.VisitMemberAccess((MemberExpression)exp);
                    case ExpressionType.Call:
                        return this.VisitMethodCall((MethodCallExpression)exp);
                    case ExpressionType.Lambda:
                        return this.VisitLambda((LambdaExpression)exp);
                    case ExpressionType.New:
                        return this.VisitNew((NewExpression)exp);
                    case ExpressionType.NewArrayInit:
                    case ExpressionType.NewArrayBounds:
                        return this.VisitNewArray((NewArrayExpression)exp);
                    case ExpressionType.Invoke:
                        return this.VisitInvocation((InvocationExpression)exp);
                    case ExpressionType.MemberInit:
                        return this.VisitMemberInit((MemberInitExpression)exp);
                    case ExpressionType.ListInit:
                        return this.VisitListInit((ListInitExpression)exp);
                    default:
                        throw new Exception(string.Format("Unhandled expression type: '{0}'", exp.NodeType));
                }
            }

            protected virtual MemberBinding VisitBinding(MemberBinding binding)
            {
                switch (binding.BindingType)
                {
                    case MemberBindingType.Assignment:
                        return this.VisitMemberAssignment((MemberAssignment)binding);
                    case MemberBindingType.MemberBinding:
                        return this.VisitMemberMemberBinding((MemberMemberBinding)binding);
                    case MemberBindingType.ListBinding:
                        return this.VisitMemberListBinding((MemberListBinding)binding);
                    default:
                        throw new Exception(string.Format("Unhandled binding type '{0}'", binding.BindingType));
                }
            }

            protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
            {
                ReadOnlyCollection<Expression> arguments = this.VisitExpressionList(initializer.Arguments);
                if (arguments != initializer.Arguments)
                {
                    return Expression.ElementInit(initializer.AddMethod, arguments);
                }
                return initializer;
            }

            protected virtual Expression VisitUnary(UnaryExpression u)
            {
                Expression operand = this.Visit(u.Operand);
                if (operand != u.Operand)
                {
                    return Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);
                }
                return u;
            }

            protected virtual Expression VisitBinary(BinaryExpression b)
            {
                Expression left = this.Visit(b.Left);
                Expression right = this.Visit(b.Right);
                Expression conversion = this.Visit(b.Conversion);
                if (left != b.Left || right != b.Right || conversion != b.Conversion)
                {
                    if (b.NodeType == ExpressionType.Coalesce && b.Conversion != null)
                        return Expression.Coalesce(left, right, conversion as LambdaExpression);
                    else
                        return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method);
                }
                return b;
            }

            protected virtual Expression VisitTypeIs(TypeBinaryExpression b)
            {
                Expression expr = this.Visit(b.Expression);
                if (expr != b.Expression)
                {
                    return Expression.TypeIs(expr, b.TypeOperand);
                }
                return b;
            }

            protected virtual Expression VisitConstant(ConstantExpression c)
            {
                return c;
            }

            protected virtual Expression VisitConditional(ConditionalExpression c)
            {
                Expression test = this.Visit(c.Test);
                Expression ifTrue = this.Visit(c.IfTrue);
                Expression ifFalse = this.Visit(c.IfFalse);
                if (test != c.Test || ifTrue != c.IfTrue || ifFalse != c.IfFalse)
                {
                    return Expression.Condition(test, ifTrue, ifFalse);
                }
                return c;
            }

            protected virtual Expression VisitParameter(ParameterExpression p)
            {
                return p;
            }

            protected virtual Expression VisitMemberAccess(MemberExpression m)
            {
                Expression exp = this.Visit(m.Expression);
                if (exp != m.Expression)
                {
                    return Expression.MakeMemberAccess(exp, m.Member);
                }
                return m;
            }

            protected virtual Expression VisitMethodCall(MethodCallExpression m)
            {
                Expression obj = this.Visit(m.Object);
                IEnumerable<Expression> args = this.VisitExpressionList(m.Arguments);
                if (obj != m.Object || args != m.Arguments)
                {
                    return Expression.Call(obj, m.Method, args);
                }
                return m;
            }

            protected virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
            {
                List<Expression> list = null;
                for (int i = 0, n = original.Count; i < n; i++)
                {
                    Expression p = this.Visit(original[i]);
                    if (list != null)
                    {
                        list.Add(p);
                    }
                    else if (p != original[i])
                    {
                        list = new List<Expression>(n);
                        for (int j = 0; j < i; j++)
                        {
                            list.Add(original[j]);
                        }
                        list.Add(p);
                    }
                }
                if (list != null)
                {
                    return list.AsReadOnly();
                }
                return original;
            }

            protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
            {
                Expression e = this.Visit(assignment.Expression);
                if (e != assignment.Expression)
                {
                    return Expression.Bind(assignment.Member, e);
                }
                return assignment;
            }

            protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
            {
                IEnumerable<MemberBinding> bindings = this.VisitBindingList(binding.Bindings);
                if (bindings != binding.Bindings)
                {
                    return Expression.MemberBind(binding.Member, bindings);
                }
                return binding;
            }

            protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding binding)
            {
                IEnumerable<ElementInit> initializers = this.VisitElementInitializerList(binding.Initializers);
                if (initializers != binding.Initializers)
                {
                    return Expression.ListBind(binding.Member, initializers);
                }
                return binding;
            }

            protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
            {
                List<MemberBinding> list = null;
                for (int i = 0, n = original.Count; i < n; i++)
                {
                    MemberBinding b = this.VisitBinding(original[i]);
                    if (list != null)
                    {
                        list.Add(b);
                    }
                    else if (b != original[i])
                    {
                        list = new List<MemberBinding>(n);
                        for (int j = 0; j < i; j++)
                        {
                            list.Add(original[j]);
                        }
                        list.Add(b);
                    }
                }
                if (list != null)
                    return list;
                return original;
            }

            protected virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
            {
                List<ElementInit> list = null;
                for (int i = 0, n = original.Count; i < n; i++)
                {
                    ElementInit init = this.VisitElementInitializer(original[i]);
                    if (list != null)
                    {
                        list.Add(init);
                    }
                    else if (init != original[i])
                    {
                        list = new List<ElementInit>(n);
                        for (int j = 0; j < i; j++)
                        {
                            list.Add(original[j]);
                        }
                        list.Add(init);
                    }
                }
                if (list != null)
                    return list;
                return original;
            }

            protected virtual Expression VisitLambda(LambdaExpression lambda)
            {
                Expression body = this.Visit(lambda.Body);
                if (body != lambda.Body)
                {
                    return Expression.Lambda(lambda.Type, body, lambda.Parameters);
                }
                return lambda;
            }

            protected virtual NewExpression VisitNew(NewExpression nex)
            {
                IEnumerable<Expression> args = this.VisitExpressionList(nex.Arguments);
                if (args != nex.Arguments)
                {
                    if (nex.Members != null)
                        return Expression.New(nex.Constructor, args, nex.Members);
                    else
                        return Expression.New(nex.Constructor, args);
                }
                return nex;
            }

            protected virtual Expression VisitMemberInit(MemberInitExpression init)
            {
                NewExpression n = this.VisitNew(init.NewExpression);
                IEnumerable<MemberBinding> bindings = this.VisitBindingList(init.Bindings);
                if (n != init.NewExpression || bindings != init.Bindings)
                {
                    return Expression.MemberInit(n, bindings);
                }
                return init;
            }

            protected virtual Expression VisitListInit(ListInitExpression init)
            {
                NewExpression n = this.VisitNew(init.NewExpression);
                IEnumerable<ElementInit> initializers = this.VisitElementInitializerList(init.Initializers);
                if (n != init.NewExpression || initializers != init.Initializers)
                {
                    return Expression.ListInit(n, initializers);
                }
                return init;
            }

            protected virtual Expression VisitNewArray(NewArrayExpression na)
            {
                IEnumerable<Expression> exprs = this.VisitExpressionList(na.Expressions);
                if (exprs != na.Expressions)
                {
                    if (na.NodeType == ExpressionType.NewArrayInit)
                    {
                        return Expression.NewArrayInit(na.Type.GetElementType(), exprs);
                    }
                    else
                    {
                        return Expression.NewArrayBounds(na.Type.GetElementType(), exprs);
                    }
                }
                return na;
            }

            protected virtual Expression VisitInvocation(InvocationExpression iv)
            {
                IEnumerable<Expression> args = this.VisitExpressionList(iv.Arguments);
                Expression expr = this.Visit(iv.Expression);
                if (args != iv.Arguments || expr != iv.Expression)
                {
                    return Expression.Invoke(expr, args);
                }
                return iv;
            }
        }

        private class ExpressionStringBuilder : ExpressionVisitor
        {
            public static string GetExpressionString(Expression expression)
            {
                var builder = new ExpressionStringBuilder();
                builder.Visit(expression);
                return builder.stringBuilder.ToString();
            }

            private readonly StringBuilder stringBuilder = new StringBuilder();

            protected override Expression VisitBinary(BinaryExpression b)
            {
                if (b.NodeType == ExpressionType.ArrayIndex)
                {
                    this.Visit(b.Left);
                    this.stringBuilder.Append("[");
                    this.Visit(b.Right);
                    this.stringBuilder.Append("]");
                }
                else
                {
                    this.Visit(b.Left);
                    this.stringBuilder.Append(" " + GetBinaryOperatorText(b.NodeType) + " ");
                    this.Visit(b.Right);
                }

                return b;
            }

            protected override Expression VisitConditional(ConditionalExpression c)
            {
                this.Visit(c.Test);
                this.stringBuilder.Append(" ? ");
                this.Visit(c.IfTrue);
                this.stringBuilder.Append(" : ");
                this.Visit(c.IfFalse);

                return c;
            }

            protected override Expression VisitConstant(ConstantExpression c)
            {
                this.stringBuilder.Append(GetValue(c.Value));
                return c;
            }

            protected override ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
            {
                bool first = true;

                foreach (var expression in original)
                {
                    if (!first) this.stringBuilder.Append(", ");

                    this.Visit(expression);

                    first = false;
                }

                return original;
            }

            protected override Expression VisitMemberAccess(MemberExpression m)
            {
                if (m.Expression == null)
                {
                    this.stringBuilder.Append(m.Member.DeclaringType.Name + ".");
                }
                else if (ShouldReport(m.Expression))
                {
                    this.Visit(m.Expression);
                    this.stringBuilder.Append(".");
                }

                this.stringBuilder.Append(m.Member.Name);

                return m;
            }

            protected override Expression VisitMemberInit(MemberInitExpression init)
            {
                this.stringBuilder.Append("new " + init.Type.Name + " { ");

                bool first = true;

                foreach (var binding in init.Bindings)
                {
                    if (!first) this.stringBuilder.Append(", ");

                    this.stringBuilder.Append(binding.Member.Name + " = ");

                    if (binding.BindingType == MemberBindingType.Assignment)
                    {
                        this.Visit(((MemberAssignment)binding).Expression);
                    }

                    first = false;
                }

                this.stringBuilder.Append(" }");

                return init;
            }

            protected override Expression VisitMethodCall(MethodCallExpression m)
            {
                if (IsExtensionMethod(m.Method))
                {
                    this.Visit(m.Arguments[0]);
                    this.stringBuilder.Append(".");
                    this.stringBuilder.Append(m.Method.Name + "(");
                    this.VisitExpressionList(new ReadOnlyCollection<Expression>(m.Arguments.Skip(1).ToList()));
                    this.stringBuilder.Append(")");

                    return m;
                }

                bool mightNeedDot = false;

                if (m.Object == null)
                {
                    this.stringBuilder.Append(m.Method.DeclaringType.Name + ".");
                }
                else if (ShouldReport(m.Object))
                {
                    this.Visit(m.Object);
                    mightNeedDot = true;
                }

                // TODO: Isn't it possible to name the indexer something other than Item?
                if (m.Method.Name == "get_Item")
                {
                    this.stringBuilder.Append("[");
                    this.VisitExpressionList(m.Arguments);
                    this.stringBuilder.Append("]");
                }
                else
                {
                    if (mightNeedDot)
                    {
                        this.stringBuilder.Append(".");
                    }

                    this.stringBuilder.Append(m.Method.Name + "(");
                    this.VisitExpressionList(m.Arguments);
                    this.stringBuilder.Append(")");
                }

                return m;
            }

            protected override Expression VisitUnary(UnaryExpression u)
            {
                if (u.NodeType == ExpressionType.ArrayLength)
                {
                    this.Visit(u.Operand);
                    this.stringBuilder.Append(".Length");
                }
                else if (u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.ConvertChecked)
                {
                    this.stringBuilder.Append("(" + GetCSharpTypeName(u.Type) + ")");
                    this.Visit(u.Operand);
                }
                else if (u.NodeType == ExpressionType.Negate || u.NodeType == ExpressionType.NegateChecked)
                {
                    this.stringBuilder.Append("-");
                    this.Visit(u.Operand);
                }
                else if (u.NodeType == ExpressionType.Not)
                {
                    this.stringBuilder.Append("!");
                    this.Visit(u.Operand);
                }
                else if (u.NodeType == ExpressionType.TypeAs)
                {
                    this.Visit(u.Operand);
                    this.stringBuilder.Append(" as " + GetCSharpTypeName(u.Type));
                }

                return u;
            }

            private static bool ShouldReport(Expression expression)
            {
                return expression != null &&
                       (expression.NodeType != ExpressionType.Constant ||
                        expression.Type.IsPrimitive ||
                        expression.Type == typeof(string));
            }

            public static string GetValue(object value)
            {
                if (value is bool)
                {
                    return (bool)value ? "true" : "false";
                }

                if (value is DateTime)
                {
                    return ((DateTime)value).ToString("s");
                }

                if (value is string)
                {
                    return "\"" + EscapeSpecialCharacters((string)value) + "\"";
                }

                if (value is Type)
                {
                    return "typeof(" + GetCSharpTypeName((Type)value) + ")";
                }

                return value.ToString();
            }

            private static string EscapeSpecialCharacters(string value)
            {
                var sb = new StringBuilder();

                foreach (char c in value)
                {
                    switch (c)
                    {
                        case '\"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\0': sb.Append("\\0"); break;
                        case '\a': sb.Append("\\a"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        case '\v': sb.Append("\\v"); break;
                        default: sb.Append(c); break;
                    }
                }

                return sb.ToString();
            }

            private static string GetBinaryOperatorText(ExpressionType type)
            {
                switch (type)
                {
                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                        return "+";
                    case ExpressionType.And:
                        return "&";
                    case ExpressionType.AndAlso:
                        return "&&";
                    case ExpressionType.Coalesce:
                        return "??";
                    case ExpressionType.Divide:
                        return "/";
                    case ExpressionType.Equal:
                        return "==";
                    case ExpressionType.ExclusiveOr:
                        return "^";
                    case ExpressionType.GreaterThan:
                        return ">";
                    case ExpressionType.GreaterThanOrEqual:
                        return ">=";
                    case ExpressionType.LeftShift:
                        return "<<";
                    case ExpressionType.LessThan:
                        return "<";
                    case ExpressionType.LessThanOrEqual:
                        return "<=";
                    case ExpressionType.Modulo:
                        return "%";
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                        return "*";
                    case ExpressionType.NotEqual:
                        return "!=";
                    case ExpressionType.Or:
                        return "|";
                    case ExpressionType.OrElse:
                        return "||";
                    case ExpressionType.Power:
                        return "^";
                    case ExpressionType.RightShift:
                        return ">>";
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                        return "-";
                    default:
                        return null;
                }
            }

            private static string GetCSharpTypeName(Type type)
            {
                if (type == typeof(string)) return "string";
                if (type == typeof(int)) return "int";
                return type.Name;
            }
        }
    }

    public delegate void MessageReporter(string message);

    public delegate string MessageFormatter(string expression, string be, string expected, string was, string actual);

    public class VerificationException : Exception
    {
        private static readonly Regex StackTraceFilter = new Regex(@"VerifyThat(?!\.(Tests))\.");

        public VerificationException(string message)
            : base(message)
        {
        }

        private string FilterStackTrace()
        {
            var sb = new StringBuilder();

            var sr = new StringReader(base.StackTrace);

            string line;

            while ((line = sr.ReadLine()) != null)
            {
                if (!StackTraceFilter.IsMatch(line))
                {
                    sb.AppendLine(line);
                }
            }

            return sb.ToString();
        }

        public override string StackTrace
        {
            get { return this.FilterStackTrace(); }
        }
    }

    public class CustomExtensionMethodVerificationException : Exception
    {
        public CustomExtensionMethodVerificationException(string beText, string expectedText, string wasText, string actualText)
        {
            this.BeText = beText;
            this.ExpectedText = expectedText;
            this.WasText = wasText;
            this.ActualText = actualText;
        }

        public string BeText { get; set; }
        public string ExpectedText { get; private set; }
        public string WasText { get; private set; }
        public string ActualText { get; private set; }
    }

    public static class EnumerableExtensions
    {
        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            int count = source.Count();

            if (count == 0) return true;

            throw new CustomExtensionMethodVerificationException("be", "empty", "contained", string.Format("{0} items", count));
        }
    }
}
