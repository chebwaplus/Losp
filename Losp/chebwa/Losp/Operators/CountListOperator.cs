namespace chebwa.LospNet.Operators
{
	public class CountListOperator() : IScriptOperator
	{
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (children.Count == 0)
			{
				return ErrorResultHelper.OneArgument(op);
			}

			if (children[0] is LospList list)
			{
				return ValueResult.SingleOrNone(list.Value!.Count());
			}
			else if (children[0] is LospScriptable obj)
			{
				return ValueResult.SingleOrNone(obj.Value!.Keys.Count());
			}

			return new ErrorResult(op, $"count operator: argument must be a non-null list or {nameof(IScriptObject)}");
		}
	}
}
