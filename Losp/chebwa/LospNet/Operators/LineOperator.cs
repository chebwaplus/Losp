// SPDX-License-Identifier: MIT

using chebwa.LospNet;

namespace chebwa.Losp.Operators
{
	public class LineOperator : IScriptOperator
	{
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			return ValueResult.SingleOrNone(Environment.NewLine);
		}
	}
}
