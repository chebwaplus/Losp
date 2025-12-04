// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	/// <summary>
	/// Performs the general equivalent of <c>list.Contains(item)</c>, determining if
	/// the second argument is contained in the first argument, which must be a list.
	/// </summary>
	public class InListOperator : IScriptOperator
	{
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (children.Count != 2)
			{
				return ErrorResultHelper.NArguments(op, 2, exactly: true);
			}

			if (!children.TryIndex<LospList>(0, out var list))
			{
				return new ErrorResult(op, "first argument must be a list");
			}

			var needle = children[1];

			foreach (var item in list)
			{
				if (item.BoxedValue == null)
				{
					if (needle.BoxedValue == null)
					{
						return ValueResult.SingleOrNone(true);
					}
				}
				else if (item.BoxedValue.Equals(needle.BoxedValue))
				{
					return ValueResult.SingleOrNone(true);
				}
			}

			return ValueResult.SingleOrNone(false);
		}
	}
}
