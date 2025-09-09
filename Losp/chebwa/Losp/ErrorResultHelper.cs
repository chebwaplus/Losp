// SPDX-License-Identifier: MIT

namespace chebwa.LospNet
{
	public static class ErrorResultHelper
	{
		public static ErrorResult OneArgument(LospNode node, bool exactly = true)
		{
			var prefix = exactly ? "exactly " : "at least ";

			return new ErrorResult(node, prefix + "one argument is required");
		}

		public static ErrorResult NArguments(LospNode node, int n, bool exactly = true)
		{
			if (n == 1)
			{
				// someone's going to do this, and there's no way to stop them.
				return OneArgument(node, exactly);
			}

			var prefix = exactly ? "exactly " : "at least ";

			return new ErrorResult(node, prefix + n + " arguments are required");
		}

		public static ErrorResult IdNotFound(LospIdentifierNode id)
		{
			return new ErrorResult(id, $"no variable named {id.Name} was found");
		}

		public static ErrorResult NotSpecialOperator(LospOperatorNode op)
		{
			return new ErrorResult(op, $"expected {nameof(LospSpecialOperatorNode)}");
		}
	}
}
