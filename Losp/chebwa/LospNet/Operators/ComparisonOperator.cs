// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	public class ComparisonOperator : IScriptOperator
	{
		public static readonly ComparisonOperator Instance = new();

		public enum Op
		{
			Equal, NotEqual, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual,
		}

		public EvalResult Run(IScriptContext context, LospOperatorNode @operator, LospChildResultDataCollection children)
		{
			if (children.Unkeyed.Count < 2)
			{
				return ErrorResultHelper.NArguments(@operator, 2);
			}

			var op = GetOp(@operator.NodeId);

			var left = children.Unkeyed[0];
			var right = children.Unkeyed[1];

			if (left.MatchesType(right))
			{
				if (left is LospBool b)
				{
					return ValueResult.SingleOrNone(PerformOp(op, b.Value, (bool)right.BoxedValue!));
				}
				else if (left.BoxedValue is IComparable leftComp)
				{
					return ValueResult.SingleOrNone(PerformOp(op, leftComp, (IComparable?)right.BoxedValue));
				}
			}
			else if (left is LospInt i1 && right is LospFloat f1)
			{
				return ValueResult.SingleOrNone(PerformOp(op, i1.Value, f1.Value));
			}
			else if (left is LospFloat f2 && right is LospInt i2)
			{
				return ValueResult.SingleOrNone(PerformOp(op, f2.Value, i2.Value));
			}

			return new ErrorResult(@operator, $"comparison operator: unsupported comparison ({op}) between {left} and {right}");
		}

		public static bool PerformOp(Op op, bool left, bool right)
		{
			return op switch
			{
				Op.Equal => left == right,
				Op.NotEqual => left != right,
				_ => throw new Exception(),
			};
		}

		public static bool PerformOp(Op op, IComparable? left, IComparable? right)
		{
			if (left == null)
			{
				return op switch
				{
					Op.Equal => right == null,
					Op.NotEqual => right != null,
					_ => throw new Exception(),
				};
			}
			else if (right == null)
			{
				return op switch
				{
					Op.Equal => false,
					Op.NotEqual => true,
					_ => throw new Exception(),
				};
			}

			return op switch
			{
				Op.Equal => left.CompareTo(right) == 0,
				Op.NotEqual => left.CompareTo(right) != 0,
				Op.LessThan => left.CompareTo(right) < 0,
				Op.LessThanOrEqual => left.CompareTo(right) <= 0,
				Op.GreaterThan => left.CompareTo(right) > 0,
				Op.GreaterThanOrEqual => left.CompareTo(right) >= 0,
				_ => throw new Exception(),
			};
		}

		public static Op GetOp(string str)
		{
			return str switch
			{
				"==" => Op.Equal,
				"!=" => Op.NotEqual,
				"<" => Op.LessThan,
				"<=" => Op.LessThanOrEqual,
				">" => Op.GreaterThan,
				">=" => Op.GreaterThanOrEqual,
				_ => throw new Exception(),
			};
		}
	}
}
