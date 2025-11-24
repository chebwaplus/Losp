// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	/// <summary>
	/// Allows (recursive) object property lookup by key name. The first argument should be
	/// an object literal or some other script object. All subsequent arguments should be
	/// strings, which are the key names used to look up values. With just one key, the
	/// value of that key is looked up on the object and returned. With multiple keys,
	/// the value is assumed to be another script object, and the next key is used to look
	/// up the next value, and so on, until the last key is reached.
	/// 
	/// <code>
	/// // assumes `obj` is an object literal or some other script object
	/// 
	/// (. obj "name") // looks up the value associated with the key "name"
	/// 
	/// (. obj "school" "name") // first looks up the value associated with the key "school";
	///   // assuming that returns another object, looks up its value associated with the key "name"
	/// 
	/// (. (. obj "school") "name") // equivalent of the above example
	/// </code>
	/// </summary>
	public class PropertyOperator : IScriptOperator
	{
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (children.Unkeyed.Count < 2)
			{
				return ErrorResultHelper.NArguments(op, 2);
			}

			if (!children.Unkeyed.TryIndexOfNonNull<IScriptObject>(0, out var obj))
			{
				return new ErrorResult(op, $"property operator: first argument must be a non-null {nameof(IScriptObject)}");
			}

			var lastPropIndex = children.Unkeyed.Count - 1;

			for (var i = 1; i <= lastPropIndex; i++)
			{
				if (!children.Unkeyed.TryIndexOfNonNull<string>(i, out var id))
				{
					return new ErrorResult(op, "property operator: property name must be a non-null string");
				}

				//TODO: allow lists/indices in additional to object/keys?

				if (obj.TryKey(id, out var value))
				{
					if (i == lastPropIndex)
					{
						return ValueResult.SingleOrNone(value);
					}
					else
					{
						if (value is not LospScriptable s)
						{
							return new ErrorResult(op, "property operator: intermediate value not a script object");
						}

						obj = s.Value!;
					}
				}
				else
				{
					return new ErrorResult(op, "property operator: property not found");
				}
			}

			return new ErrorResult(op, "property operator: operator terminated without returning a value");
		}
	}
}
