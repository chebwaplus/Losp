namespace chebwa.LospNet.Operators
{
	/// <summary>
	/// Roughly the inverse of <see cref="ExpandOperator"/>, this operator collects all
	/// child results and emits them as a single list.
	/// </summary>
	public class CollapseOperator : IScriptOperator
	{
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			return ValueResult.SingleOrNone(new LospList(children));
		}
	}
}
