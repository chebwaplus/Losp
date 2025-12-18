// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	/// <summary>
	/// <code>
	/// (PI)
	/// </code>
	/// Returns <see cref="MathF.PI"/>.
	/// </summary>
	public class PiOperator() : IScriptOperator
	{
		public readonly static LospFloat Pi = new(MathF.PI);

		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			return ValueResult.SingleOrNone(Pi);
		}
	}
}
