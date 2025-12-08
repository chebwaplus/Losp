// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	/// <summary>
	/// <code>
	/// // determines if the value is false
	/// (0 true)
	/// 
	/// // determines if the value is false-like
	/// (~0 1)
	/// 
	/// // determines if the value is true
	/// (1 true)
	/// 
	/// // determines if the value is true-like
	/// (~1 1)
	/// </code>
	/// <para>
	/// The important evaluation methods are <see cref="GetTrue(LospValue)"/> and
	/// <see cref="GetTrueLike(LospValue)"/>.
	/// </para>
	/// </summary>
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

		/// <summary>
		/// Determines if the value of a <see cref="LospValue"/> is true. This includes
		/// the exact value <see langword="true"/> as well as a list where all items
		/// in the list are continued true (recursively). Any other value returns
		/// <see langword="false"/>.
		/// </summary>
		/// <param name="value">The <see cref="LospValue"/> to evaluate.</param>
		public static bool GetTrue(LospValue value)
		{
			return value switch
			{
				LospBool b => b.Value,
				LospList list => list.Value != null && list.Value.All(GetTrue),
				_ => false,
			};
		}

		/// <summary>
		/// Determines if the value of a <see cref="LospValue"/> is "truthy". This includes:
		/// <list type="bullet">
		/// <item><c>true</c></item>
		/// <item>any int or float that is not 0</item>
		/// <item>any non-empty string</item>
		/// <item>a list where all items in the list are true-like (recursively)</item>
		/// </list>
		/// Any of the above return <see langword="true"/>; otherwise <see langword="false"/>
		/// is returned.
		/// </summary>
		/// <param name="value">The <see cref="LospValue"/> to evaluate.</param>
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
