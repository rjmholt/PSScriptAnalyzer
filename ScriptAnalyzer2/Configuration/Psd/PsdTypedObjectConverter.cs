using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
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
        private readonly IReadOnlyList<PsdTypeConverter> _converters;

        public PsdTypedObjectConverter(IReadOnlyList<PsdTypeConverter> converters)
        {
            _converters = converters;
        }

        public PsdTypedObjectConverter() : this(converters: null)
        {
        }

        public TTarget Convert<TTarget>(Ast ast)
        {
            object conversionResult;

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
        }

        private bool TryConvertVariableExpression(Type target, Ast ast, out object value)
        {
            if (!(ast is VariableExpressionAst variableExpressionAst))
            {
                value = null;
                return false;
            }

            string variableName = variableExpressionAst.VariablePath.UserPath;

            if (variableName.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                if (target.IsValueType)
                {
                    throw new ArgumentException($"Cannot convert value type '{target.FullName}' to null");
                }

                value = null;
                return true;
            }

            if (target == typeof(bool))
            {
                if (variableName.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                    return true;
                }

                if (variableName.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
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

            throw CreateBadAstException(ast, typeof(string).FullName);
        }

        private bool TryConvertNumber(Type target, Ast ast, out object value)
        {
            if (IsNumericType(target))
            {
                if (!(ast is ConstantExpressionAst constantExpression)
                    || !IsNumericType(constantExpression.StaticType))
                {
                    throw CreateBadAstException(ast, target.FullName);
                }

                value = System.Convert.ChangeType(constantExpression.Value, target);
                return true;
            }

            value = null;
            return false;
        }

        private DateTime ConvertDateTime(Ast ast)
        {
            if (ast is StringConstantExpressionAst stringConstantExpressionAst
                && DateTime.TryParse(stringConstantExpressionAst.Value, out DateTime dateTime))
            {
                return dateTime;
            }

            throw CreateBadAstException(ast, typeof(DateTime).FullName);
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

        private static Exception CreateBadAstException(Ast ast, string expectedType)
        {
            return new ArgumentException($"Unable to convert ast '{ast.Extent.Text}' of type '{ast.GetType().FullName}' to type '{expectedType}'");
        }
    }
}
