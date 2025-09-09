// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	public class ForOperator : ISpecialOperator
	{
		public LospSpecialOperatorNode Prepare(LospOperatorNode node)
		{
			var cond = FindConditionNode(node.Children);
			if (cond == null)
			{
				throw new Exception();
			}
			if (!node.Children.TryKey("do", out var kv))
			{
				throw new Exception();
			}

			var sp = LospSpecialOperatorNode.FromOperator(node);

			sp.SpecialOperatorChildren.Add(cond);
			sp.SpecialOperatorChildren.Add(kv);

			return sp;
		}

		public EvalResult Run(IScriptContext runner, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (op is not LospSpecialOperatorNode sp)
			{
				return ErrorResultHelper.NotSpecialOperator(op);
			}

			EvalResult onCondComplete(LospChildResultDataCollection results)
			{
				if (results[0].TryGetValue<bool>(out var res) && res)
				{
					return new PushResult([sp.SpecialOperatorChildren[1]], onDoComplete);
				}
				else
				{
					return ValueResult.None();
				}
			}

			EvalResult onDoComplete(LospChildResultDataCollection results)
			{
				return new PushResult([sp.SpecialOperatorChildren[0]], onCondComplete);
			}

			return new PushResult([sp.SpecialOperatorChildren[0]], onCondComplete);
		}

		public static LospOperatorNode? FindConditionNode(LospChildCollection children)
		{
			foreach (var node in children)
			{
				if (node is LospOperatorNode op && op.NodeId == "?")
				{
					return op;
				}
			}

			return null;
		}
	}
}
