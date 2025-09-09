// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;

namespace chebwa.LospNet
{
	// high-level process:
	//
	// whenever the parser finds certain token types, it enters a corresponding state.
	// (example: the `[` token enters the list parsing state.)
	//
	// in a given state, only certain token type pairs should be found together.
	// these are fairly permissive, but the restrictions allow for certain sanity checks
	// to quickly rule out impossible parse scenarios.
	// the pairs (token at i - 1, token at i) are checked first as we step through each
	// token. pairs not explicitly allowed are disallowed.
	//
	// confident that the current token is legal in the current context, we can then
	// check if some logic has to be performed. typically, the logic is that if it's
	// a node start token, we create the appropriate node and push a new parser state;
	// if it's a node end token, we end the current node and pop the current parser state.
	// when we have a complete node, we add it to its parent's list of child nodes.
	//
	// a lot of node handling (esp. node creation) is shared across all/most parser
	// states, so the loop is broken up to handle state-specific cases first, then any
	// remaining common cases.

	public class ASTBuilder
	{
		/// <summary>
		/// An intermediate node that will be used to produce the final
		/// <see cref="LospFunctionNode"/>.
		/// </summary>
		private class LospFunctionProtoNode(LospToken idToken) : LospNode()
		{
			public override LospNodeType Type => LospNodeType.Function;

			public readonly LospToken IdToken = idToken;

			public override LospChildCollection Children { get; } = [];
		}
		/// <summary>
		/// A version of a ListNode that allows for all child types; used only
		/// at root level (AST root only) to allow top-level KV nodes.
		/// </summary>
		private class LospTopListNode() : LospListNode()
		{
			public override LospChildCollection Children { get; } = new(AllowedChildTypes.All);
		}

		public enum ParseState
		{
			Operator,
			SpecialOperator,
			Filter,
			ObjLiteral,
			KeyValue,
			List,
			Function,
			FunctionParams,
		}

		private record struct ParseStateEntry(ParseState State, LospNode Node, bool IsTop = false);

		public static LospNode BuildAST(List<LospToken> tokens)
		{
			static bool Peek<T>(Stack<ParseStateEntry> stack, [NotNullWhen(true)] out T? node) where T : LospNode
			{
				if (stack.Count == 0)
				{
					node = null;
					return false;
				}

				if (stack.Peek().Node is T n)
				{
					node = n;
					return true;
				}

				node = null;
				return false;
			}
			static LospOperatorNode? PeekOp(Stack<ParseStateEntry> stack)
			{
				if (Peek<LospOperatorNode>(stack, out var node)) return node;
				return null;
			}
			static LospFilterNode? PeekFilter(Stack<ParseStateEntry> stack)
			{
				if (Peek<LospFilterNode>(stack, out var node)) return node;
				return null;
			}
			static LospKeyValueNode? PeekKV(Stack<ParseStateEntry> stack)
			{
				if (Peek<LospKeyValueNode>(stack, out var node)) return node;
				return null;
			}
			static LospObjectLiteralNode? PeekOL(Stack<ParseStateEntry> stack)
			{
				if (Peek<LospObjectLiteralNode>(stack, out var node)) return node;
				return null;
			}

			Stack<ParseStateEntry> states = [];

			/*
			 * ensure the input is wrapped in a list to allow for multiple root-level
			 * nodes. if the list only has one child, we'll return that child; otherwise
			 * we'll return the list.
			 */
			tokens = [
				new LospToken(LospTokenType.LeftSquare, "[", 0, 0),
				.. tokens,
				new LospToken(LospTokenType.RightSquare, "]", 0, 0)
			];

			LospTopListNode top = new();
			states.Push(new(ParseState.List, top, true));

			var i = 1;
			while (i < tokens.Count)
			{
				//TODO: I think we guarantee that states.Count > 0, but maybe we should be sure.
				// anyway, should probably throw an error if Count == 0
				var curState = states.Count == 0 ? ParseState.List : states.Peek().State;

				var tokenPrev = tokens[i - 1].Type;
				var tokenCurr = tokens[i].Type;

				//TODO: figure out how to allow KVs at the top level
				if (!Allowed(curState, tokenPrev, tokenCurr))
				{
					var errorToken = tokens[i];
					var errorArea = GetSubstringNear(errorToken.Input, errorToken.TokenStart);

					throw new Exception($"invalid input at char {errorToken.TokenStart} (`{errorToken.Raw()}`): ...{errorArea}...\n"
						+ $"state: {curState}; previous: {tokenPrev}; current: {tokenCurr}");
				}

				/// <summary>
				/// Indicates whether state-specific handling was done.
				/// </summary>
				var stateHandled = false;

				switch (curState)
				{
					case ParseState.Operator:
					case ParseState.SpecialOperator:
						{
							if (tokenCurr == LospTokenType.RightParen)
							{
								if (curState == ParseState.SpecialOperator)
								{
									var op = PeekOp(states)!;

									if (Losp.TryGetSpecialOperator(op.NodeId, out var sp))
									{
										var state = states.Pop();
										state.Node = sp.Prepare(op);
										states.Push(state);
									}
								}

								if (TryPopAndAddToParent(states))
								{
									stateHandled = true;
								}
							}
							else if (tokenPrev == LospTokenType.LeftParen && tokenCurr == LospTokenType.Symbol)
							{
								PeekOp(states)!.IdNode = new()
								{
									SourceToken = tokens[i],
								};

								stateHandled = true;
							}
						}
						break;
					case ParseState.Filter:
						{
							if (tokenCurr == LospTokenType.RightParen)
							{
								//TODO: if filter is chained, do not add to parent
								// (we just need to pop state, I believe)
								if (TryPopAndAddToParent(states))
								{
									stateHandled = true;
								}
							}
							else if (LospTokenType.LeftFilter.HasFlag(tokenPrev) && tokenCurr == LospTokenType.Symbol)
							{
								PeekFilter(states)!.IdNode = new()
								{
									SourceToken = tokens[i],
								};

								stateHandled = true;
							}
						}
						break;
					case ParseState.List:
						{
							if (tokenCurr == LospTokenType.RightSquare)
							{
								if (TryPopAndAddToParent(states))
								{
									stateHandled = true;
								}
							}
						}
						break;
					case ParseState.KeyValue:
						{
							if (tokenCurr == LospTokenType.RightCurly)
							{
								if (TryPopAndAddToParent(states))
								{
									stateHandled = true;
								}
							}
							else if (tokenCurr == LospTokenType.Tag)
							{
								PeekKV(states)!.Tags.Add(tokens[i].Raw());

								stateHandled = true;
							}
							else if (tokenPrev == LospTokenType.LeftCurly && tokenCurr == LospTokenType.Symbol)
							{
								PeekKV(states)!.IdNode = new()
								{
									SourceToken = tokens[i],
								};

								stateHandled = true;
							}
						}
						break;
					case ParseState.ObjLiteral:
						{
							if (tokenCurr == LospTokenType.DblRightCurly)
							{
								if (TryPopAndAddToParent(states))
								{
									stateHandled = true;
								}
							}
							else if (tokenCurr == LospTokenType.Tag)
							{
								PeekOL(states)!.Tags.Add(tokens[i].Raw());

								stateHandled = true;
							}
						}
						break;
					case ParseState.Function:
						{
							if (tokenCurr == LospTokenType.RightParen)
							{
								/*
								 * we've used a FunctionProtoNode as an intermediate node
								 * to house the child nodes of the function. now we separate
								 * the parameter list from the rest and, if need be, move
								 * "the rest" under a single operator node (which will only
								 * return the last result of its children).
								 */
								var state = states.Pop();
								var funcProto = state.Node as LospFunctionProtoNode;

								if (funcProto!.Children!.Count < 2)
								{
									throw new Exception("a function definition requires at least two child nodes: one paramater list and at least one node as the body");
								}
								if (funcProto.Children[0] is not LospListNode paramList)
								{
									throw new Exception("a function definition MUST have a list as its first child node");
								}

								var func = new LospFunctionNode()
								{
									Params = paramList,
								};

								for (var c = 1; c < funcProto.Children.Count; c++)
								{
									func.Children.Add(funcProto.Children[c]);
								}

								states.Push(new(state.State, func));

								if (TryPopAndAddToParent(states))
								{
									stateHandled = true;
								}
							}
						}
						break;
					case ParseState.FunctionParams:
						{
							if (tokenCurr == LospTokenType.RightSquare)
							{
								if (TryPopAndAddToParent(states))
								{
									stateHandled = true;
								}
							}
						}
						break;
				}

				if (!stateHandled)
				{
					// handle common logic not handled by state-specific logic

					if (tokenCurr == LospTokenType.LeftParen)
					{
						var op = new LospOperatorNode();
						states.Push(new(ParseState.Operator, op));
					}
					else if (tokenCurr == LospTokenType.LeftSquare)
					{
						var list = new LospListNode();
						states.Push(new(ParseState.List, list));
					}
					else if (tokenCurr == LospTokenType.LeftCurly)
					{
						var kv = new LospKeyValueNode();
						states.Push(new(ParseState.KeyValue, kv));
					}
					else if (tokenCurr == LospTokenType.DblLeftCurly)
					{
						var obj = new LospObjectLiteralNode();
						states.Push(new(ParseState.ObjLiteral, obj));
					}
					else if (tokenCurr == LospTokenType.SpecialOperatorSymbol)
					{
						var spOp = new LospOperatorNode()
						{
							IdNode = new()
							{
								SourceToken = MapSpecialOpSourceToken(tokens[i]),
							},
						};
						states.Push(new(ParseState.SpecialOperator, spOp));
					}
					else if (tokenCurr == LospTokenType.LeftInitFunc)
					{
						var func = new LospFunctionProtoNode(tokens[i]);
						states.Push(new(ParseState.Function, func));
					}
					else if (tokenCurr == LospTokenType.LeftInitFilter)
					{
						var filter = new LospFilterNode(false);
						states.Push(new(ParseState.Filter, filter));
					}
					else if (tokenCurr == LospTokenType.LeftChainFilter)
					{
						var filter = new LospFilterNode(true);

						// before pushing a new state, we have to peek the preceding
						//  filter out of the current top state. the filter *must*
						//  be the last node in the parent's child list (otherwise
						//  we wouldn't have anything to chain the new filter to).

						//TODO: it's probably possible for a filter to be defined
						// after an operator, and therefore we have to do some extra
						// checks to ensure that didn't happen.
						// example: (OUTER (INNER)#(FILTER))
						// ...
						// okay, I guess I added the check. still need to make sure
						// it functions properly.

						if (states.TryPeek(out var peek))
						{
							var prev = peek.Node.Children!.List[^1];
							if (prev is LospFilterNode prevFilter)
							{
								//TODO: need to follow the filter chain and assign
								// to last item in current chain
								prevFilter.NextFilter = filter;
							}
							else
							{
								// if the previous node wasn't a filter, then the script
								//  author accidentally chained a filter to an operator.
								//  we have to correct by creating a new filter, since
								//  its position type is readonly.
								filter = new(false);
							}
						}

						states.Push(new(ParseState.Filter, filter));
					}
					else if (TryParseLiteralOrIDNode(tokens[i], out var node))
					{
						if (states.TryPeek(out var peek))
						{
							peek.Node.Children!.Add(node);
						}
					}
				}

				i++;
			}

			if (states.Count > 0)
			{
				throw new Exception("invalid input: opening paren/bracket count is larger than the closing paren/bracket count");
			}

			return top.Children.Count == 1 ? top.Children[0] : top;
		}

		/// <summary>
		/// Helper function for showing the source string near an error point.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		private static string GetSubstringNear(string input, int index)
		{
			if (input == null || index >= input.Length) return string.Empty;

			const int width = 10;

			var left = index - width;
			var right = index + width + 1;

			if (left < 0)
			{
				left = 0;
			}
			if (right >= input.Length)
			{
				right = input.Length;
			}

			return input[left..right];
		}

		public static bool TryParseLiteralOrIDNode(LospToken token, [NotNullWhen(true)] out LospNode? node)
		{
			if (token.Type == LospTokenType.Null)
			{
				node = new LospLiteralNode()
				{
					SourceToken = token,
					Data = new LospNull(),
				};
				return true;
			}
			else if (token.Type == LospTokenType.Int)
			{
				node = new LospLiteralNode()
				{
					SourceToken = token,
					Data = int.Parse(token.RawSpan()),
				};
				return true;
			}
			else if (token.Type == LospTokenType.Float)
			{
				node = new LospLiteralNode()
				{
					SourceToken = token,
					Data = float.Parse(token.RawSpan()),
				};
				return true;
			}
			else if (token.Type == LospTokenType.Bool)
			{
				node = new LospLiteralNode()
				{
					SourceToken = token,
					Data = MemoryExtensions.Equals(token.RawSpan(), "true", StringComparison.Ordinal),
				};
				return true;
			}
			else if (token.Type == LospTokenType.String)
			{
				node = new LospLiteralNode()
				{
					SourceToken = token,
					Data = token.RawSpan()[1..^1].ToString(),
				};
				return true;
			}
			else if (token.Type == LospTokenType.Symbol)
			{
				node = new LospIdentifierNode()
				{
					SourceToken = token,
				};
				return true;
			}

			node = null;
			return false;
		}

		private static bool TryPopAndAddToParent(Stack<ParseStateEntry> states)
		{
			// we don't anticipate this happening, since we're always in a
			//  known state when calling this method
			if (!states.TryPop(out var popped))
			{
				throw new Exception("no state to pop");
			}

			if (!states.TryPeek(out var peek))
			{
				//TODO: handle an error? (too many closing brackets)
				// this is valid at the very end, but otherwise should be an error
				return true;
			}
			peek.Node.Children?.Add(popped.Node);

			return true;
		}

		private static LospToken MapSpecialOpSourceToken(LospToken token)
		{
			var raw = token.Raw();

			if (LospInternalContext.SpecialOperators.ContainsKey(LospInternalContext.LospSpecialOperatorPrefix + raw))
			{
				var op = LospInternalContext.LospSpecialOperatorPrefix + raw;
				return new(token.Type, op, 0, op.Length - 1);
			}

			return token;
		}

		private static bool PairsBuilt = false;
		private static readonly HashSet<(LospTokenType, LospTokenType)> OperatorPairs = [];
		private static readonly HashSet<(LospTokenType, LospTokenType)> FilterPairs = [];
		private static readonly HashSet<(LospTokenType, LospTokenType)> ListPairs = [];
		private static readonly HashSet<(LospTokenType, LospTokenType)> FuncPairs = [];
		private static readonly HashSet<(LospTokenType, LospTokenType)> ParamListPairs = [];
		private static readonly HashSet<(LospTokenType, LospTokenType)> KVPairs = [];
		private static readonly HashSet<(LospTokenType, LospTokenType)> ObjectPairs = [];

		private static void BuildAllowedPairs()
		{
			// * (
			var anyOperator = (LospTokenType.AnyLeftHand, LospTokenType.LeftParen);
			// * *(
			var anySpecialOp = (LospTokenType.AnyLeftHand, LospTokenType.SpecialOperatorSymbol);
			// * #(
			var anyFilter = (LospTokenType.AnyLeftHand, LospTokenType.LeftInitFilter);
			// * FN(
			var anyFunction = (LospTokenType.AnyLeftHand, LospTokenType.LeftInitFunc);
			// )#(
			var filterChain = (LospTokenType.RightParen, LospTokenType.LeftChainFilter);
			// * [
			var anyList = (LospTokenType.AnyLeftHand, LospTokenType.LeftSquare);
			// * {
			var anyKV = (LospTokenType.AnyLeftHand, LospTokenType.LeftCurly);
			// * {{
			var anyObj = (LospTokenType.AnyLeftHand, LospTokenType.DblLeftCurly);
			// * symbol | literal
			var anySymLit = (LospTokenType.AnyLeftHand, LospTokenType.SymbolOrLiteral);

			// *( *
			OperatorPairs.Add((LospTokenType.SpecialOperatorSymbol, LospTokenType.AnyRightHand));
			// ( symbol
			OperatorPairs.Add((LospTokenType.LeftParen, LospTokenType.Symbol));
			// * (
			OperatorPairs.Add(anyOperator);
			// * *(
			OperatorPairs.Add(anySpecialOp);
			// * FN(
			OperatorPairs.Add(anyFunction);
			// * #(
			OperatorPairs.Add(anyFilter);
			// )#(
			OperatorPairs.Add(filterChain);
			// * [
			OperatorPairs.Add(anyList);
			// * {
			OperatorPairs.Add(anyKV);
			// * {{
			OperatorPairs.Add(anyObj);
			// * symbol | literal
			OperatorPairs.Add(anySymLit);
			// * )
			OperatorPairs.Add((LospTokenType.AnyLeftHand, LospTokenType.RightParen));

			// #( symbol
			FilterPairs.Add((LospTokenType.LeftFilter, LospTokenType.Symbol));
			// * (
			FilterPairs.Add(anyOperator);
			// * #(
			FilterPairs.Add(anyFilter);
			// )#(
			FilterPairs.Add(filterChain);
			// * [
			FilterPairs.Add(anyList);
			// * {
			FilterPairs.Add(anyKV);
			// * {{
			FilterPairs.Add(anyObj);
			// * symbol | literal
			FilterPairs.Add(anySymLit);
			// * )
			FilterPairs.Add((LospTokenType.AnyLeftHand, LospTokenType.RightParen));

			// { symbol
			KVPairs.Add((LospTokenType.LeftCurly, LospTokenType.Symbol));
			// symbol tag
			KVPairs.Add((LospTokenType.Symbol, LospTokenType.Tag));
			// tag tag
			KVPairs.Add((LospTokenType.Tag, LospTokenType.Tag));
			// * (
			KVPairs.Add(anyOperator);
			// * *(
			KVPairs.Add(anySpecialOp);
			// * FN(
			KVPairs.Add(anyFunction);
			// * #(
			KVPairs.Add(anyFilter);
			// )#(
			KVPairs.Add(filterChain);
			// * [
			KVPairs.Add(anyList);
			// * {
			KVPairs.Add(anyKV);
			// * {{
			KVPairs.Add(anyObj);
			// * symbol | literal
			KVPairs.Add(anySymLit);
			// * }
			KVPairs.Add((LospTokenType.AnyLeftHand, LospTokenType.RightCurly));
			// note KVs allow tags (see also object literals).
			//  they must come after the initial symbol
			//  (although as written there are ways around that restriction).
			//TODO: ensure tags can't come after non-initial symbols

			// * (
			ListPairs.Add(anyOperator);
			// * *(
			ListPairs.Add(anySpecialOp);
			// * [
			ListPairs.Add(anyList);
			// * {{
			ListPairs.Add(anyObj);
			// * symbol | literal
			ListPairs.Add(anySymLit);
			// * ]
			ListPairs.Add((LospTokenType.AnyLeftHand, LospTokenType.RightSquare));
			// [ ( | *(
			ListPairs.Add((LospTokenType.LeftSquare, LospTokenType.LeftParen | LospTokenType.SpecialOperatorSymbol));
			// [ [
			ListPairs.Add((LospTokenType.LeftSquare, LospTokenType.LeftSquare));
			// [ {{
			ListPairs.Add((LospTokenType.LeftSquare, LospTokenType.DblLeftCurly));
			// [ symbol | literal
			ListPairs.Add((LospTokenType.LeftSquare, LospTokenType.SymbolOrLiteral));
			// [ ]		empty list
			ListPairs.Add((LospTokenType.LeftSquare, LospTokenType.RightSquare));
			// note lists do not allow KVs

			// FN( [		begin param list
			FuncPairs.Add((LospTokenType.LeftInitFunc, LospTokenType.LeftSquare));
			// ] ( | *(
			FuncPairs.Add((LospTokenType.RightSquare, LospTokenType.LeftParen | LospTokenType.SpecialOperatorSymbol));
			// ) ( | *(
			FuncPairs.Add((LospTokenType.RightParen, LospTokenType.LeftParen | LospTokenType.SpecialOperatorSymbol));
			// ) )
			FuncPairs.Add((LospTokenType.RightParen, LospTokenType.RightParen));
			// note a function must start with a param list and
			//  afterward may only have operators
			//TODO: I may have to think about this a bit. in practice, if I just want to spit out a value
			// (with a bare symbol) this is inconvenient.

			// [ symbol
			ParamListPairs.Add((LospTokenType.LeftSquare, LospTokenType.Symbol));
			// symbol symbol
			ParamListPairs.Add((LospTokenType.Symbol, LospTokenType.Symbol));
			// symbol ]
			ParamListPairs.Add((LospTokenType.Symbol, LospTokenType.RightSquare));
			// [ ]		empty list
			ParamListPairs.Add((LospTokenType.LeftSquare, LospTokenType.RightSquare));
			// note param lists only allow symbols

			// {{ | tag {
			ObjectPairs.Add((LospTokenType.DblLeftCurly | LospTokenType.Tag, LospTokenType.LeftCurly));
			// {{ | tag tag
			ObjectPairs.Add((LospTokenType.DblLeftCurly | LospTokenType.Tag, LospTokenType.Tag));
			// } {
			ObjectPairs.Add((LospTokenType.RightCurly, LospTokenType.LeftCurly));
			// } | tag }}
			ObjectPairs.Add((LospTokenType.RightCurly | LospTokenType.Tag, LospTokenType.DblRightCurly));
			// {{ }}	empty object
			ObjectPairs.Add((LospTokenType.DblLeftCurly, LospTokenType.DblRightCurly));
			// note object literals only allow tags and KVs.
			// note only object literals may contains tags
			//  (although KVs can count as object literals in some cases).
			//  they must be listed first, and there may be more than one (for now?).

			PairsBuilt = true;
		}

		/// <summary>
		/// Based on the current parser <paramref name="state"/>, determines if two
		/// token types may appear together.
		/// </summary>
		/// <param name="state">The current parser state.</param>
		/// <param name="tokenA">The left-side token.</param>
		/// <param name="tokenB">The right-side token.</param>
		public static bool Allowed(ParseState state, LospTokenType tokenA, LospTokenType tokenB)
		{
			if (!PairsBuilt)
			{
				BuildAllowedPairs();
			}

			bool CheckPairs(HashSet<(LospTokenType, LospTokenType)> pairs)
			{
				foreach (var pair in pairs)
				{
					if (pair.Item1.HasFlag(tokenA) && pair.Item2.HasFlag(tokenB))
					{
						return true;
					}
				}

				return false;
			}

			return state switch
			{
				ParseState.Operator or ParseState.SpecialOperator => CheckPairs(OperatorPairs),
				ParseState.Filter => CheckPairs(FilterPairs),
				ParseState.KeyValue => CheckPairs(KVPairs),
				ParseState.List => CheckPairs(ListPairs),
				ParseState.Function => CheckPairs(FuncPairs),
				ParseState.FunctionParams => CheckPairs(ParamListPairs),
				ParseState.ObjLiteral => CheckPairs(ObjectPairs),
				_ => false,
			};
		}
	}
}
