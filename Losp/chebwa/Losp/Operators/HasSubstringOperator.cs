// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	public class HasSubstringOperator : IScriptOperator
	{
		public const string StartsWithOpName = "STARTS";
		public const string EndsWithOpName = "ENDS";
		public const string ContainsOpName = "CONTAINS";
		public const string CaseKeyName = "case";
		public const string IgnoreCaseKeyName = "i";

		public static readonly HasSubstringOperator Instance = new();

		public EvalResult Run(IScriptContext context, LospOperatorNode @operator, LospChildResultDataCollection children)
		{
			var op = @operator.NodeId;

			StringComparison compare = StringComparison.Ordinal;

			if (children.Unkeyed.Count != 2)
			{
				return ErrorResultHelper.NArguments(@operator, 2, exactly: true);
			}

			if (!children.Unkeyed.TryIndexOfNonNull<string>(0, out var haystack))
			{
				return new ErrorResult(@operator, "substring operator: arguments must be non-null strings");
			}
			if (!children.Unkeyed.TryIndexOfNonNull<string>(1, out var needle))
			{
				return new ErrorResult(@operator, "substring operator: arguments must be non-null strings");
			}

			if (children.TryKey(CaseKeyName, out var comp) && comp.TryGetValue<bool>(out var useCase))
			{
				compare = useCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			}
			else if (children.TryKey(IgnoreCaseKeyName, out var ign) && ign.TryGetValue<bool>(out var ignoreCase))
			{
				compare = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			}

			var result = false;

			if (op == StartsWithOpName)
			{
				result = haystack.StartsWith(needle, compare);
			}
			else if (op == EndsWithOpName)
			{
				result = haystack.EndsWith(needle, compare);
			}
			else if (op == ContainsOpName)
			{
				result = haystack.Contains(needle, compare);
			}

			return ValueResult.SingleOrNone(result);
		}
	}
}
