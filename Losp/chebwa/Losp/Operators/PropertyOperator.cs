// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	public class PropertyOperator : IScriptOperator
	{
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (children.Unkeyed.Count != 2)
			{
				return ErrorResultHelper.NArguments(op, 2, exactly: true);
			}

			if (!children.Unkeyed.TryIndexOfNonNull<IScriptObject>(0, out var obj))
			{
				return new ErrorResult(op, $"property operator: first argument must be a non-null {nameof(IScriptObject)}");
			}
			if (!children.Unkeyed.TryIndexOfNonNull<string>(1, out var id))
			{
				return new ErrorResult(op, "property operator: second argument must be a non-null string");
			}

			//TODO: allow for more than one property (to inspect nested objects/properties)
			// e.g. `(. obj "top-prop" "inner-prop" "deepest-prop")`, which is equivalent to
			// `(. (. (. obj "top-prop") "inner-prop") "deepest-prop")`

			//TODO: allow lists/indices in additional to object/keys?

			if (obj.TryKey(id, out var value))
			{
				return ValueResult.SingleOrNone(value);
			}
			else
			{
				return new ErrorResult(op, "property operator: property not found");
			}
		}
	}
}
