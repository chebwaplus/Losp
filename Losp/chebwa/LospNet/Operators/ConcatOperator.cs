// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	public class ConcatOperator : IScriptOperator
	{
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			string? delim = null;

			if (children.TryKey("delim", out var delimKV))
			{
				if (delimKV is LospString str)
				{
					delim = str.Value;
				}
			}

			delim ??= "";

			var values = children.UnkeyedValues.Select(x => ValToString(x, delim));
			var result = string.Join(delim, values);

			return ValueResult.SingleOrNone(result);
		}

		public static string ValToString(LospValue value, string delim)
		{
			switch (value)
			{
				case LospFunc:
					return "func";
				case LospList list:
					if (list.Value == null) return string.Empty;
					return string.Join(delim, list.Value.Select(x => ValToString(x, delim)));
				default:
					return value.BoxedValue?.ToString() ?? string.Empty;
			}
		}
	}
}
