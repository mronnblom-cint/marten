﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Baseline;
using Baseline.Reflection;
using Marten.Schema;

namespace Marten.Linq
{
    public interface IMethodCallParser
    {
        bool Matches(MethodCallExpression expression);
        IWhereFragment Parse(
            IDocumentMapping mapping, 
            ISerializer serializer, 
            MethodCallExpression expression
        );
    }

    public abstract class MethodCallParser<T> : IMethodCallParser
    {
        private readonly MethodInfo _method;

        public MethodCallParser(Expression<Action<T>> method)
        {
            _method = ReflectionHelper.GetMethod(method);
        }

        public bool Matches(MethodCallExpression expression)
        {
            // You cannot use the Equals() method on any Reflection objects, they
            // only check for reference equality. Ask me how I know that;)
            return expression.Object?.Type == typeof (T) && expression.Method.Name == _method.Name;
        }

        public abstract IWhereFragment Parse(
            IDocumentMapping mapping, 
            ISerializer serializer,
            MethodCallExpression expression);
    }

    public class StringContains : MethodCallParser<string>
    {
        public StringContains() : base(x => x.Contains(null))
        {
        }

        public override IWhereFragment Parse(IDocumentMapping mapping, ISerializer serializer, MethodCallExpression expression)
        {
            var locator = mapping.JsonLocator(expression.Object);
            var value = expression.Arguments.Single().Value().As<string>();
            return new WhereFragment("{0} like ?".ToFormat(locator), "%" + value + "%");
        }
    }

    public class StringStartsWith : MethodCallParser<string>
    {
        public StringStartsWith() : base(x => x.StartsWith(null))
        {
        }

        public override IWhereFragment Parse(IDocumentMapping mapping, ISerializer serializer, MethodCallExpression expression)
        {
            var locator = mapping.JsonLocator(expression.Object);
            var value = expression.Arguments.Single().Value().As<string>();
            return new WhereFragment("{0} like ?".ToFormat(locator), value + "%");
        }
    }

    public class StringEndsWith : MethodCallParser<string>
    {
        public StringEndsWith() : base(x => x.EndsWith(null))
        {
        }

        public override IWhereFragment Parse(IDocumentMapping mapping, ISerializer serializer, MethodCallExpression expression)
        {
            var @object = expression.Object;
            var locator = mapping.JsonLocator(@object);
            var value = expression.Arguments.Single().Value().As<string>();
            return new WhereFragment("{0} like ?".ToFormat(locator), "%" + value);
        }
    }

    public class EnumerableContains : IMethodCallParser
    {
        public bool Matches(MethodCallExpression expression)
        {
            return expression.Method.Name == MartenExpressionParser.CONTAINS &&
                   expression.Object.Type.IsGenericEnumerable();
        }

        public IWhereFragment Parse(IDocumentMapping mapping, ISerializer serializer, MethodCallExpression expression)
        {
            var value = expression.Arguments.Single().Value();
            return ContainmentWhereFragment.SimpleArrayContains(serializer, expression.Object,
                value);
        }
    }

}