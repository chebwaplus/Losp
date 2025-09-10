// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	public class ContainerLastResultOperator() : IScriptOperator
	{
		public static readonly ContainerLastResultOperator Instance = new();
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (children.Count == 0)
			{
				return ValueResult.None();
			}

			return ValueResult.SingleOrNone(children[^1]);
		}
	}
}
