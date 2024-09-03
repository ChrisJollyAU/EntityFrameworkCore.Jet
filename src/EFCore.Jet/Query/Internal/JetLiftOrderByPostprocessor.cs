using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.Jet.Query.Internal;
#pragma warning disable CS9113
/// <remarks>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </remarks>
[EntityFrameworkInternal]
public class JetLiftOrderByPostprocessor(IRelationalTypeMappingSource typeMappingSource,
    ISqlExpressionFactory sqlExpressionFactory,
    SqlAliasManager sqlAliasManager)
    : ExpressionVisitor 
{
    private Stack<SelectExpression> parent = new Stack<SelectExpression>();
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
                shapedQueryExpression = shapedQueryExpression
                    .UpdateQueryExpression(Visit(shapedQueryExpression.QueryExpression))
                    .UpdateShaperExpression(Visit(shapedQueryExpression.ShaperExpression));

                return shapedQueryExpression.UpdateShaperExpression(Visit(shapedQueryExpression.ShaperExpression));
            case RelationalSplitCollectionShaperExpression relationalSplitCollectionShaperExpression:
                var newSelect = Visit(relationalSplitCollectionShaperExpression.SelectExpression);
                var newInner = Visit(relationalSplitCollectionShaperExpression.InnerShaper);
                relationalSplitCollectionShaperExpression = relationalSplitCollectionShaperExpression.Update(
                    relationalSplitCollectionShaperExpression.ParentIdentifier,
                    relationalSplitCollectionShaperExpression.ChildIdentifier, (SelectExpression)newSelect, newInner);
                return relationalSplitCollectionShaperExpression;
            case NonQueryExpression nonQueryExpression:
                return nonQueryExpression;
            case SelectExpression selectExpression:
                {
                    for (int i = 0; i < selectExpression.Tables.Count; i++)
                    {
                        if (selectExpression.Tables[i] is OuterApplyExpression outerApplyExpression)
                        {
                            var applySelect = outerApplyExpression.Table as SelectExpression;
                            var applyPredicate = applySelect?.Predicate;
                            var applyS2 = applySelect?.Tables[0] as SelectExpression;
                            if (applyPredicate is null)
                            {
                                
                                applyPredicate = applyS2?.Predicate;
                            }
                            else
                            {
                                if (applyS2?.Predicate != null)
                                {
                                    applyPredicate = sqlExpressionFactory.AndAlso(applyPredicate, applyS2.Predicate);
                                }
                            }
                            if (applyPredicate != null && applySelect != null && applySelect.Limit == null)
                            {
                                applySelect = applySelect.Update(applySelect.Tables, null, applySelect.GroupBy,
                                    applySelect.Having,applySelect.Projection, applySelect.Orderings, applySelect.Offset, applySelect.Limit);
                                applySelect = AddAliasManager(applySelect);
                                var lj = new LeftJoinExpression(applySelect, applyPredicate);
                                selectExpression = selectExpression.Update(new List<TableExpressionBase>() { selectExpression.Tables[0], lj }, selectExpression.Predicate,
                                    selectExpression.GroupBy,
                                    selectExpression.Having, selectExpression.Projection, selectExpression.Orderings, selectExpression.Offset,
                                    selectExpression.Limit);
                                selectExpression = AddAliasManager(selectExpression);
                                //selectExpression.AddLeftJoin(applySelect, applyPredicate);
                                expression = selectExpression;
                            }
                        }
                    }




                    Dictionary<int, (int? indexcol, OrderingExpression? orderexp, bool ascend, bool rewrite, bool referstocurouter)> columnsToRewrite = new();
                    bool isscalarselect = selectExpression is { Limit: SqlConstantExpression { Value: 1 }, Projection.Count: 1 };
                    for (int i = 0; i < selectExpression.Orderings.Count; i++)
                    {
                        var sqlExpression = selectExpression.Orderings[i].Expression;
                        if (sqlExpression is not ColumnExpression && sqlExpression is not SqlConstantExpression && sqlExpression is not SqlParameterExpression)
                        {
                            var locate = new JetLocateScalarSubqueryVisitor(typeMappingSource, sqlExpressionFactory);
                            var locatedExpression = locate.Visit(sqlExpression);
                            bool containsscalar = locatedExpression is ScalarSubqueryExpression or ExistsExpression;
                            if (locatedExpression is ExistsExpression existsExpression)
                            {
                                var ncc = sqlExpressionFactory.Case(
                                    new[]
                                    {
                                        new CaseWhenClause(
                                            existsExpression,
                                            sqlExpressionFactory.ApplyDefaultTypeMapping(sqlExpressionFactory.Constant(true)))
                                    },
                                    sqlExpressionFactory.Constant(false));
                                sqlExpression = ncc;
                            }
                            if (containsscalar)
                            {
                                int index = selectExpression.AddToProjection(sqlExpression);
                                columnsToRewrite.Add(i, (index, null, selectExpression.Orderings[i].IsAscending, true, false));
                                continue;
                            }

                            var existingIndex = selectExpression.Projection.ToList().FindIndex(pe => pe.Expression.Equals(sqlExpression));
                            if (existingIndex != -1)
                            {
                                columnsToRewrite.Add(i, (existingIndex, null, selectExpression.Orderings[i].IsAscending, true, false));
                            }
                        }
                        else
                        {
                            var existingIndex = selectExpression.Projection.ToList().FindIndex(pe => pe.Expression.Equals(sqlExpression));
                            if (existingIndex != -1)
                            {
                                bool referouter = sqlExpression is ColumnExpression colexp1 &&
                                    selectExpression.Tables.Select(d => d.Alias).Contains(colexp1.TableAlias);
                                columnsToRewrite.Add(i,
                                    (existingIndex, selectExpression.Orderings[i], selectExpression.Orderings[i].IsAscending, false, referouter));
                            }
                            
                        }
                    }

                    if (columnsToRewrite.Count == 0 || columnsToRewrite.All(p => p.Value.rewrite == false))
                    {
                        parent.Push(selectExpression);
                        var result1 = base.Visit(expression);
                        parent.Pop();
                        return result1;
                    }

                    selectExpression.ClearOrdering();
                    //Keep the limit in parent expression
                    if (selectExpression.Limit != null)
                    {
                        var limit = selectExpression.Limit;
                        MethodInfo? dynMethod1 = selectExpression.GetType().GetMethod("set_Limit",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        dynMethod1?.Invoke(selectExpression, new object?[] { null });

                        //This doesn't work. Update returns a new select expression but without the sql alias manager. Pushdown requires the alias manager
                        /*selectExpression = selectExpression.Update(selectExpression.Tables,
                            selectExpression.Predicate, selectExpression.GroupBy, selectExpression.Having, selectExpression.Projection,
                            selectExpression.Orderings, null, null);*/
                        selectExpression = AddAliasManager(selectExpression);
                        selectExpression.PushdownIntoSubquery();
                        selectExpression.ApplyLimit(limit);
                    }
                    else
                    {
                        selectExpression = AddAliasManager(selectExpression);
                        selectExpression.PushdownIntoSubquery();
                    }

                    foreach (var colr in columnsToRewrite)
                    {
                        (int? index, OrderingExpression? oexp, bool ascending, bool rewrite, bool referstocurouter) = colr.Value;
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

                    if (isscalarselect && selectExpression.Projection.Count > 1)
                    {
                        List<ProjectionExpression> newProjections = [selectExpression.Projection[0]];
                        selectExpression = selectExpression.Update(selectExpression.Tables, selectExpression.Predicate,
                            selectExpression.GroupBy, selectExpression.Having, newProjections, selectExpression.Orderings,
                            selectExpression.Offset, selectExpression.Limit);
                    }
                    var result = base.Visit(selectExpression);
                    return result;
                }
            case RelationalGroupByShaperExpression relationalGroupByShaperExpression:
            {
                return base.VisitExtension(relationalGroupByShaperExpression);
            }
            case LeftJoinExpression leftJoinExpression:
            {
                if (leftJoinExpression.Table is SelectExpression joinSelectExpression)
                {
                    List<ColumnExpression> cols = new List<ColumnExpression>();
                    if (leftJoinExpression.JoinPredicate is SqlBinaryExpression binaryExpression)
                    {
                        cols = ExtractColumnExpressions(binaryExpression);
                    }
                    else if (leftJoinExpression.JoinPredicate is SqlUnaryExpression unaryExpression)
                    {
                        cols = ExtractColumnExpressions(unaryExpression);
                    }

                    var collist = cols.Distinct().Where(c => joinSelectExpression.Tables.Select(d => d.Alias).Contains(c.TableAlias)).ToList();
                    var proj = joinSelectExpression.Projection.Where(p => collist.Contains(p.Expression)).Select(p => p.Expression);
                    var collist2 = collist.Except(proj);
                    parent.TryPeek(out var pp);
                    foreach (var cl2 in collist2)
                    {
                        joinSelectExpression.AddToProjection(cl2);
                        if (joinSelectExpression.GroupBy.Count > 0)
                        {
                            joinSelectExpression.ApplyGrouping(cl2);
                        }

                        var newjoinpred = ReplaceTableForColumnPredicate(leftJoinExpression.JoinPredicate, cl2, pp ?? joinSelectExpression, leftJoinExpression);
                        leftJoinExpression = leftJoinExpression.Update(leftJoinExpression.Table, newjoinpred);
                    }
                }
                var result = base.Visit(leftJoinExpression);
                return result;
            }
        }

        return base.Visit(expression);
    }

    private SqlExpression ReplaceTableForColumnPredicate(SqlExpression originalExpression, SqlExpression cl2, SelectExpression joinSelectExpression, LeftJoinExpression leftJoinExpression)
    {
        if (originalExpression is SqlBinaryExpression sqlBinary)
        {
            var left = sqlBinary.Left;
            var right = sqlBinary.Right;
            var op = sqlBinary.OperatorType;
            if (left.Equals(cl2) && cl2 is ColumnExpression lCol)
            {
                left = joinSelectExpression.CreateColumnExpression(leftJoinExpression, lCol.Name, lCol.Type,
                    lCol.TypeMapping, lCol.IsNullable);
            }
            else
            {
                left = ReplaceTableForColumnPredicate(left, cl2, joinSelectExpression, leftJoinExpression);
            }

            if (right.Equals(cl2) && cl2 is ColumnExpression rCol)
            {
                right = joinSelectExpression.CreateColumnExpression(leftJoinExpression, rCol.Name, rCol.Type, rCol.TypeMapping, rCol.IsNullable);

            }
            else
            {
                right = ReplaceTableForColumnPredicate(right, cl2, joinSelectExpression, leftJoinExpression);
            }

            return new SqlBinaryExpression(op, left, right, sqlBinary.Type, sqlBinary.TypeMapping);
        }

        return originalExpression;
    }

    private List<ColumnExpression> ExtractColumnExpressions(SqlBinaryExpression binaryexp)
    {
        List<ColumnExpression> result = new List<ColumnExpression>();
        if (binaryexp.Left is SqlBinaryExpression left)
        {
            result.AddRange(ExtractColumnExpressions(left));
        }
        else if (binaryexp.Left is ColumnExpression colLeft)
        {
            result.Add(colLeft);
        }

        if (binaryexp.Right is SqlBinaryExpression right)
        {
            result.AddRange(ExtractColumnExpressions(right));
        }
        else if (binaryexp.Right is ColumnExpression colRight)
        {
            result.Add(colRight);
        }

        return result;
    }
    private List<ColumnExpression> ExtractColumnExpressions(SqlUnaryExpression unaryexp)
    {
        List<ColumnExpression> result = new List<ColumnExpression>();
        if (unaryexp.Operand is SqlBinaryExpression left)
        {
            result.AddRange(ExtractColumnExpressions(left));
        }
        else if (unaryexp.Operand is ColumnExpression colLeft)
        {
            result.Add(colLeft);
        }

        return result;
    }

    private SelectExpression AddAliasManager(SelectExpression selectExpression)
    {
        //get private IsMutable property
        var ismutable = selectExpression.GetType().GetProperty("IsMutable", BindingFlags.NonPublic | BindingFlags.Instance);
        //get value
        var ismut = (bool)ismutable?.GetValue(selectExpression)!;

        //create new selectexp from selectexpression with aliasmanager
        var newselect = new SelectExpression(selectExpression.Alias,
            selectExpression.Tables.ToList(),
            selectExpression.Predicate, selectExpression.GroupBy.ToList(), selectExpression.Having,
            selectExpression.Projection.ToList(), selectExpression.IsDistinct,
            selectExpression.Orderings.ToList(), selectExpression.Offset, selectExpression.Limit,
            selectExpression.Tags, new Dictionary<string, IAnnotation>(), sqlAliasManager, ismut);

        //do private stuff
        //_projectionMapping = newProjectionMappings,
        //_clientProjections = newClientProjections,
        var clientProj = selectExpression.GetType().GetField("_clientProjections", BindingFlags.NonPublic | BindingFlags.Instance);
        var projMap = selectExpression.GetType().GetField("_projectionMapping", BindingFlags.NonPublic | BindingFlags.Instance);
        clientProj?.SetValue(newselect, clientProj.GetValue(selectExpression));
        projMap?.SetValue(newselect, projMap.GetValue(selectExpression));
        return newselect;
    }
}
