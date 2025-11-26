// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	/// <summary>
	/// <para>
	/// Takes two script objects and merges them into a new script object without
	/// changing the source objects. The keys are applied first-to-last, so if
	/// multiple objects define the same key, the key from the last-most object
	/// will be the one reflected in the final script object.
	/// </para>
	/// <para>
	/// Values (specifically, their <see cref="LospValue"/> wrappers) are copied by
	/// reference.
	/// </para>
	/// </summary>
	public class MergeOperator : IScriptOperator
	{
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			//TODO: allow an arbitrary number of objects
			//TODO: allow an option to change copy-by-ref behavior

			if (children.Count != 2)
			{
				return ErrorResultHelper.NArguments(op, 2, exactly: true);
			}

			if (!children.TryIndexOfNonNull<IScriptObject>(0, out var obj1)
				|| !children.TryIndexOfNonNull<IScriptObject>(1, out var obj2))
			{
				return new ErrorResult(op, "all arguments must be script objects");
			}

			var merged = new LospObjectLiteral();

			foreach (var key in obj1.Keys)
			{
				merged.Set(key, obj1.Get(key)!);
			}

			foreach (var key in obj2.Keys)
			{
				merged.Set(key, obj2.Get(key)!);
			}

			return ValueResult.SingleOrNone(new LospScriptable(merged));
		}
	}
}
