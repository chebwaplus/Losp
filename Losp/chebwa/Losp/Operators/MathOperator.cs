// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	public class MathOperator : IScriptOperator
	{
		public enum Op
		{
			Add, Subtract, Multiply, Divide, Modulo, Exponent
		}

		public static readonly MathOperator Instance = new();
		public EvalResult Run(IScriptContext context, LospOperatorNode @operator, LospChildResultDataCollection children)
		{
			var op = GetOp(@operator.NodeId);

			/*
			 * we'll use ints as long as we can get away with it, but as soon as we have to use floats,
			 * we will switch entirely.
			 */
			int? intResult = null;
			float? floatResult = null;

			foreach (var param in children)
			{
				if (param is LospInt i)
				{
					if (floatResult.HasValue)
					{
						// floats have already been used, so remain in float land
						floatResult = PerformOp(op, floatResult.Value, i.Value);
					}
					else
					{
						if (intResult.HasValue)
						{
							// floats have not been used, so remain in int land
							intResult = PerformOp(op, intResult.Value, i.Value);
						}
						else
						{
							// first value is an int
							intResult = i.Value;
						}
					}
				}
				else if (param is LospFloat f)
				{
					if (intResult.HasValue)
					{
						// ints have been used, but now we must convert to floats and prevent further ints
						floatResult = PerformOp(op, intResult.Value, f.Value);
						intResult = null;
					}
					else
					{
						if (floatResult.HasValue)
						{
							// floats have already been used
							floatResult = PerformOp(op, floatResult.Value, f.Value);
						}
						else
						{
							// first value is a float
							floatResult = f.Value;
						}
					}
				}
			}

			return ValueResult.SingleOrNone(intResult.HasValue
				? new LospInt(intResult.Value)
				: floatResult.HasValue
					? new LospFloat(floatResult.Value)
					: new LospInt(0)); //TODO: unsure what else to do here. error? success?
		}

		public static int PerformOp(Op op, int left, int right)
		{
			return op switch
			{
				Op.Add => left + right,
				Op.Subtract => left - right,
				Op.Multiply => left * right,
				Op.Divide => right == 0 ? int.MaxValue : left / right, //TODO: divide by zero result?
				Op.Modulo => left % right,
				Op.Exponent => (int)MathF.Round(MathF.Pow(left, right)),
				_ => throw new Exception(),
			};
		}

		public static float PerformOp(Op op, float left, float right)
		{
			return op switch
			{
				Op.Add => left + right,
				Op.Subtract => left - right,
				Op.Multiply => left * right,
				Op.Divide => right == 0 ? float.PositiveInfinity : left / right, //TODO: divide by zero result?
				Op.Modulo => left % right,
				Op.Exponent => MathF.Pow(left, right),
				_ => throw new Exception(),
			};
		}

		public static Op GetOp(string str)
		{
			return str switch
			{
				"+" => Op.Add,
				"-" => Op.Subtract,
				"*" => Op.Multiply,
				"/" => Op.Divide,
				"%" => Op.Modulo,
				"^" => Op.Exponent,
				_ => throw new Exception(),
			};
		}
	}
}
