// SPDX-License-Identifier: MIT

using System;
using System.Globalization;

namespace chebwa.LospNet
{
	public class Tokenizer
	{
		// high-level process:
		//
		// as a Lisp-like, the lexing is pretty straight-forward. in most cases, we can
		// simply find a character, read forward to consume as much as appropriate, then
		// continue on to the next character. whitespace is ignored, although we must
		// be careful about filters since the semantics of ") #(" is different than
		// ")#(".

		public static List<LospToken> Tokenize(string input)
		{
			List<LospToken> tokens = [];

			var i = 0;
			while (i < input.Length)
			{
				if (char.IsWhiteSpace(input[i]))
				{
					i = ReadToNext(input, i + 1);
				}
				else if (input[i] == '(')
				{
					tokens.Add(new(LospTokenType.LeftParen, input, i, i));
					i++;
				}
				else if (input[i] == ')')
				{
					tokens.Add(new(LospTokenType.RightParen, input, i, i));
					i++;
				}
				else if (input[i] == '{')
				{
					if (MatchAt(input, i + 1, '{'))
					{
						tokens.Add(new(LospTokenType.DblLeftCurly, input, i, i + 1));
						i += 2;
					}
					else
					{
						tokens.Add(new(LospTokenType.LeftCurly, input, i, i));
						i++;
					}
				}
				else if (input[i] == '}')
				{
					if (MatchAt(input, i + 1, '}'))
					{
						tokens.Add(new(LospTokenType.DblRightCurly, input, i, i + 1));
						i += 2;
					}
					else
					{
						tokens.Add(new(LospTokenType.RightCurly, input, i, i));
						i++;
					}
				}
				else if (input[i] == '[')
				{
					tokens.Add(new(LospTokenType.LeftSquare, input, i, i));
					i++;
				}
				else if (input[i] == ']')
				{
					tokens.Add(new(LospTokenType.RightSquare, input, i, i));
					i++;
				}
				else if (input[i] == '%')
				{
					tokens.Add(new(LospTokenType.FilterChainer, input, i, i));
					i++;
				}
				else if (input[i] == '"' || input[i] == '`')
				{
					var end = ReadString(input, i);
					tokens.Add(new(LospTokenType.String, input, i, end));
					i = end + 1;
				}
				else if (input[i] == '/' && MatchAt(input, i + 1, '/'))
				{
					// comment
					i = ReadToEOL(input, i) + 1;
				}
				//else if (input[i] == '#' && MatchAt(input, i + 1, '('))
				//{
				//	//NOTE: we enforce that the input starts with '[',
				//	// so if we are matching '#' we know that i != 0, and
				//	// therefore we can look at index i - 1 without needing to
				//	// check if i == 0
				//	var filterType = input[i - 1] == ')'
				//		? LospTokenType.LeftChainFilter
				//		: LospTokenType.LeftInitFilter;
				//	tokens.Add(new(filterType, input, i, i + 1));
				//	i += 2;
				//}
				else if (input[i] == 'F' && MatchAt(input, i + 1, "N("))
				{
					tokens.Add(new(LospTokenType.LeftInitFunc, input, i, i + 1));
					i += 3;
				}
				else if (TryReadSpecialOperator(input, i, out var spOpEnd))
				{
					var type = LospTokenType.SpecialOperatorSymbol;
					if (input[i] == '#' && input[i + 1] == '(')
					{
						type = LospTokenType.LeftInitFilter;
					}

					tokens.Add(new(type, input, i, spOpEnd));
					i = spOpEnd + 2;
				}
				//else if (input[i] == '$' && TryReadSpecialOperator(input, i, out var spEnd))
				//{
				//	// note that the symbol does not include the leading '$'
				//	tokens.Add(new(LospTokenType.SpecialOperatorSymbol, input, i + 1, spEnd - 1));
				//	i = spEnd + 1;
				//}
				else
				{
					var end = ReadValue(input, i);
					if (MatchAt(input, i, "null"))
					{
						tokens.Add(new(LospTokenType.Null, input, i, end));
					}
					else if (input[i] == '#')
					{
						// removing the leading '#'
						tokens.Add(new(LospTokenType.Tag, input, i + 1, end));
					}
					else if (IsBoolean(input, i, end))
					{
						tokens.Add(new(LospTokenType.Bool, input, i, end));
					}
					else if (IsNumeric(input, i, end, out var numType))
					{
						tokens.Add(new(
							numType == NumericType.Float ? LospTokenType.Float : LospTokenType.Int,
							input, i, end
						));
					}
					else
					{
						tokens.Add(new(LospTokenType.Symbol, input, i, end));
					}
					i = end + 1;
				}
			}

			return tokens;
		}

		public static bool MatchAt(string input, int i, char match)
		{
			ArgumentNullException.ThrowIfNull(input, nameof(input));
			if (i < 0 || i >= input.Length) return false;
			return input[i] == match;
		}
		public static bool MatchAt(string input, int i, string match)
		{
			ArgumentNullException.ThrowIfNull(input, nameof(input));
			ArgumentNullException.ThrowIfNull(match, nameof(match));
			if (i < 0 || i >= input.Length) return false;
			if ((i + match.Length) > input.Length) return false;
			return MemoryExtensions.Equals(input.AsSpan(i, match.Length), match, StringComparison.Ordinal);
		}

		public static int ReadToNext(string input, int i)
		{
			ArgumentNullException.ThrowIfNull(input, nameof(input));
			//TODO: check i >= 0?
			while (i < input.Length)
			{
				if (!char.IsWhiteSpace(input[i]))
				{
					return i;
				}
				i++;
			}

			return i;
		}

		public static int ReadToEOL(string input, int i)
		{
			ArgumentNullException.ThrowIfNull(input, nameof(input));
			//TODO: check i >= 0?
			while (i < input.Length)
			{
				if (input[i] == '\n' || input[i] == '\r')
				{
					return i;
				}
				i++;
			}

			return input.Length - 1;
		}

		/// <summary>
		/// Returns the index of the last character of the string (i.e. the index of the
		/// closing quote character).
		/// </summary>
		/// <param name="input">The full input string.</param>
		/// <param name="i">The index of the first character of the string (i.e. the
		/// index of the opening quote character).</param>
		/// <exception cref="Exception">The character at <paramref name="i"/> is not a
		/// valid opening quote, or there is no matching closing quote character.</exception>
		public static int ReadString(string input, int i)
		{
			var start = i;
			char quote;

			if (input[i] == '"' || input[i] == '`')
			{
				quote = input[i];
				i++;
			}
			else
			{
				throw new Exception($"not a quoted string at char {start}");
			}

			while (i < input.Length)
			{
				if (input[i] == quote)
				{
					if (input[i - 1] != '\\')
					{
						return i;
					}
				}
				i++;
			}

			throw new Exception($"quoted string at char {start} did not terminate");
		}

		/// <summary>
		/// <para>
		/// Reads forward until certain reserved characters are found, until whitespace
		/// is found, until a comment is found, or until the end of the string is found.
		/// The index of the last valid character is returned. (If any of the above
		/// character types are found, the index before that character is returned.)
		/// </para>
		/// <para>
		/// Reserved characters are the enclosing brackets types: <c>(){}[]</c>.
		/// </para>
		/// </summary>
		/// <param name="input">The full input string.</param>
		/// <param name="i">The index to start at.</param>
		public static int ReadValue(string input, int i)
		{
			const string EndingValues = "(){}[]";

			while (i < input.Length)
			{
				if (input[i] == '/' && MatchAt(input, i + 1, '/'))
				{
					return i - 1;
				}
				else if (EndingValues.Contains(input[i]))
				{
					return i - 1;
				}
				else if (char.IsWhiteSpace(input[i]))
				{
					return i - 1;
				}

				i++;
			}

			return input.Length - 1;
		}

		private static HashSet<string>? _specialOpNames = null;
		public static HashSet<string> SpecialOpNames
		{
			get
			{
				if (_specialOpNames == null)
				{
					_specialOpNames = [];
					foreach (var lospSpOp in LospInternalContext.SpecialOperators.Keys)
					{
						_specialOpNames.Add(lospSpOp + "(");
					}
				}
				return _specialOpNames;
			}
		}

		public static bool TryReadSpecialOperator(string input, int start, out int end)
		{
			var i = ReadValue(input, start);

			// the character immediately following the symbol must be a left paren
			if (!MatchAt(input, i + 1, '('))
			{
				end = 0;
				return false;
			}

			if (input[start] == '$')
			{
				// if i == start, and the symbol is only '$', the symbol is invalid
				//  as a special operator
				if (i == start)
				{
					end = 0;
					return false;
				}

				// any other user-defined ("$*") special operator symbol is valid
				end = i;
				return true;
			}
			else
			{
				// if the symbol doesn't start with a $, it must be a Losp official
				//  operator
				if (LospInternalContext.SpecialOperators.ContainsKey(input[start..(i + 1)]))
				{
					end = i;
					return true;
				}
				else
				{
					end = 0;
					return false;
				}
			}
		}

		public enum NumericType
		{
			None,
			Float,
			Int,
		}

		public static bool IsBoolean(string input, int start, int end)
		{
			ArgumentNullException.ThrowIfNull(input, nameof(input));
			if (start < 0 || start >= input.Length)
			{
				throw new IndexOutOfRangeException($"{nameof(start)} ({start}) is out of range of {nameof(input)} ({input.Length})");
			}
			if (end >= input.Length)
			{
				throw new IndexOutOfRangeException($"{nameof(end)} ({end}) is out of range of {nameof(input)} ({input.Length})");
			}
			if (end < start)
			{
				throw new ArgumentException($"{nameof(start)} ({start}) is larger than {nameof(end)} ({end})");
			}

			var sp = input.AsSpan(new Range(start, end + 1)).ToString();

			return MemoryExtensions.Equals(sp, "true", StringComparison.Ordinal)
				|| MemoryExtensions.Equals(sp, "false", StringComparison.Ordinal);
		}

		/// <summary>
		/// Determines if the substring from indices <paramref name="start"/> to
		/// <paramref name="end"/> can be parsed as an int or a float. If the former,
		/// <paramref name="type"/> is set to <see cref="NumericType.Int"/> and
		/// <see langword="true"/> is returned. If the latter, <paramref name="type"/>
		/// is set to <see cref="NumericType.Float"/> and <see langword="true"/> is returned.
		/// Otherwise, <see langword="false"/> is returned.
		/// </summary>
		/// <param name="input">The full input string.</param>
		/// <param name="start">The start index of the substring to check.</param>
		/// <param name="end">The end index of the substring to check.</param>
		/// <param name="type">The numeric type to which the string can be parsed,
		/// if any.</param>
		public static bool IsNumeric(string input, int start, int end, out NumericType type)
		{
			ArgumentNullException.ThrowIfNull(input, nameof(input));
			if (start < 0 || start >= input.Length)
			{
				throw new IndexOutOfRangeException($"{nameof(start)} ({start}) is out of range of {nameof(input)} ({input.Length})");
			}
			if (end >= input.Length)
			{
				throw new IndexOutOfRangeException($"{nameof(end)} ({end}) is out of range of {nameof(input)} ({input.Length})");
			}
			if (end < start)
			{
				throw new ArgumentException($"{nameof(start)} ({start}) is larger than {nameof(end)} ({end})");
			}

			if (input[start] != '-' && !char.IsNumber(input[start]))
			{
				type = NumericType.None;
				return false;
			}

			var sp = input.AsSpan(new Range(start, end + 1));

			if (int.TryParse(sp, out _))
			{
				type = NumericType.Int;
				return true;
			}
			if (float.TryParse(sp, CultureInfo.InvariantCulture, out _))
			{
				type = NumericType.Float;
				return true;
			}

			type = NumericType.None;
			return false;
		}
	}

	// nothing coherent here; just one thie from ChatGPT, and then a potentially better thing
	//  from https://blog.ndepend.com/alternate-lookup-for-dictionary-and-hashset-in-net-9/
	class SpanStringComparer : IEqualityComparer<string>
	{
		public bool Equals(string? x, string? y) => string.Equals(x, y);
		public int GetHashCode(string obj) => string.GetHashCode(obj.AsSpan());

		public bool Equals(string x, ReadOnlySpan<char> y) =>
			MemoryExtensions.Equals(x, y, StringComparison.Ordinal);

		public void Eq()
		{
			var dico = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
				{ "Paul", 11 },
				{ "John", 22 },
				{ "Jack", 33 }
			};

			// .NET 9 : GetAlternateLookup()
			Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> lookup = dico.GetAlternateLookup<ReadOnlySpan<char>>();

			// https://learn.microsoft.com/en-us/dotnet/api/system.memoryextensions.split?view=net-8.0
			string names = "jack ; paul;john ";
			MemoryExtensions.SpanSplitEnumerator<char> ranges = names.AsSpan().Split(';');

			foreach (Range range in ranges)
			{
				ReadOnlySpan<char> key = names.AsSpan(range).Trim();
				int val = lookup[key];
				Console.WriteLine(val);
			}
		}

		public int GetHashCode(ReadOnlySpan<char> span) =>
			string.GetHashCode(span);
	}
}
