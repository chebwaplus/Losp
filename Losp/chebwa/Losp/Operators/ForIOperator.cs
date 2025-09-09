// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	public class ForIOperator : ISpecialOperator
	{
		public LospSpecialOperatorNode Prepare(LospOperatorNode node)
		{
			if (node.Children.Count < 2)
			{
				throw new Exception("TODO");
			}

			if (node.Children[0] is not LospObjectLiteralNode cond || !ValidateConditionNode(cond, out var nodes))
			{
				throw new Exception("TODO");
			}

			var sp = LospSpecialOperatorNode.FromOperator(node);

			sp.SpecialOperatorChildren.Add(nodes.id);
			sp.SpecialOperatorChildren.Add(node.Children[1]);
			sp.Children.Add(nodes.from);
			sp.Children.Add(nodes.before);
			if (nodes.emit != null)
			{
				sp.Children.Add(nodes.emit);
			}

			return sp;
		}

		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (op is not LospSpecialOperatorNode sp)
			{
				return ErrorResultHelper.NotSpecialOperator(op);
			}

			var idx = (LospIdentifierNode)sp.SpecialOperatorChildren[0];
			var body = sp.SpecialOperatorChildren[1];

			var fromVal = children[0];
			var beforeVal = children[1];
			LospValue? emitVal = children.Count > 2 ? children[2] : null;

			if (fromVal is not LospInt from)
			{
				return new ErrorResult(sp, "for i operator: TODO");
			}
			if (beforeVal is not LospInt before)
			{
				return new ErrorResult(sp, "for i operator: TODO");
			}
			var emit = false;
			if (emitVal is LospBool b)
			{
				emit = b.Value;
			}

			// can't emit if the loop doesn't run
			if (from.Value >= before.Value) return ValueResult.None();

			var cur = from.Value;
			context.SetVar(idx.Name, cur);

			EvalResult onDoComplete(LospChildResultDataCollection results)
			{
				cur++;
				if (cur == before)
				{
					if (emit)
					{
						//TODO: do we need to accumulate all results from each loop?
						// or just the last one result of each loop?
						return ValueResult.MultipleOrNone(results);
					}
					else
					{
						return ValueResult.None();
					}
				}
				context.SetVar(idx.Name, cur);

				return new PushResult([body], onDoComplete);
			}

			return new PushResult([body], onDoComplete);
		}

		public static bool ValidateConditionNode(LospObjectLiteralNode cond, out (LospNode from, LospNode before, LospNode? emit, LospIdentifierNode id) nodes)
		{
			if (!cond.Children.TryKey("from", out var from))
			{
				nodes = default;
				return false;
			}
			if (!cond.Children.TryKey("before", out var before))
			{
				nodes = default;
				return false;
			}
			if (!cond.Children.TryKey("idx", out var idxv) || idxv.Children[0] is not LospIdentifierNode id)
			{
				nodes = default;
				return false;
			}
			cond.Children.TryKey("emit", out var emit);

			nodes = (from, before, emit, id);
			return true;
		}
	}
}
