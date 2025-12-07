// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	/// <summary>
	/// Example:
	/// <code>IF((&lt; val 0.5) 0 1) // rounds a value </code>
	/// The first argument is a condition expression. The
	/// second argument is an expression that is evaluated and emitted if the condition
	/// is <see langword="true"/>. The second argument, which is optional, is an
	/// expression that is evaluated and emitted if the condition is any value except
	/// <see langword="true"/>.
	/// </summary>
	public class IfOperator : ISpecialOperator
	{
		public LospSpecialOperatorNode Prepare(LospOperatorNode op)
		{
			if (op.Children.Count < 2)
			{
				throw new Exception(op.NodeId + ": at least 2 arguments required (1: condition node; 2: then branch)");
			}

			var cond = op.Children[0];
			var then = op.Children[1];

			var sp = LospSpecialOperatorNode.FromOperator(op);

			sp.SpecialOperatorChildren.Add(then);
			sp.Children.Add(cond);

			// adds the third child as the "else" branch
			if (op.Children.Count > 2)
			{
				sp.SpecialOperatorChildren.Add(op.Children[2]);
			}

			return sp;
		}

		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (op is not LospSpecialOperatorNode sp)
			{
				return ErrorResultHelper.NotSpecialOperator(op);
			}

			//TODO: what would be the child results?
			if (children[0].TryGet<bool>(out var cond) && cond)
			{
				return new PushResult([sp.SpecialOperatorChildren[0]], res => ValueResult.MultipleOrNone(res));
			}
			else if (sp.SpecialOperatorChildren.Count > 1)
			{
				return new PushResult([sp.SpecialOperatorChildren[1]], res => ValueResult.MultipleOrNone(res));
			}

			return ValueResult.None();
		}
	}
}
