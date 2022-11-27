using System.Linq.Expressions;
using static Weasel.Postgresql.TokenParser;

namespace MartenWebApplication1
{
    public static class ExpressionHelpersExtensions
    {
        public static Expression ReplaceParameter(this Expression expression, ParameterExpression source, Expression target)
        {
            return new ParameterReplacer { Source = source, Target = target }.Visit(expression);
        }

        class ParameterReplacer : ExpressionVisitor
        {
            public ParameterExpression Source;
            public Expression Target;
            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == Source ? Target : base.VisitParameter(node);
            }
        }

        public static Expression<Func<TModel, bool>>? CreateCombinedAndLambda<TModel>(params Expression<Func<TModel, bool>>[] filterList)
        {
            ParameterExpression parameterExpression = Expression.Parameter(typeof(TModel));
            Expression binaryExpression = null;

            filterList.Where(p => p != null).ToList().ForEach(p =>
            {
                Expression expression = p.Body.ReplaceParameter(p.Parameters.First(), parameterExpression);
                binaryExpression = binaryExpression == null ? expression : Expression.AndAlso(binaryExpression, expression);
            });

            return binaryExpression == null
            ? null
                : Expression.Lambda<Func<TModel, bool>>(binaryExpression, parameterExpression);
        }
    }
}
