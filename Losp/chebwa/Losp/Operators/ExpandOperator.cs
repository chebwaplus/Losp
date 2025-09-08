namespace chebwa.LospNet.Operators
{
	/// <summary>
	/// Expands and flattens any list in the child results and emits each item as part
	/// of the operator's results. For example, a collection of children results
	/// <code>0, 1, [10, 20, 30], [12, 24], 2, 3</code> would be emitted as a flat
	/// list <code>0, 1, 10, 20, 30, 12, 24, 2, 3</code>. Only top-level lists are
	/// expanded.
	/// </summary>
	public class ExpandOperator : IScriptOperator
	{
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			return ValueResult.MultipleOrNone(GetValues(children));
		}

		private static IEnumerable<LospValue> GetValues(LospChildResultDataCollection children)
		{
			foreach (var child in children)
			{
				if (child is LospList list)
				{
					foreach (var val in list.Value!)
					{
						yield return val;
					}
				}
				else
				{
					yield return child;
				}
			}
		}
	}
}
