// SPDX-License-Identifier: MIT

namespace chebwa.LospNet
{
	/// <summary>
	/// A utility class that creates somewhat common <see cref="ErrorResult"/> messages.
	/// </summary>
	public static class ErrorResultHelper
	{
		/// <summary>
		/// Creates an <see cref="ErrorResult"/> for scenarios where <em>exactly</em> or
		/// <em>at least</em> one operator argument is required.
		/// </summary>
		/// <param name="node">The associated <see cref="LospNode"/>.</param>
		/// <param name="exactly">Indicates whether the operator needs exactly one
		/// (<see langword="true"/>) or at least one (<see langword="false"/>)
		/// argument.</param>
		public static ErrorResult OneArgument(LospNode node, bool exactly = true)
		{
			var prefix = exactly ? "exactly " : "at least ";

			return new ErrorResult(node, prefix + "one argument is required");
		}

		/// <summary>
		/// Creates an <see cref="ErrorResult"/> for scenarios where <em>exactly</em> or
		/// <em>at least</em> some non-zero number of operator arguments is required.
		/// </summary>
		/// <param name="node">The associated <see cref="LospNode"/>.</param>
		/// <param name="n">The number of required arguments.</param>
		/// <param name="exactly">Indicates whether the operator needs exactly
		/// (<see langword="true"/>) or at least (<see langword="false"/>)
		/// <paramref name="n"/> arguments.</param>
		public static ErrorResult NArguments(LospNode node, int n, bool exactly = true)
		{
			if (n == 1)
			{
				// someone's going to do this, and there's no way to stop them
				return OneArgument(node, exactly);
			}

			var prefix = exactly ? "exactly " : "at least ";

			return new ErrorResult(node, prefix + n + " arguments are required");
		}

		/// <summary>
		/// Creates an <see cref="ErrorResult"/> for scenarios where a
		/// <see cref="LospIdentifierNode"/> could not resolved as a variable.
		/// </summary>
		/// <param name="id">The associated <see cref="LospIdentifierNode"/>.</param>
		public static ErrorResult VarIdNotFound(LospIdentifierNode id)
		{
			return new ErrorResult(id, $"no variable named {id.Name} was found");
		}

		/// <summary>
		/// Creates an <see cref="ErrorResult"/> for scenarios where an
		/// <see cref="ISpecialOperator"/> expects an <see cref="LospSpecialOperatorNode"/>
		/// but receives only a non-special <see cref="LospOperatorNode"/>.
		/// </summary>
		/// <param name="op">The associated non-special <see cref="LospOperatorNode"/>.</param>
		public static ErrorResult NotSpecialOperator(LospOperatorNode op)
		{
			return new ErrorResult(op, $"expected a {nameof(LospSpecialOperatorNode)} when handling a special operator");
		}

		/// <summary>
		/// Creates an <see cref="ErrorResult"/> for scenarios where an argument to
		/// a <see cref="LospOperatorNode"/> is not the expected type. The
		/// <paramref name="expectedType"/> can be a single type ("int") or can be written
		/// as a set of types ("int, float, or bool").
		/// </summary>
		/// <param name="op">The associated <see cref="LospOperatorNode"/>.</param>
		/// <param name="index">The index of the errant argument.</param>
		/// <param name="expectedType">The expected type (or types) as a string descriptor.</param>
		/// <param name="foundValue">The value found with the expected type.</param>
		public static ErrorResult ArgNIsNotType(LospOperatorNode op, int index, string expectedType, LospValue foundValue)
		{
			var suffix = foundValue == null
				? "instead, no value found"
				: foundValue is LospNull
					? "instead, found value of type null (LospNull)"
					: "instead, found value of type " + foundValue.BoxedValue!.GetType().Name;

			return new ErrorResult(op, $"expected an argument of type {expectedType} at index {index}; {suffix}");
		}
	}
}
