// SPDX-License-Identifier: MIT

using chebwa.LospNet;
using System.Text;

namespace chebwa.Losp
{
	internal static class LospWriter
	{
		const string Indent = "  ";

		static StringBuilder WriteIndent(StringBuilder sb, int depth)
		{
			for (var i = 0; i < depth; i++)
			{
				sb.Append(Indent);
			}
			return sb;
		}

		static void WriteOperator(LospOperatorNode op, int depth, StringBuilder sb)
		{
			WriteIndent(sb, depth).AppendLine(op.NodeId + "()");

			if (op is LospSpecialOperatorNode sp)
			{
				if (sp.SpecialOperatorChildren.Count == 0)
				{
					WriteIndent(sb, depth).AppendLine(Indent + "Sp. Children: (none)");
				}
				else
				{
					WriteIndent(sb, depth).AppendLine(Indent + "Sp. Children:");
					WriteChildren(sp.SpecialOperatorChildren, depth + 1, sb);
				}
			}

			WriteChildren(op.Children, depth + 1, sb);
		}

		static void WriteKV(LospKeyValueNode kv, int depth, StringBuilder sb)
		{
			if (kv.Children.Count == 0 && kv.Tags.Count == 0)
			{
				WriteIndent(sb, depth).AppendLine("{" + kv.NodeId + "}");
			}
			else
			{
				WriteIndent(sb, depth).AppendLine("{" + kv.NodeId + ":");

				if (kv.Tags.Count > 0)
				{
					WriteIndent(sb, depth).AppendLine(Indent + "Tags: " + string.Join(", ", kv.Tags));
				}
				if (kv.Children.Count > 0)
				{
					WriteChildren(kv.Children, depth + 1, sb);
				}

				WriteIndent(sb, depth).AppendLine("}");
			}
		}

		static void WriteList(LospListNode list, int depth, StringBuilder sb)
		{
			if (list.Children.Count == 0)
			{
				WriteIndent(sb, depth).AppendLine("[ ]");
			}
			else
			{
				WriteIndent(sb, depth).AppendLine("[");
				WriteChildren(list.Children, depth + 1, sb);
				WriteIndent(sb, depth).AppendLine("]");
			}
		}

		static void WriteFunction(LospFunctionNode func, int depth, StringBuilder sb)
		{
			WriteIndent(sb, depth).AppendLine("FN(");

			WriteIndent(sb, depth).AppendLine(Indent + "Params:");
			WriteList(func.Params, depth + 2, sb);
			WriteIndent(sb, depth).AppendLine(Indent + "Body:");
			WriteChildren(func.Children, depth + 1, sb);

			WriteIndent(sb, depth).AppendLine(")");
		}

		static void WriteObject(LospObjectLiteralNode obj, int depth, StringBuilder sb)
		{
			if (obj.Children.Count == 0 && obj.Tags.Count == 0)
			{
				WriteIndent(sb, depth).AppendLine("{{ }}");
			}
			else
			{
				WriteIndent(sb, depth).AppendLine("{{");

				if (obj.Tags.Count > 0)
				{
					WriteIndent(sb, depth).AppendLine(Indent + "Tags: " + string.Join(", ", obj.Tags));
				}
				if (obj.Children.Count > 0)
				{
					WriteChildren(obj.Children, depth + 1, sb);
				}

				WriteIndent(sb, depth).AppendLine("}}");
			}
		}

		static void WriteChildren(LospChildCollection children, int depth, StringBuilder sb)
		{
			foreach (var child in children)
			{
				WriteNode(child, depth, sb);
			}
		}

		public static StringBuilder WriteNode(LospNode node, StringBuilder? sb = null)
		{
			return WriteNode(node, 0, sb);
		}

		public static StringBuilder WriteNode(LospNode node, int depth, StringBuilder? sb = null)
		{
			sb ??= new();

			if (node is LospOperatorNode op)
			{
				WriteOperator(op, depth, sb);
			}
			else if (node is LospListNode list)
			{
				WriteList(list, depth, sb);
			}
			else if (node is LospObjectLiteralNode obj)
			{
				WriteObject(obj, depth, sb);
			}
			else if (node is LospKeyValueNode kv)
			{
				WriteKV(kv, depth, sb);
			}
			else if (node is LospFunctionNode func)
			{
				WriteFunction(func, depth, sb);
			}
			else if (node is LospLiteralNode data)
			{
				WriteIndent(sb, depth);
				WriteValue(data.Data, sb).AppendLine();
			}
			else if (node is LospIdentifierNode id)
			{
				WriteIndent(sb, depth)
					.AppendLine(id.Name);
			}
			else
			{
				WriteIndent(sb, depth)
					.AppendLine(node.Type.ToString());
			}

			return sb;
		}

		public static StringBuilder WriteValue(LospValue value, StringBuilder sb)
		{
			if (value == null || value is LospNull || value.BoxedValue == null) return sb.Append("null");
			else if (value.BoxedValue is string str) return sb.Append('"').Append(str).Append('"');
			else return sb.Append(value.BoxedValue.ToString());
		}

		/// <summary>
		/// Prints an <see cref="LospResult"/> as a string intended to be used e.g. as
		/// output to a REPL. For a <see cref="ValueResult"/>, the verbosity of the
		/// printed values is controlled by <paramref name="underlyingValueOnly"/>.
		/// </summary>
		/// <param name="result">The result type to print.</param>
		/// <param name="underlyingValueOnly">When printing <see cref="LospValue"/>s,
		/// determines whether it value is annotated with its type. When <see langword="true"/>,
		/// the <see cref="LospValue"/>'s underlying value is printed with no type
		/// annotation.</param>
		/// <returns>The <paramref name="result"/> as a string.</returns>
		public static string WriteResult(LospResult result, bool underlyingValueOnly)
		{
			switch (result)
			{
				case LospValueResult vr:
					if (vr.Type == ResultType.SuccessNoEmit)
					{
						return "<success>";
					}
					else
					{
						var list = vr.Values.ToList();
						if (list.Count == 1)
						{
							return WriteValue(list[0], underlyingValueOnly);
						}
						return "[" + string.Join(", ", list.Select(v => WriteValue(v, underlyingValueOnly))) + "]";
					}
				case LospErrorResult er:
					var str = (er.Source is LospOperatorNode op ? op.NodeId + ": " : string.Empty) + er.Message;
					return "<error: " + str + ">";
				case LospAsyncResult:
					return "<async>";
				default:
					return $"<unexpected result type: {result.Type}>";
			}
		}

		/// <summary>
		/// Prints an <see cref="EvalResult"/> as a string intended to be used e.g. as
		/// output to a REPL. For a <see cref="ValueResult"/>, the verbosity of the
		/// printed values is controlled by <paramref name="underlyingValueOnly"/>.
		/// </summary>
		/// <param name="result">The result type to print.</param>
		/// <param name="underlyingValueOnly">When printing <see cref="LospValue"/>s,
		/// determines whether it value is annotated with its type. When <see langword="true"/>,
		/// the <see cref="LospValue"/>'s underlying value is printed with no type
		/// annotation.</param>
		/// <returns>The <paramref name="result"/> as a string.</returns>
		public static string WriteResult(EvalResult result, bool underlyingValueOnly)
		{
			switch (result)
			{
				case ErrorResult er:
					var str = (er.Source is LospOperatorNode op ? op.NodeId + ": " : string.Empty) + er.Message;
					return "<error: " + str + ">";
				case AsyncResult:
					return "<async>";
				case PushResult:
					return "<push>";
				case ValueResult vr:
					if (vr.Type == ResultType.SuccessNoEmit)
					{
						return "<success>";
					}
					else
					{
						var list = vr.Values.ToList();
						if (list.Count == 1)
						{
							return WriteValue(list[0], underlyingValueOnly);
						}
						return "[" + string.Join(", ", vr.Values.Select(v => WriteValue(v, underlyingValueOnly))) + "]";
					}
				default:
					return $"<unexpected result type: {result.Type}>";
			}
		}

		/// <summary>
		/// Prints a <see cref="LospValue"/> as a string intended to be used e.g. as
		/// output to a REPL. The verbosity of the printed value (and any nested values)
		/// is controlled by <paramref name="underlyingValueOnly"/>.
		/// </summary>
		/// <param name="value">The value to print.</param>
		/// <param name="underlyingValueOnly">Determines whether the output is annotated
		/// with types. When <see langword="true"/>, the <paramref name="value"/> and any
		/// nested values are printed with no type annotation.</param>
		/// <returns>The <paramref name="value"/> as a string.</returns>
		public static string WriteValue(LospValue value, bool underlyingValueOnly)
		{
			var uvo = underlyingValueOnly;

			return value switch
			{
				LospNull => uvo ? "null" : "<null> null",
				LospValue<int> i => uvo ? i.Value.ToString() : "<int> " + i.Value.ToString(),
				LospValue<float> f => uvo ? f.Value.ToString() : "<float> " + f.Value.ToString(),
				LospValue<bool> b => uvo ? b.Value.ToString() : "<bool> " + b.Value.ToString(),
				LospValue<string> s => uvo ? (s.Value ?? "null") : "<string> " + (s.Value ?? "null"),
				LospList list => "<list> " + WriteList(list.Value, uvo),
				LospFunc => "<lambda>",
				LospScriptable s => "<scriptable> " + WriteScriptable(s.Value, uvo),
			};
		}

		private static readonly List<(List<string> List, bool Used)> _reusableStringLists = [];
		private static List<string> BorrowList()
		{
			for (var i = 0; i < _reusableStringLists.Count; i++)
			{
				if (!_reusableStringLists[i].Used)
				{
					var list = _reusableStringLists[i].List;
					_reusableStringLists[i] = (list, true);
					return list;
				}
			}

			var newPair = (new List<string>(), true);
			_reusableStringLists.Add(newPair);
			return newPair.Item1;
		}
		private static void ReturnList(List<string> list)
		{
			list.Clear();
			for (var i = 0; i < _reusableStringLists.Count; i++)
			{
				if (_reusableStringLists[i].List == list)
				{
					_reusableStringLists[i] = (list, false);
					return;
				}
			}
		}

		public static string WriteList(IEnumerable<LospValue>? list, bool underlyingValueOnly)
		{
			if (list == null) return "null";

			var strList = BorrowList();

			foreach (var val in list)
			{
				strList.Add(WriteValue(val, underlyingValueOnly));
			}

			var str = "[" + string.Join(" ", strList) + "]";

			ReturnList(strList);
			return str;
		}

		public static string WriteScriptable(IScriptObject? scriptable, bool underlyingValueOnly)
		{
			if (scriptable == null) return "null";

			var strList = BorrowList();

			foreach (var key in scriptable.Keys)
			{
				strList.Add("{" + key + WriteValue(scriptable.Get(key)!, underlyingValueOnly) + "}");
			}

			var str = string.Join(" ", strList);

			ReturnList(strList);
			return str;
		}
	}
}
