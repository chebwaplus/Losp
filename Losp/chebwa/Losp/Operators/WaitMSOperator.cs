namespace chebwa.LospNet.Operators
{
	public class WaitMSOperator : ISpecialOperator
	{
		public LospSpecialOperatorNode Prepare(LospOperatorNode node)
		{
			if (node.Children.Count != 2)
			{
				throw new Exception("wait operator: exactly two arguments are required");
			}

			var sp = LospSpecialOperatorNode.FromOperator(node);

			sp.Children.Add(node.Children[0]);
			sp.SpecialOperatorChildren.Add(node.Children[1]);

			return sp;
		}

		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (op is not LospSpecialOperatorNode sp)
			{
				return ErrorResultHelper.NotSpecialOperator(op);
			}

			if (children.Count != 1)
			{
				return ErrorResultHelper.OneArgument(op, exactly: true);
			}

			if (!children.TryIndexOf(0, out int ms) || ms < 0)
			{
				return new ErrorResult(op, "wait operator: first argument must be a positive integer");
			}

			static EvalResult OnChildResults(LospChildResultDataCollection results)
			{
				return ValueResult.MultipleOrNone(results);
			}

			if (ms == 0)
			{
				return new PushResult([sp.SpecialOperatorChildren[0]], OnChildResults);
			}

			var proxy = new AsyncProxy();

			_ = Task.Delay(ms).ContinueWith(
				_ => proxy.Complete(new PushResult([sp.SpecialOperatorChildren[0]], OnChildResults)),
				TaskScheduler.FromCurrentSynchronizationContext()
			);

			return new AsyncResult(proxy);
		}
	}
}
