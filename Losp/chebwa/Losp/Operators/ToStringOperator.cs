namespace chebwa.LospNet.Operators
{
	public class ToStringOperator() : IScriptOperator
	{
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			return ValueResult.MultipleOrNone(StringifyChildren(children));
		}

		private static IEnumerable<LospString> StringifyChildren(LospChildResultDataCollection children)
		{
			foreach (var child in children)
			{
				if (child is LospNull)
				{
					yield return new LospString("null");
				}

				yield return new LospString(child.BoxedValue!.ToString()!);
			}
		}
	}
}
