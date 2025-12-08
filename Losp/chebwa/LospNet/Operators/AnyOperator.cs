// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	/// <summary>
	/// <code>
	/// // default: only `true` counts
	/// (ANY false false false true false)
	/// 
	/// // coersion option: any truthy value counts
	/// (ANY {~} 0 0 0 1 0)
	/// </code>
	/// <para>
	/// Determines if any one of the (unkeyed) arguments are <c>true</c>. By default, the
	/// determination is exact: only the value <see langword="true"/> is sufficient.
	/// With the <c>{~}</c> option, any truthy value (see
	/// <see cref="TruthinessOperator.GetTrueLike(LospValue)"/>) is sufficient.
	/// </para>
	/// </summary>
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
