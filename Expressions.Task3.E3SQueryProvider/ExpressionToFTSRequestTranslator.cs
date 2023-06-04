using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;

        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            return _resultStringBuilder.ToString();
        }

        #region protected methods

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable)
                && node.Method.Name == "Where")
            {
                var predicate = node.Arguments[1];
                Visit(predicate);

                return node;
            }

            if ( node.Method.Name == "Equals")
            {
                Visit(node.Object);
                var predicate = node.Arguments[0];
                _resultStringBuilder.Append("(");
                Visit(predicate);
                _resultStringBuilder.Append(")");

                return node;
            }

            if (node.Method.Name == "StartsWith")
            {
                Visit(node.Object);
                var predicate = node.Arguments[0];
                _resultStringBuilder.Append("(");
                Visit(predicate);
                _resultStringBuilder.Append("*)");

                return node;
            }

            if (node.Method.Name == "EndsWith")
            {
                Visit(node.Object);
                var predicate = node.Arguments[0];
                _resultStringBuilder.Append("(*");
                Visit(predicate);
                _resultStringBuilder.Append(")");

                return node;
            }

            if (node.Method.Name == "Contains")
            {
                Visit(node.Object);
                var predicate = node.Arguments[0];
                _resultStringBuilder.Append("(*");
                Visit(predicate);
                _resultStringBuilder.Append("*)");

                return node;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            BinaryExpression changedNode = node;

            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    if (node.Left.NodeType == ExpressionType.Constant)
					{
                        changedNode = node.Update(node.Right, node.Conversion, node.Left);
                        Visit(changedNode);
                        break;
                    }

                    if (changedNode.Left.NodeType != ExpressionType.MemberAccess)
                        throw new NotSupportedException($"Left operand should be property or field: {changedNode.NodeType}");

                    if (changedNode.Right.NodeType != ExpressionType.Constant)
                        throw new NotSupportedException($"Right operand should be constant: {changedNode.NodeType}");

                    Visit(changedNode.Left);
                    _resultStringBuilder.Append("(");
                    Visit(changedNode.Right);
                    _resultStringBuilder.Append(")");
                    break;

                default:
                    throw new NotSupportedException($"Operation '{changedNode.NodeType}' is not supported");
            };

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(":");

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);

            return node;
        }

        #endregion
    }
}
