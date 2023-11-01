using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using EntityFrameworkCore.Jet.Utilities;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.Jet.Query.Internal;

/// <remarks>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </remarks>
public class JetLiftOrderByPostprocessor : ExpressionVisitor
{
    private readonly IRelationalTypeMappingSource _typeMappingSource;
    private readonly ISqlExpressionFactory _sqlExpressionFactory;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public JetLiftOrderByPostprocessor(
        IRelationalTypeMappingSource typeMappingSource,
        ISqlExpressionFactory sqlExpressionFactory)
    {
        (_typeMappingSource, _sqlExpressionFactory) = (typeMappingSource, sqlExpressionFactory);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual Expression Process(Expression expression)
    {
        return Visit(expression);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    [return: NotNullIfNotNull("expression")]
    public override Expression? Visit(Expression? expression)
    {
        switch (expression)
        {
            case ShapedQueryExpression shapedQueryExpression:
                return shapedQueryExpression.UpdateQueryExpression(Visit(shapedQueryExpression.QueryExpression));
            case SelectExpression selectExpression:
                {
                    Dictionary<int, (int? indexcol, OrderingExpression? orderexp, bool ascend, bool rewrite, bool referstocurouter)> columnsToRewrite = new();

                    for (int i = 0; i < selectExpression.Orderings.Count; i++)
                    {
                        var sqlExpression = selectExpression.Orderings[i].Expression;
                        if (sqlExpression is not ColumnExpression)
                        {
                            bool containsscalar = sqlExpression is ScalarSubqueryExpression;
                            containsscalar = containsscalar || TryPeekFunction(sqlExpression as SqlFunctionExpression) || TryPeekCase(sqlExpression as CaseExpression) || TryPeekExists(sqlExpression as ExistsExpression);
                            if (containsscalar)
                            {
                                int index = selectExpression.AddToProjection(sqlExpression);
                                columnsToRewrite.Add(i, (index, null, selectExpression.Orderings[i].IsAscending, true, false));
                            }
                        }
                        else
                        {
                            var foundproj = selectExpression.Projection.FirstOrDefault(p =>
                                p.Expression.Equals(sqlExpression));
                            if (foundproj == null && sqlExpression is ColumnExpression colexp && !selectExpression.Tables.Contains(colexp.Table))
                            {
                                var ix = selectExpression.AddToProjection(sqlExpression);
                                columnsToRewrite.Add(i, (ix, null, selectExpression.Orderings[i].IsAscending, false, false));
                            }
                            else
                            {
                                bool referouter = sqlExpression is ColumnExpression colexp1 &&
                                                  selectExpression.Tables.Contains(colexp1.Table);
                                columnsToRewrite.Add(i,
                                    (null, selectExpression.Orderings[i], selectExpression.Orderings[i].IsAscending, false, referouter));
                            }
                        }
                    }

                    if (columnsToRewrite.Count == 0 || columnsToRewrite.All(p => p.Value.rewrite == false))
                    {
                        return base.Visit(expression);
                    }

                    for (int A = 0; A < columnsToRewrite.Count; A++)
                    {
                        if (!columnsToRewrite[A].referstocurouter)
                        {
                            continue;
                        }
                        var col = columnsToRewrite[A].orderexp!.Expression as ColumnExpression;
                        var colitem = columnsToRewrite[A];
                        colitem.indexcol = selectExpression.AddToProjection(col!);
                        columnsToRewrite[A] = colitem;
                    }

                    selectExpression.ClearOrdering();
                    selectExpression.PushdownIntoSubquery();

                    for (int j = 0; j < columnsToRewrite.Count; j++)
                    {
                        (int? index, OrderingExpression? oexp, bool ascending, bool rewrite, bool referstocurouter) = columnsToRewrite[j];
                        if (index.HasValue)
                        {
                            var proj = selectExpression.Projection[index.Value];
                            selectExpression.AppendOrdering(new OrderingExpression(proj.Expression, ascending));
                        }
                        else if (oexp != null)
                        {
                            var col = oexp.Expression as ColumnExpression;
                            var newcolexp = selectExpression.CreateColumnExpression(selectExpression.Tables[0], col!.Name, col.Type,
                                col.TypeMapping, col.IsNullable);
                            selectExpression.AppendOrdering(new OrderingExpression(newcolexp, ascending));
                        }
                    }
                    var result = base.Visit(selectExpression);
                    return result;
                }
        }

        return base.Visit(expression);
    }

    private bool TryPeekExists(ExistsExpression? existsExpression)
    {
        return existsExpression is not null;
    }

    private bool TryPeekCase(CaseExpression? sqlExpression)
    {
        if (sqlExpression is null) return false;
        if (sqlExpression.ElseResult is ScalarSubqueryExpression) return true;
        if (sqlExpression.WhenClauses.Any(x => x.Result is ScalarSubqueryExpression)) return true;
        if (sqlExpression.Operand is ScalarSubqueryExpression) return true;
        return false;
    }

    private bool TryPeekFunction(SqlFunctionExpression? sqlExpression)
    {
        if (sqlExpression is null) return false;
        return sqlExpression.Arguments != null && sqlExpression.Arguments.Any(x => x is ScalarSubqueryExpression);
    }
}