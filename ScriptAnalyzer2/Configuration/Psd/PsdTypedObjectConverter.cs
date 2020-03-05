using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration.Psd
{
    public abstract class PsdTypeConverter
    {
        public abstract bool CanConvert(Type type);

        public abstract object ConvertPsdType(Type type, Ast psdAst);
    }

    public abstract class PsdTypeConverter<TTarget> : PsdTypeConverter
    {
        public abstract TTarget ConvertPsdType(Ast psdAst);

        public override bool CanConvert(Type type)
        {
            return type is TTarget;
        }

        public override object ConvertPsdType(Type type, Ast psdAst)
        {
            return ConvertPsdType(psdAst);
        }
    }

    internal class PsdTypedObjectConverter
    {
        private readonly PsdDataParser _psdDataParser;

        private readonly IReadOnlyList<PsdTypeConverter> _converters;

        public PsdTypedObjectConverter(IReadOnlyList<PsdTypeConverter> converters)
        {
            _converters = converters;
            _psdDataParser = new PsdDataParser();
        }

        public PsdTypedObjectConverter() : this(converters: null)
        {
        }

        public TTarget Convert<TTarget>(Ast ast)
        {
            object conversionResult;

            if (typeof(TTarget) == typeof(object))
            {
                return (TTarget)_psdDataParser.ConvertAstValue((ExpressionAst)ast);
            }

            if (TryConvertVariableExpression(typeof(TTarget), ast, out conversionResult))
            {
                return (TTarget)conversionResult;
            }

            // Check primitive types the fast way
            switch (Type.GetTypeCode(typeof(TTarget)))
            {
                case TypeCode.String:
                    return (TTarget)(object)ConvertString(ast);

                case TypeCode.DateTime:
                    return (TTarget)(object)ConvertDateTime(ast);

                case TypeCode.DBNull:
                case TypeCode.Empty:
                    throw new ArgumentException($"Type '{typeof(TTarget).FullName}' has invalid type code '{Type.GetTypeCode(typeof(TTarget))}'");

                case TypeCode.Boolean:
                    throw new ArgumentException($"Ast '{ast.Extent.Text}' cannot be cast to boolean type");
            }

            if (TryConvertNumber(typeof(TTarget), ast, out conversionResult))
            {
                return (TTarget)conversionResult;
            }

            // TODO:
            // - arrays
            // - dictionaries
            // - objects
            if (TryConvertDictionary(typeof(TTarget), ast, out conversionResult))
            {
                return (TTarget)conversionResult;
            }

            if (TryConvertEnumerable(typeof(TTarget), ast, out conversionResult))
            {
                return (TTarget)conversionResult;
            }

            return ConvertObject(typeof(TTarget), ast);
        }

        private bool TryConvertDictionary(Type target, Ast ast, out object result)
        {
            bool isGeneric = ImplementsGenericInterface(
                target,
                typeof(IReadOnlyDictionary<,>),
                out IReadOnlyList<Type> genericParameters);

            if (!isGeneric
                && !typeof(IDictionary).IsAssignableFrom(target))
            {
                result = null;
                return false;
            }

            if (!(ast is HashtableAst hashtableAst))
            {
                throw CreateConversionMismatchException(ast, target);
            }

            if (!isGeneric)
            {
                var dict = new Dictionary<object, object>();

                foreach (Tuple<ExpressionAst, StatementAst> entry in hashtableAst.KeyValuePairs)
                {
                    object key = _psdDataParser.ConvertAstValue(entry.Item1);
                    object value = _psdDataParser.ConvertAstValue(GetExpressionFromStatementAst(entry.Item2));
                    dict[key] = value;
                }

                result = dict;
                return true;
            }
        }

        private bool TryConvertVariableExpression(Type target, Ast ast, out object result)
        {
            if (!(ast is VariableExpressionAst variableExpressionAst))
            {
                result = null;
                return false;
            }

            string variableName = variableExpressionAst.VariablePath.UserPath;

            if (variableName.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                if (target.IsValueType)
                {
                    throw new ArgumentException($"Cannot convert value type '{target.FullName}' to null");
                }

                result = null;
                return true;
            }

            if (target == typeof(bool))
            {
                if (variableName.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                    return true;
                }

                if (variableName.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    result = false;
                    return true;
                }

                throw new ArgumentException($"Cannot convert '{ast}' to boolean type");
            }

            throw new ArgumentException($"Cannot convert non-constant variable '{ast}' to value");
        }

        private string ConvertString(Ast ast)
        {
            if (ast is StringConstantExpressionAst stringConstantExpression)
            {
                return stringConstantExpression.Value;
            }

            throw CreateConversionMismatchException(ast, typeof(string));
        }

        private bool TryConvertNumber(Type target, Ast ast, out object result)
        {
            if (IsNumericType(target))
            {
                if (!(ast is ConstantExpressionAst constantExpression)
                    || !IsNumericType(constantExpression.StaticType))
                {
                    throw CreateConversionMismatchException(ast, target);
                }

                result = System.Convert.ChangeType(constantExpression.Value, target);
                return true;
            }

            result = null;
            return false;
        }

        private DateTime ConvertDateTime(Ast ast)
        {
            if (ast is StringConstantExpressionAst stringConstantExpressionAst
                && DateTime.TryParse(stringConstantExpressionAst.Value, out DateTime dateTime))
            {
                return dateTime;
            }

            throw CreateConversionMismatchException(ast, typeof(DateTime));
        }

        private static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.Byte:
                case TypeCode.SByte:
                    return true;
            }

            return false;
        }

        private ExpressionAst GetExpressionFromStatementAst(StatementAst stmtAst)
        {
            return (stmtAst as PipelineAst)?.GetPureExpression()
                ?? throw new ArgumentException($"'{stmtAst}' must be a pure pipeline AST to convert a value");
        }

        private static Exception CreateConversionMismatchException(Ast ast, Type type)
        {
            return new ArgumentException($"Unable to convert ast '{ast.Extent.Text}' of type '{ast.GetType().FullName}' to type '{type.FullName}'");
        }

        private static bool ImplementsGenericInterface(
            Type target,
            Type genericInterface,
            out IReadOnlyList<Type> genericParameters)
        {
            foreach (Type implementedInterface in target.GetInterfaces())
            {
                if (!implementedInterface.IsGenericType)
                {
                    continue;
                }

                if (implementedInterface.GetGenericTypeDefinition() == genericInterface)
                {
                    genericParameters = implementedInterface.GetGenericArguments();
                    return true;
                }
            }

            genericParameters = null;
            return false;
        }
    }
}
