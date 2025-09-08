namespace chebwa.LospNet.Operators
{
	public class TruthinessOperator : IScriptOperator
	{
		public static readonly TruthinessOperator Instance = new();

		public enum Op
		{
			True,
			TrueLike,
			False,
			FalseLike,
		}

		public EvalResult Run(IScriptContext context, LospOperatorNode @operator, LospChildResultDataCollection children)
		{
			var op = GetOp(@operator.NodeId);

			if (children.Count == 0)
			{
				return ErrorResultHelper.OneArgument(@operator, exactly: false);
			}

			foreach (var child in children)
			{
				PerformOp(op, child);
			}

			return ValueResult.SingleOrNone(false);
		}

		public static bool PerformOp(Op op, LospValue value)
		{
			return op switch
			{
				Op.True => GetTrue(value),
				Op.TrueLike => GetTrueLike(value),
				Op.False => !GetTrue(value),
				Op.FalseLike => !GetTrueLike(value),
				_ => false,
			};
		}

		public static bool GetTrue(LospValue value)
		{
			return value switch
			{
				LospBool b => b.Value,
				LospList list => list.Value != null && list.Value.All(GetTrue),
				_ => false,
			};
		}

		public static bool GetTrueLike(LospValue value)
		{
			return value switch
			{
				LospBool b => b.Value,
				LospInt i => i.Value != 0,
				LospFloat f => f.Value != 0f,
				LospString s => !string.IsNullOrEmpty(s.Value),
				LospList list => list.Value != null && list.Value.All(GetTrueLike),
				_ => value.BoxedValue != null,
			};
		}

		public static Op GetOp(string str)
		{
			return str switch
			{
				"1" => Op.True,
				"~1" => Op.TrueLike,
				"0" => Op.False,
				"~0" => Op.FalseLike,
				_ => throw new Exception(),
			};
		}
	}
}
