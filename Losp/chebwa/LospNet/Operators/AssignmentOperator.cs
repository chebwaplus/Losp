// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	//TODO: quiet mode? (i.e. don't emit new value)
	public class AssignmentOperator : ISpecialOperator
	{
		public LospSpecialOperatorNode Prepare(LospOperatorNode op)
		{
			if (op.Children.Count != 2)
			{
				throw new Exception();
			}
			if (op.Children[0] is not LospIdentifierNode)
			{
				throw new Exception();
			}

			var assignment = LospSpecialOperatorNode.FromOperator(op);

			assignment.SpecialOperatorChildren.Add(op.Children[0]);
			assignment.Children.Add(op.Children[1]);

			return assignment;
		}

		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (op is not LospSpecialOperatorNode sp)
			{
				return ErrorResultHelper.NotSpecialOperator(op);
			}

			if (!sp.SpecialOperatorChildren.TryIndex<LospIdentifierNode>(0, out var id))
			{
				return new ErrorResult(op, "assignment operator: expected an identifier");
			}

			context.SetVar(id.Name, children[0]);

			return ValueResult.SingleOrNone(children[0]);
		}
	}
}
