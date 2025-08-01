﻿// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

using ExpressionExtensions = Microsoft.EntityFrameworkCore.Query.ExpressionExtensions;

namespace EntityFrameworkCore.Jet.Query.ExpressionTranslators.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class JetStringMethodTranslator(ISqlExpressionFactory sqlExpressionFactory) : IMethodCallTranslator
    {
        private readonly JetSqlExpressionFactory _sqlExpressionFactory = (JetSqlExpressionFactory)sqlExpressionFactory;

        private static readonly MethodInfo IndexOfMethodInfoString
            = typeof(string).GetRuntimeMethod(nameof(string.IndexOf), [typeof(string)])!;

        private static readonly MethodInfo IndexOfMethodInfoChar
            = typeof(string).GetRuntimeMethod(nameof(string.IndexOf), [typeof(char)])!;

        private static readonly MethodInfo IndexOfMethodInfoWithStartingPositionString
            = typeof(string).GetRuntimeMethod(nameof(string.IndexOf), [typeof(string), typeof(int)])!;

        private static readonly MethodInfo IndexOfMethodInfoWithStartingPositionChar
            = typeof(string).GetRuntimeMethod(nameof(string.IndexOf), [typeof(char), typeof(int)])!;

        private static readonly MethodInfo ReplaceMethodInfoString
            = typeof(string).GetRuntimeMethod(nameof(string.Replace), [typeof(string), typeof(string)])!;

        private static readonly MethodInfo ReplaceMethodInfoChar
            = typeof(string).GetRuntimeMethod(nameof(string.Replace), [typeof(char), typeof(char)])!;

        private static readonly MethodInfo _toLowerMethodInfo
            = typeof(string).GetRuntimeMethod(nameof(string.ToLower), Type.EmptyTypes)!;

        private static readonly MethodInfo _toUpperMethodInfo
            = typeof(string).GetRuntimeMethod(nameof(string.ToUpper), Type.EmptyTypes)!;

        private static readonly MethodInfo _substringMethodInfoWithOneArg
            = typeof(string).GetRuntimeMethod(nameof(string.Substring), [typeof(int)])!;

        private static readonly MethodInfo _substringMethodInfoWithTwoArgs
            = typeof(string).GetRuntimeMethod(nameof(string.Substring), [typeof(int), typeof(int)])!;

        private static readonly MethodInfo _isNullOrEmptyMethodInfo
            = typeof(string).GetRuntimeMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!;

        private static readonly MethodInfo _isNullOrWhiteSpaceMethodInfo
            = typeof(string).GetRuntimeMethod(nameof(string.IsNullOrWhiteSpace), [typeof(string)])!;

        // Method defined in netcoreapp2.0 only
        private static readonly MethodInfo _trimStartMethodInfoWithoutArgs
            = typeof(string).GetRuntimeMethod(nameof(string.TrimStart), Type.EmptyTypes)!;

        private static readonly MethodInfo _trimEndMethodInfoWithoutArgs
            = typeof(string).GetRuntimeMethod(nameof(string.TrimEnd), Type.EmptyTypes)!;

        private static readonly MethodInfo _trimMethodInfoWithoutArgs
            = typeof(string).GetRuntimeMethod(nameof(string.Trim), Type.EmptyTypes)!;

        // Method defined in netstandard2.0
        private static readonly MethodInfo _trimStartMethodInfoWithCharArrayArg
            = typeof(string).GetRuntimeMethod(nameof(string.TrimStart), [typeof(char[])])!;

        private static readonly MethodInfo _trimEndMethodInfoWithCharArrayArg
            = typeof(string).GetRuntimeMethod(nameof(string.TrimEnd), [typeof(char[])])!;

        private static readonly MethodInfo _trimMethodInfoWithCharArrayArg
            = typeof(string).GetRuntimeMethod(nameof(string.Trim), [typeof(char[])])!;

        private static readonly MethodInfo _firstOrDefaultMethodInfoWithoutArgs
            = typeof(Enumerable).GetRuntimeMethods().Single(
                m => m.Name == nameof(Enumerable.FirstOrDefault)
                     && m.GetParameters().Length == 1).MakeGenericMethod(typeof(char));

        private static readonly MethodInfo _lastOrDefaultMethodInfoWithoutArgs
            = typeof(Enumerable).GetRuntimeMethods().Single(
                m => m.Name == nameof(Enumerable.LastOrDefault)
                     && m.GetParameters().Length == 1).MakeGenericMethod(typeof(char));


        public SqlExpression? Translate(SqlExpression? instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            if (instance != null)
            {
                if (IndexOfMethodInfoString.Equals(method) || IndexOfMethodInfoChar.Equals(method))
                {
                    return TranslateIndexOf(instance, method, arguments[0], null);
                }

                if (IndexOfMethodInfoWithStartingPositionString.Equals(method) || IndexOfMethodInfoWithStartingPositionChar.Equals(method))
                {
                    return TranslateIndexOf(instance, method, arguments[0], arguments[1]);
                }

                if (ReplaceMethodInfoString.Equals(method) || ReplaceMethodInfoChar.Equals(method))
                {
                    var firstArgument = arguments[0];
                    var secondArgument = arguments[1];
                    var stringTypeMapping =
                        ExpressionExtensions.InferTypeMapping(instance, firstArgument, secondArgument);

                    instance = _sqlExpressionFactory.ApplyTypeMapping(instance, stringTypeMapping);
                    firstArgument = _sqlExpressionFactory.ApplyTypeMapping(firstArgument, firstArgument.Type == typeof(char) ? CharTypeMapping.Default : stringTypeMapping);
                    secondArgument = _sqlExpressionFactory.ApplyTypeMapping(secondArgument, secondArgument.Type == typeof(char) ? CharTypeMapping.Default : stringTypeMapping);

                    return _sqlExpressionFactory.Function(
                        "REPLACE",
                        [instance, firstArgument, secondArgument],
                        nullable: true,
                        argumentsPropagateNullability: [true, true, true],
                        method.ReturnType,
                        stringTypeMapping);
                }

                // Jet TRIM does not take arguments.
                // _trimWithNoParam is only available since .NET Core 2.0 (or .NET Standard 2.1).
                if (Equals(method, _trimMethodInfoWithoutArgs) ||
                    Equals(method, _trimMethodInfoWithCharArrayArg) &&
                    (arguments[0] as SqlConstantExpression)?.Value is Array { Length: 0 })
                {
                    return _sqlExpressionFactory.Function("TRIM", [instance], true, [true],
                        instance.Type, instance.TypeMapping);
                }

                // Jet LTRIM does not take arguments
                // _trimStartWithNoParam is only available since .NET Core 2.0 (or .NET Standard 2.1).
                if (Equals(method, _trimStartMethodInfoWithoutArgs) ||
                    Equals(method, _trimStartMethodInfoWithCharArrayArg) &&
                    (arguments[0] as SqlConstantExpression)?.Value is Array { Length: 0 })
                {
                    return _sqlExpressionFactory.Function("LTRIM", [instance], true, [true],
                        instance.Type, instance.TypeMapping);
                }

                // Jet RTRIM does not take arguments
                // _trimEndWithNoParam is only available since .NET Core 2.0 (or .NET Standard 2.1).
                if (Equals(method, _trimEndMethodInfoWithoutArgs) ||
                    Equals(method, _trimEndMethodInfoWithCharArrayArg) &&
                    (arguments[0] as SqlConstantExpression)?.Value is Array { Length: 0 })
                {
                    return _sqlExpressionFactory.Function("RTRIM", [instance], true, [true],
                        instance.Type, instance.TypeMapping);
                }

                if (_toLowerMethodInfo.Equals(method)
                    || _toUpperMethodInfo.Equals(method))
                {
                    return _sqlExpressionFactory.Function(
                        _toLowerMethodInfo.Equals(method) ? "LCASE" : "UCASE",
                        [instance],
                        nullable: true,
                        argumentsPropagateNullability: [true],
                        method.ReturnType,
                        instance.TypeMapping);
                }

                if (_substringMethodInfoWithOneArg.Equals(method))
                {
                    return _sqlExpressionFactory.Function(
                        "MID",
                        [
                            instance,
                            _sqlExpressionFactory.Add(
                                arguments[0],
                                _sqlExpressionFactory.Constant(1)),
                            _sqlExpressionFactory.Coalesce(
                                _sqlExpressionFactory.Function(
                                    "LEN",
                                    [instance],
                                    nullable: true,
                                    argumentsPropagateNullability: [true],
                                    typeof(int)),
                                _sqlExpressionFactory.Constant(0)
                            )
                        ],
                        nullable: true,
                        argumentsPropagateNullability: [true, true, true],
                        method.ReturnType,
                        instance.TypeMapping);
                }

                if (_substringMethodInfoWithTwoArgs.Equals(method))
                {
                    return _sqlExpressionFactory.Function(
                        "MID",
                        [
                            instance,
                            _sqlExpressionFactory.Add(
                                arguments[0],
                                _sqlExpressionFactory.Constant(1)),
                            arguments[1]
                        ],
                        nullable: true,
                        argumentsPropagateNullability: [true, true, true],
                        method.ReturnType,
                        instance.TypeMapping);
                }
            }

            if (_isNullOrEmptyMethodInfo.Equals(method))
            {
                var argument = arguments[0];

                return _sqlExpressionFactory.OrElse(
                    _sqlExpressionFactory.IsNull(argument),
                    _sqlExpressionFactory.Like(
                        argument,
                        _sqlExpressionFactory.Constant(string.Empty)));
            }

            if (_isNullOrWhiteSpaceMethodInfo.Equals(method))
            {
                var argument = arguments[0];

                return _sqlExpressionFactory.OrElse(
                    _sqlExpressionFactory.IsNull(argument),
                    _sqlExpressionFactory.Equal(
                        argument,
                        _sqlExpressionFactory.Constant(string.Empty, argument.TypeMapping)));
            }

            if (_firstOrDefaultMethodInfoWithoutArgs.Equals(method))
            {
                var argument = arguments[0];
                return _sqlExpressionFactory.Function(
                    "MID",
                    [argument, _sqlExpressionFactory.Constant(1), _sqlExpressionFactory.Constant(1)],
                    nullable: true,
                    argumentsPropagateNullability: [true, true, true],
                    method.ReturnType);
            }

            if (_lastOrDefaultMethodInfoWithoutArgs.Equals(method))
            {
                var argument = arguments[0];
                var lenfunction = _sqlExpressionFactory.Function(
                    "LEN",
                    [argument],
                    nullable: true,
                    argumentsPropagateNullability: [true],
                    typeof(int));
                var casefunc = _sqlExpressionFactory.Case(
                    [
                        new CaseWhenClause(
                            _sqlExpressionFactory.Equal(
                                lenfunction,
                                _sqlExpressionFactory.Constant(0)),
                            _sqlExpressionFactory.Constant(1))
                    ],
                    lenfunction);
                return _sqlExpressionFactory.Function(
                    "MID",
                    [
                            argument,
                            _sqlExpressionFactory.Coalesce(
                                casefunc,
                                _sqlExpressionFactory.Constant(0)
                            ),
                            _sqlExpressionFactory.Constant(1)
                    ],
                    nullable: true,
                    argumentsPropagateNullability: [true, true, true],
                    method.ReturnType);
            }

            return null;
        }

        private SqlExpression TranslateIndexOf(
        SqlExpression instance,
        MethodInfo method,
        SqlExpression searchExpression,
        SqlExpression? startIndex)
        {
            var stringTypeMapping = ExpressionExtensions.InferTypeMapping(instance, searchExpression)!;
            searchExpression = _sqlExpressionFactory.ApplyTypeMapping(searchExpression, searchExpression.Type == typeof(char) ? CharTypeMapping.Default : stringTypeMapping);
            instance = _sqlExpressionFactory.ApplyTypeMapping(instance, stringTypeMapping);

            var charIndexArguments = new List<SqlExpression> { instance, searchExpression };

            if (startIndex is not null)
            {
                charIndexArguments.Insert(0,
                    startIndex is SqlConstantExpression { Value: int constantStartIndex }
                        ? _sqlExpressionFactory.Constant(constantStartIndex + 1, typeof(int))
                        : _sqlExpressionFactory.Add(startIndex, _sqlExpressionFactory.Constant(1)));
            }
            else
            {
                charIndexArguments.Insert(0, _sqlExpressionFactory.Constant(1));
            }
            charIndexArguments.Add(_sqlExpressionFactory.Constant(1));

            var argumentsPropagateNullability = Enumerable.Repeat(true, charIndexArguments.Count);

            SqlExpression charIndexExpression = charIndexExpression = _sqlExpressionFactory.Function(
                "INSTR",
                charIndexArguments,
                nullable: true,
                argumentsPropagateNullability,
                method.ReturnType);

            // If the pattern is an empty string, we need to special case to always return 0 (since CHARINDEX return 0, which we'd subtract to
            // -1). Handle separately for constant and non-constant patterns.
            if (searchExpression is SqlConstantExpression { Value: "" })
            {
                return _sqlExpressionFactory.Case(
                    [new CaseWhenClause(_sqlExpressionFactory.IsNotNull(instance), _sqlExpressionFactory.Constant(0))],
                    elseResult: null
                );
            }

            SqlExpression offsetExpression = searchExpression is SqlConstantExpression
                ? _sqlExpressionFactory.Constant(1)
                : _sqlExpressionFactory.Case(
                    [
                        new CaseWhenClause(
                            _sqlExpressionFactory.Equal(
                                searchExpression,
                                _sqlExpressionFactory.Constant(string.Empty, stringTypeMapping)),
                            _sqlExpressionFactory.Constant(0))
                    ],
                    _sqlExpressionFactory.Constant(1));


            return _sqlExpressionFactory.Subtract(charIndexExpression, offsetExpression);
        }
    }
}