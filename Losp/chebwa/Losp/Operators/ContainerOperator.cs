namespace chebwa.LospNet.Operators
{
	public class ContainerOperator(bool emitsResult = true) : IScriptOperator
	{
		public readonly bool EmitsResult = emitsResult;

		public static readonly ContainerOperator UnmutedInstance = new();
		public static readonly ContainerOperator MutedInstance = new(false);

		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (!EmitsResult || children.Count == 0)
			{
				return ValueResult.None();
			}

			return ValueResult.MultipleOrNone(children);
		}
	}
}
