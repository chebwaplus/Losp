using System;

namespace chebwa.LospNet
{
	[Flags]
	public enum LospTokenType
	{
		/// <summary><c>(</c></summary>
		LeftParen = 1 << 1,
		/// <summary><c>)</c></summary>
		RightParen = 1 << 2,
		/// <summary><c>#(</c></summary>
		LeftInitFilter = 1 << 3,
		/// <summary><c>#(</c> (directly following a <c>)</c>)</summary>
		LeftChainFilter = 1 << 4,
		/// <summary><c>[</c></summary>
		LeftSquare = 1 << 5,
		/// <summary><c>]</c></summary>
		RightSquare = 1 << 6,
		/// <summary><c>{</c></summary>
		LeftCurly = 1 << 7,
		/// <summary><c>}</c></summary>
		RightCurly = 1 << 8,
		/// <summary><c>{{</c></summary>
		DblLeftCurly = 1 << 9,
		/// <summary><c>}}</c></summary>
		DblRightCurly = 1 << 10,
		/// <summary>A bare string not parsed any other token type.</summary>
		Symbol = 1 << 11,
		/// <summary>A quoted string.</summary>
		String = 1 << 12,
		/// <summary>A bare string that can be parsed as an int.</summary>
		Int = 1 << 13,
		/// <summary>A bare string that can be parsed as a float.</summary>
		Float = 1 << 14,
		/// <summary>A bare string that can be parsed as a bool.</summary>
		Bool = 1 << 15,
		/// <summary><c>null</c></summary>
		Null = 1 << 16,
		/// <summary><c>FN(</c></summary>
		LeftInitFunc = 1 << 17,
		/// <summary>
		/// <para>
		/// A bare string, like a <see cref="Symbol"/>, which implicitly starts a special
		/// operator without explicitly including the <see cref="LeftCurly"/> as part of
		/// the symbol. User-defined special operator symbols must start with the <c>$</c>
		/// character.
		/// </para>
		/// <para>
		/// While parsing, the leading <see cref="Symbol"/> and the trailing
		/// <see cref="LeftCurly"/> are parsed as a whole, but the generated token does
		/// not include the <see cref="LeftCurly"/> for convenience of matching
		/// the operator name.
		/// </para>
		/// </summary>
		SpecialOperatorSymbol = 1 << 18,
		Tag = 1 << 19,

		/// <summary>
		/// Both filter prefixes (which are identical anyway).
		/// </summary>
		LeftFilter = LeftInitFilter | LeftChainFilter,
		/// <summary>
		/// All literal types.
		/// </summary>
		Literal = String | Float | Int | Bool | Null,
		/// <summary>
		/// The identifier type and all literal types.
		/// </summary>
		SymbolOrLiteral = Symbol | Literal,
		/// <summary>
		/// All structure suffixes (tokens which close a structure).
		/// </summary>
		StructureEnd = RightParen | RightCurly | RightSquare | DblRightCurly,
		/// <summary>
		/// <para>
		/// Types that can appear in the left hand of a pair of tokens.
		/// </para>
		/// <para>
		/// The symbol type, all literal types, and all structure suffixes.
		/// </para>
		/// </summary>
		AnyLeftHand = SymbolOrLiteral | StructureEnd,
		/// <summary>
		/// <para>
		/// Types that can appear in the right hand of a pair of tokens.
		/// </para>
		/// <para>
		/// The symbol type, all literal types, and all structure prefixes;
		/// the latter includes special operator symbols.
		/// </para>
		/// </summary>
		AnyRightHand = SymbolOrLiteral
			| LeftParen | LeftCurly | LeftSquare | DblLeftCurly
			| SpecialOperatorSymbol,
	}

	/// <summary>
	/// A <see cref="LospToken"/> describes an atomic element of a script from the
	/// source input. The token indicates where the element starts and ends within
	/// the script as character indices.
	/// </summary>
	/// <param name="type">The token type represented by the input characters.</param>
	/// <param name="index">The index of the token within the token stream.</param>
	/// <param name="input">The full source input string.</param>
	/// <param name="tokenStart">The character index at which the token starts (inclusive).</param>
	/// <param name="tokenEnd">The character index at which the token ends (inclusive).</param>
	public class LospToken(LospTokenType type, string input, int tokenStart, int tokenEnd)
	{
		/// <summary>
		/// The token type represented by the input characters.
		/// </summary>
		public LospTokenType Type = type;
		/// <summary>
		/// The full source input string.
		/// </summary>
		public string Input = input;
		/// <summary>
		/// The character index at which the token starts (inclusive).
		/// </summary>
		public int TokenStart = tokenStart;
		/// <summary>
		/// The character index at which the token ends (inclusive).
		/// </summary>
		public int TokenEnd = tokenEnd;

		private string? _raw;
		/// <summary>
		/// Returns the substring represented by this token from the <see cref="Input"/>.
		/// </summary>
		public string Raw() => _raw ??= Input[TokenStart..(TokenEnd + 1)];
		/// <summary>
		/// Returns the substring represented by this token (as a <see cref="ReadOnlySpan{T}"/>)
		/// from the <see cref="Input"/>.
		/// </summary>
		/// <returns></returns>
		public ReadOnlySpan<char> RawSpan() => Input.AsSpan(new Range(TokenStart, TokenEnd + 1));

		public static LospToken SymbolFromString(string input)
		{
			ArgumentNullException.ThrowIfNull(input, nameof(input));
			return new(LospTokenType.Symbol, input, 0, input.Length - 1);
		}
	}
}
