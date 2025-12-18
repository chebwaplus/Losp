// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	/// <summary>
	/// <code>
	/// (STR 1)
	/// (TO-STR false true)
	/// </code>
	/// Calls <see cref="object.ToString"/> on each argument and emits the results.
	/// The string value of a <see cref="LospNull"/> is <c>"null"</c>. All aliases
	/// for the <see cref="ToStringOperator"/> take zero or more arguments.
	/// </summary>
	public class ToStringOperator() : IScriptOperator
	{
		public static readonly ToStringOperator Instance = new();

		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			return ValueResult.MultipleOrNone(StringifyChildren(children));
		}

		private static IEnumerable<LospString> StringifyChildren(LospChildResultDataCollection children)
		{
			foreach (var child in children)
			{
				if (child is LospNull)
				{
					yield return new LospString("null");
				}

				yield return new LospString(child.BoxedValue!.ToString()!);
			}
		}
	}
}
