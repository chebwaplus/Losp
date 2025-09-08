namespace chebwa.LospNet.Operators
{
	public class NotOperator : IScriptOperator
	{
		public static readonly NotOperator Instance = new();

		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (children.Unkeyed.Count == 0)
			{
				return ErrorResultHelper.OneArgument(op);
			}

			var coerce = false;

			if (op.NodeId == "~!")
			{
				coerce = true;
			}
			else if (children.TryKey("~", out var coerceVal))
			{
				if (!coerceVal.TryGetValue(out coerce))
				{
					coerce = false;
				}
			}

			if (coerce)
			{
				return ValueResult.SingleOrNone(!TruthinessOperator.GetTrueLike(children.Unkeyed[0]));
			}
			else
			{
				return ValueResult.SingleOrNone(!TruthinessOperator.GetTrue(children.Unkeyed[0]));
			}
		}
	}
}
