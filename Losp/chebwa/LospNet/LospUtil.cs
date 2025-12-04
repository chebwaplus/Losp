// SPDX-License-Identifier: MIT

namespace chebwa.LospNet
{
	public static class LospUtil
	{
		public static List<T> FromList<T>(LospList list, Func<LospObjectLiteral, T> generator)
		{
			List<T> items = [];

			foreach (var val in list)
			{
				if (val.TryGetObjectLiteral(out var obj))
				{
					items.Add(generator(obj));
				}
			}

			return items;
		}
	}
}
