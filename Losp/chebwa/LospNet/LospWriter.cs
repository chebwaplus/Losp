// SPDX-License-Identifier: MIT

using chebwa.LospNet;
using System.Text;

namespace chebwa.Losp
{
	internal static class LospWriter
	{
		const string Indent = "  ";
		static readonly StringBuilder _cmdsb = new();

		static StringBuilder WriteIndent(StringBuilder sb, int depth)
		{
			sb.Clear();
			for (var i = 0; i < depth; i++)
			{
				sb.Append(Indent);
			}
			return sb;
		}

		static void WriteOperator(LospOperatorNode op, int depth, StringBuilder sb)
		{
			var indent = WriteIndent(_cmdsb, depth).ToString();
			sb.AppendLine(indent + op.NodeId + "()");

			WriteChildren(op.Children, indent, depth, sb);
		}

		static void WriteFilter(LospFilterNode filter, int depth, StringBuilder sb)
		{
			var indent = WriteIndent(_cmdsb, depth).ToString();
			if (filter.FilterPosition == LospFilterPosition.Head)
			{
				sb.AppendLine(indent + "# " + filter.NodeId + "()");
			}
			else
			{
				sb.AppendLine(indent + "|> # " + filter.NodeId + "()");
			}

			WriteChildren(filter.Children, indent, depth, sb);
		}

		static void WriteKV(LospKeyValueNode kv, int depth, StringBuilder sb)
		{
			var indent = WriteIndent(_cmdsb, depth).ToString();

			if (kv.Children.Count == 0 && kv.Tags.Count == 0)
			{
				sb.AppendLine(indent + "{" + kv.NodeId + "}");
			}
			else
			{
				sb.AppendLine(indent + "{" + kv.NodeId + ":");

				if (kv.Tags.Count > 0)
				{
					sb.AppendLine(indent + Indent + "Tags: " + string.Join(", ", kv.Tags));
				}
				if (kv.Children.Count > 0)
				{
					WriteChildren(kv.Children, indent, depth, sb);
				}

				sb.AppendLine(indent + "}");
			}
		}

		static void WriteList(LospListNode list, int depth, StringBuilder sb)
		{
			var indent = WriteIndent(_cmdsb, depth).ToString();

			if (list.Children.Count == 0)
			{
				sb.AppendLine(indent + "[ ]");
			}
			else
			{
				sb.AppendLine(indent + "[");
				WriteChildren(list.Children, indent, depth, sb);
				sb.AppendLine(indent + "]");
			}
		}

		static void WriteFunction(LospFunctionNode func, int depth, StringBuilder sb)
		{
			var indent = WriteIndent(_cmdsb, depth).ToString();

			sb.AppendLine(indent + "FN(");

			sb.AppendLine(indent + Indent + "Params:");
			WriteList(func.Params, depth + 2, sb);
			sb.AppendLine(indent + Indent + "Body:");
			WriteChildren(func.Children, indent + Indent, depth + 1, sb);

			sb.AppendLine(indent + ")");
		}

		static void WriteObject(LospObjectLiteralNode obj, int depth, StringBuilder sb)
		{
			var indent = WriteIndent(_cmdsb, depth).ToString();

			if (obj.Children.Count == 0 && obj.Tags.Count == 0)
			{
				sb.AppendLine(indent + "{{ }}");
			}
			else
			{
				sb.AppendLine(indent + "{{");

				if (obj.Tags.Count > 0)
				{
					sb.AppendLine(indent + Indent + "Tags: " + string.Join(", ", obj.Tags));
				}
				if (obj.Children.Count > 0)
				{
					WriteChildren(obj.Children, indent, depth, sb);
				}

				sb.AppendLine(indent + "}}");
			}
		}

		static void WriteChildren(LospChildCollection children, string indent, int parentDepth, StringBuilder sb)
		{
			foreach (var child in children)
			{
				WriteNode(child, indent, parentDepth, sb);
			}
		}

		public static StringBuilder WriteNode(LospNode node, StringBuilder? sb = null)
		{
			return WriteNode(node, string.Empty, 0, sb);
		}

		public static StringBuilder WriteNode(LospNode node, string indent, int parentDepth, StringBuilder? sb = null)
		{
			sb ??= new();

			if (node is LospOperatorNode op)
			{
				WriteOperator(op, parentDepth + 1, sb);
			}
			else if (node is LospFilterNode filter)
			{
				WriteFilter(filter, parentDepth + 1, sb);
			}
			else if (node is LospListNode list)
			{
				WriteList(list, parentDepth + 1, sb);
			}
			else if (node is LospObjectLiteralNode obj)
			{
				WriteObject(obj, parentDepth + 1, sb);
			}
			else if (node is LospKeyValueNode kv)
			{
				WriteKV(kv, parentDepth + 1, sb);
			}
			else if (node is LospFunctionNode func)
			{
				WriteFunction(func, parentDepth + 1, sb);
			}
			else if (node is LospLiteralNode data)
			{
				sb.AppendLine(indent + Indent + data.Data.BoxedValue?.ToString() ?? "");
			}
			else if (node is LospIdentifierNode id)
			{
				sb.AppendLine(indent + Indent + id.Name);
			}
			else
			{
				sb.AppendLine(indent + Indent + node.Type.ToString());
			}

			return sb;
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
						return "[" + string.Join(", ", vr.Values.Select(v => WriteValue(v, underlyingValueOnly))) + "]";
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
