// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{

	/// <summary>
	/// An operator that increments or decrements a value either from a variable or from
	/// the result of a child operator. Other child types (literals, lists) are not
	/// supported. If operating on a variable, the resulting value is reapplied to
	/// the variable.
	/// </summary>
	public class IncDecOperator : ISpecialOperator
	{
		public static readonly IncDecOperator Instance = new();

		public LospSpecialOperatorNode Prepare(LospOperatorNode op)
		{
			if (op.Children.Count != 1)
			{
				throw new Exception();
			}

			if (op.Children[0] is LospIdentifierNode)
			{
				var assignment = LospSpecialOperatorNode.FromOperator(op);

				assignment.SpecialOperatorChildren.Add(op.Children[0]);

				return assignment;
			}
			else if (op.Children[0] is LospOperatorNode)
			{
				var assignment = LospSpecialOperatorNode.FromOperator(op);

				assignment.Children.Add(op.Children[0]);

				return assignment;
			}

			throw new Exception();
		}

		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (op is not LospSpecialOperatorNode sp)
			{
				throw new Exception();
			}

			// identifier
			if (sp.SpecialOperatorChildren.Count > 0)
			{
				if (sp.SpecialOperatorChildren[0] is LospIdentifierNode id)
				{
					if (context.TryGetVar(id.Name, out var val))
					{
						var result = ApplyOp(sp, val);
						if (result is ValueResult vsr && vsr.Type == ResultType.SuccessEmit)
						{
							context.SetVar(id.Name, vsr.Values.First());
						}
						else if (result is AsyncResult asr)
						{
							asr.Source.OnAsyncCompleted(r =>
							{
								if (r is ValueResult avr && avr.Type == ResultType.SuccessEmit)
								{
									context.SetVar(id.NodeId, avr.Values.First());
								}
							});
						}
						return result;
					}

					return ErrorResultHelper.VarIdNotFound(id);
				}

				return new ErrorResult(op, "inc/dec operator: expected an identifier node");
			}
			// operator
			else if (children.Count > 0)
			{
				return ApplyOp(sp, children[0]);
			}
			else
			{
				return new ErrorResult(op, "inc/dec operator: expected either an identifier or a numeric value");
			}
		}

		private static EvalResult ApplyOp(LospSpecialOperatorNode sp, LospValue val)
		{
			if (val.TryGet<int>(out var intVal))
			{
				if (sp.NodeId == LospInternalContext.LospIncOpName)
				{
					intVal++;
				}
				else
				{
					intVal--;
				}

				return ValueResult.SingleOrNone(intVal);
			}
			else if (val.TryGet<float>(out var floatVal))
			{
				if (sp.NodeId == LospInternalContext.LospIncOpName)
				{
					floatVal++;
				}
				else
				{
					floatVal--;
				}

				return ValueResult.SingleOrNone(floatVal);
			}
			else
			{
				return new ErrorResult(sp, "inc/dec operator: variable not a numerical value");
			}
		}
	}
}
