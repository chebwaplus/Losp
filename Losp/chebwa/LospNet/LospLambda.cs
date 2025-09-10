// SPDX-License-Identifier: MIT

namespace chebwa.LospNet
{
	public sealed class LospLambda
	{
		public readonly List<string> ParamNames = [];
		public required LospChildCollection Children;

		public static LospLambda FromNode(LospFunctionNode node)
		{
			var func = new LospLambda()
			{
				Children = node.Children,
			};

			foreach (var child in node.Params.Children)
			{
				if (child is LospIdentifierNode id)
				{
					func.ParamNames.Add(id.Name);
				}
			}

			return func;
		}
	}
}
