// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chebwa.LospNet.Operators
{
	internal class FilterOperator : ISpecialOperator
	{
		public LospSpecialOperatorNode Prepare(LospOperatorNode node)
		{
			var sp = LospSpecialOperatorNode.FromOperator(node);

			var filter = new LospOperatorNode()
			{
				IdNode = node.Children[0] as LospIdentifierNode,
			};
			// create and add the variable symbol before adding the script-defined children
			filter.Children.Add(new LospIdentifierNode()
			{
				SourceToken = LospToken.SymbolFromString("value"),
			});
			// and now, the script-defined children
			foreach (var child in node.Children.Skip(1))
			{
				filter.Children.Add(child);
			}

			sp.Children.Add(filter);

			return sp;
		}

		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			return ValueResult.SingleOrNone(children[0]);
		}
	}

	internal class FilterRunnerOperator : ISpecialOperator
	{
		public LospSpecialOperatorNode Prepare(LospOperatorNode node)
		{
			var sp = LospSpecialOperatorNode.FromOperator(node);

			foreach (var child in node.Children)
			{
				sp.SpecialOperatorChildren.Add(child);
			}

			return sp;
		}

		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (op is not LospSpecialOperatorNode sp)
			{
				return ErrorResultHelper.NotSpecialOperator(op);
			}

			var cur = 0;

			EvalResult onDoComplete(LospChildResultDataCollection results)
			{
				if (results.TryIndexOf(0, out bool val))
				{
					if (!val) return ValueResult.SingleOrNone(false);
				}

				cur++;
				if (cur < sp.SpecialOperatorChildren.Count)
				{
					return new PushResult([sp.SpecialOperatorChildren[cur]], onDoComplete);
				}

				return ValueResult.SingleOrNone(true);
			}

			return new PushResult([sp.SpecialOperatorChildren[0]], onDoComplete);
		}
	}
}
