// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	public class AnyOperator() : IScriptOperator
	{
		public static readonly AnyOperator Instance = new();

		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (children.Unkeyed.Count == 0)
			{
				return ErrorResultHelper.OneArgument(op, exactly: false);
			}

			var coerce = false;

			if (children.TryKey("~", out var coerceVal))
			{
				if (!coerceVal.TryGet(out coerce))
				{
					coerce = false;
				}
			}

			foreach (var child in children.UnkeyedValues)
			{
				var val = coerce ? TruthinessOperator.GetTrueLike(child) : TruthinessOperator.GetTrue(child);
				if (val)
				{
					return ValueResult.SingleOrNone(true);
				}
			}

			return ValueResult.SingleOrNone(false);
		}
	}
}
