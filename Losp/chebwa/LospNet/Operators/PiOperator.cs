// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	public class PiOperator() : IScriptOperator
	{
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			return ValueResult.SingleOrNone(new LospFloat(MathF.PI));
		}
	}
}
