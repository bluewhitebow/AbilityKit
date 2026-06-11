#if UNITY_EDITOR
using System;

namespace AbilityKit.Ability.Editor.Utilities
{
    internal static class SourceConditionPlanMapping
    {
        public const string BoolKindConst = "Const";
        public const string BoolKindAnd = "And";
        public const string BoolKindOr = "Or";
        public const string BoolKindNot = "Not";
        public const string BoolKindCompareNumeric = "CompareNumeric";

        public const string CompareEqual = "Equal";
        public const string CompareNotEqual = "NotEqual";
        public const string CompareGreaterThan = "GreaterThan";
        public const string CompareGreaterThanOrEqual = "GreaterThanOrEqual";
        public const string CompareLessThan = "LessThan";
        public const string CompareLessThanOrEqual = "LessThanOrEqual";

        public static bool IsKind(string actual, string expected)
        {
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryGetCompareOpForConditionType(string conditionType, out string compareOp)
        {
            compareOp = null;
            if (string.IsNullOrEmpty(conditionType)) return false;

            switch (conditionType.Trim().ToLowerInvariant())
            {
                case "arg_eq":
                    compareOp = CompareEqual;
                    return true;
                case "arg_neq":
                    compareOp = CompareNotEqual;
                    return true;
                case "arg_gt":
                case "num_var_gt":
                    compareOp = CompareGreaterThan;
                    return true;
                case "arg_gte":
                    compareOp = CompareGreaterThanOrEqual;
                    return true;
                case "arg_lt":
                    compareOp = CompareLessThan;
                    return true;
                case "arg_lte":
                    compareOp = CompareLessThanOrEqual;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryGetConditionTypeForCompareOp(string compareOp, out string conditionType)
        {
            conditionType = null;
            if (string.IsNullOrEmpty(compareOp)) return false;

            switch (NormalizeCompareOp(compareOp))
            {
                case CompareEqual:
                    conditionType = "arg_eq";
                    return true;
                case CompareNotEqual:
                    conditionType = "arg_neq";
                    return true;
                case CompareGreaterThan:
                    conditionType = "arg_gt";
                    return true;
                case CompareGreaterThanOrEqual:
                    conditionType = "arg_gte";
                    return true;
                case CompareLessThan:
                    conditionType = "arg_lt";
                    return true;
                case CompareLessThanOrEqual:
                    conditionType = "arg_lte";
                    return true;
                default:
                    return false;
            }
        }

        public static string NormalizeCompareOp(string compareOp)
        {
            if (string.IsNullOrEmpty(compareOp)) return CompareEqual;

            switch (compareOp.Trim().ToLowerInvariant())
            {
                case "eq":
                case "equal":
                case "==":
                    return CompareEqual;
                case "ne":
                case "notequal":
                case "!=":
                    return CompareNotEqual;
                case "gt":
                case "greaterthan":
                case ">":
                    return CompareGreaterThan;
                case "ge":
                case "gte":
                case "greaterthanorequal":
                case ">=":
                    return CompareGreaterThanOrEqual;
                case "lt":
                case "lessthan":
                case "<":
                    return CompareLessThan;
                case "le":
                case "lte":
                case "lessthanorequal":
                case "<=":
                    return CompareLessThanOrEqual;
                default:
                    return compareOp;
            }
        }
    }
}
#endif
