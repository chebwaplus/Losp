namespace chebwa.LospNet.Operators
{
	public class StrToIntOperator() : IScriptOperator
	{
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (children.Unkeyed.Count != 1)
			{
				return ErrorResultHelper.OneArgument(op, exactly: true);
			}

			if (!children.TryIndexOf(0, out string? str) || str == null)
			{
				return new ErrorResult(op, "str-to-int operator: first argument must be a non-null string");
			}

			if (!int.TryParse(str, out var val))
			{
				return new ErrorResult(op, "str-to-int operator: could not parse string as int: " + str);
			}

			return ValueResult.SingleOrNone(val);
		}
	}
}
