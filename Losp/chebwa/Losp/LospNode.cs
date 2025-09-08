using System.Collections;
using System.Diagnostics.CodeAnalysis;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace chebwa.LospNet
{
	public enum LospNodeType
	{
		Operator,
		Filter,
		Identifier,
		KeyValue,
		Literal,
		Object,
		List,
		Function,
	}

	public enum AllowedChildTypes
	{
		All,
		KV,
		NonKV,
	}

	public enum LospFilterPosition
	{
		/// <summary>
		/// The filter is the first in a chained series (or stands alone).
		/// </summary>
		Head,
		/// <summary>
		/// The filter is part of the body of a series of chained filters.
		/// </summary>
		Chain,
	}

	public abstract class LospNode()
	{
		public abstract LospNodeType Type { get; }

		public LospIdentifierNode? IdNode;
		public string NodeId => IdNode?.Name ?? string.Empty;
		public LospToken? SourceToken = null;

		public virtual LospChildCollection? Children { get; } = null;

		public override string ToString()
		{
			return $"LospNode (type = {Type})";
		}
	}

	public class LospOperatorNode() : LospNode()
	{
		public override LospNodeType Type => LospNodeType.Operator;

		public override LospChildCollection Children { get; } = [];

		public override string ToString()
		{
			return $"LospOperatorNode ({NodeId})";
		}
	}

	public class LospSpecialOperatorNode() : LospOperatorNode()
	{
		public LospChildCollection SpecialOperatorChildren { get; } = [];

		public static LospSpecialOperatorNode FromOperator(LospOperatorNode op, string? name = null)
		{
			var token = name == null
				? op.IdNode!.SourceToken
				: LospToken.SymbolFromString(name);

			return new LospSpecialOperatorNode
			{
				IdNode = new()
				{
					SourceToken = token,
				},
			};
		}
	}

	/// <summary>
	/// A <see cref="LospFilterNode"/> functions nearly identically to a
	/// <see cref="LospOperatorNode"/>, with two notable and related differences. First,
	/// filters may be <em>chained</em>, in which the output from one filter is passed
	/// to the next in the chain sequence. Second, as alluded to, the primary value on
	/// which the filter operates is supplied by its parent node <em>or</em> by the
	/// preceding filter node in the chain. Whereas all parameters of a
	/// <see cref="LospOperatorNode"/> come from child nodes, filters rely on their
	/// context.
	/// </summary>
	/// <param name="chained"></param>
	public class LospFilterNode(bool chained) : LospNode()
	{
		public override LospNodeType Type => LospNodeType.Filter;
		public readonly LospFilterPosition FilterPosition
			= chained
			? LospFilterPosition.Chain
			: LospFilterPosition.Head;

		/// <summary>
		/// The next filter in the chain sequence, if any.
		/// </summary>
		public LospFilterNode? NextFilter = null;

		public override LospChildCollection Children { get; } = [];
	}

	/// <summary>
	/// A node which allows a parent <see cref="LospNode"/> to associate one or more
	/// values (derived from the nodes in <see cref="Children"/>) with a key string, as
	/// provided by <see cref="Key"/>.
	/// </summary>
	public class LospKeyValueNode() : LospNode()
	{
		public override LospNodeType Type => LospNodeType.KeyValue;
		public readonly List<string> Tags = [];

		public override LospChildCollection Children { get; } = [];
	}

	/// <summary>
	/// A context-dependent identifier. This may, for example, be the name of a
	/// <see cref="LospOperatorNode"/> or <see cref="LospFilterNode"/>, or it may be
	/// the name of a variable to be retrieved or assigned.
	/// </summary>
	/// <param name="token">The source token which defines the id string.</param>
	public class LospIdentifierNode() : LospNode()
	{
		public override LospNodeType Type => LospNodeType.Identifier;

		public string Name => SourceToken?.Raw() ?? string.Empty;

		public override string ToString()
		{
			return $"LospLiteralNode ({Name})";
		}
	}

	/// <summary>
	/// The base class of a node that represents a literal value.
	/// </summary>
	/// <param name="token"></param>
	public class LospLiteralNode() : LospNode()
	{
		public override LospNodeType Type => LospNodeType.Literal;

		public required LospValue Data;

		public override string ToString()
		{
			return $"LospLiteralNode ({Data})";
		}
	}

	/// <summary>
	/// A basic collection of child nodes. <see cref="LospKeyValueNode"/> are not
	/// allowed as child nodes.
	/// </summary>
	public class LospListNode() : LospNode()
	{
		public override LospNodeType Type => LospNodeType.List;

		public override LospChildCollection Children { get; } = new(AllowedChildTypes.NonKV);

		public override string ToString()
		{
			return $"LospListNode ({Children.Count} children)";
		}
	}

	public class LospObjectLiteralNode() : LospNode()
	{
		public override LospNodeType Type => LospNodeType.Object;
		public readonly List<string> Tags = [];

		public override LospChildCollection Children { get; } = new(AllowedChildTypes.KV);
	}

	/// <summary>
	/// Note that this node's <see cref="LospNode.Children"/> do not contain the parameter
	/// list. The parameter list instead is stripped and becomes the <see cref="Params"/>.
	/// </summary>
	public class LospFunctionNode() : LospNode()
	{
		public override LospNodeType Type => LospNodeType.Function;
		public required LospListNode Params;

		public override LospChildCollection Children { get; } = [];
	}

	//TODO: instead of a bool, use AllowedChildType.All, NonKV, KV
	/// <summary>
	/// A collection of child nodes where the children exist in a list and can be
	/// enumerated, and may also be keyed (if the child is a <see cref="LospKeyValueNode"/>)
	/// and accessed via the key string. The list of keys can be accessed, and key/value
	/// pairs can be enumerated. Note that for list nodes, the parser disallows the use
	/// of keyed children, and thus the collection will not have any keys.
	/// </summary>
	/// <param name="disallowKeys">Determines whether the collection will allow
	/// <see cref="LospKeyValueNode"/> child nodes.</param>
	public class LospChildCollection(AllowedChildTypes childTypes = AllowedChildTypes.All) : IEnumerable<LospNode>
	{
		public readonly AllowedChildTypes AllowedChildTypes = childTypes;

		private readonly List<LospNode> _list = [];
		/// <summary>
		/// Returns the sequential list of child nodes contained by this collection.
		/// To add a child node, use <see cref="Add(LospNode)"/>.
		/// </summary>
		public IReadOnlyList<LospNode> List => _list;

		/// <summary>
		/// The number of child nodes in the collection.
		/// </summary>
		public int Count => _list.Count;

		private readonly Dictionary<string, int> _keyedChildren = [];

		/// <summary>
		/// Adds the <paramref name="node"/> as a child node to the collection.
		/// If the <paramref name="node"/> is a <see cref="LospKeyValueNode"/>,
		/// the node's key is mapped to the new child's index.
		/// </summary>
		/// <param name="node">The <see cref="LospNode"/> to add as a child node.</param>
		public void Add(LospNode node)
		{
			if (node == null)
			{
				return;
			}

			if (AllowedChildTypes == AllowedChildTypes.KV && node is not LospKeyValueNode)
			{
				throw new Exception("only KV child nodes allowed");
			}
			else if (AllowedChildTypes == AllowedChildTypes.NonKV && node is LospKeyValueNode)
			{
				throw new Exception("only non-KV child nodes allowed");
			}

			if (node is LospKeyValueNode kv)
			{
				_keyedChildren[kv.NodeId] = _list.Count;
			}

			_list.Add(node);
		}

		public IEnumerator<LospNode> GetEnumerator()
		{
			return ((IEnumerable<LospNode>)_list).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_list).GetEnumerator();
		}

		/// <summary>
		/// Retrieves a child node by its index.
		/// </summary>
		public LospNode this[int index]
		{
			get { return _list[index]; }
		}
		public LospNode? this[string key]
		{
			get
			{
				if (TryKey(key, out var kv))
				{
					return kv;
				}
				return null;
			}
		}

		public bool TryKey(string key, [NotNullWhen(true)] out LospKeyValueNode? kv)
		{
			if (_keyedChildren.TryGetValue(key, out var i))
			{
				kv = _list[i] as LospKeyValueNode;
				return kv != null;
			}

			kv = null;
			return false;
		}

		public bool TryIndex<T>(int index, [NotNullWhen(true)] out T? value) where T : LospNode
		{
			if (Count > index)
			{
				if (this[index] is T val)
				{
					value = val;
					return true;
				}
			}

			value = null;
			return false;
		}

		//public bool TryGetType<T>([NotNullWhen(true)] out T? node) where T : LospNode
		//{
		//	foreach (var child in _list)
		//	{
		//		if (child is T n)
		//		{
		//			node = n;
		//			return true;
		//		}
		//	}

		//	node = null;
		//	return false;
		//}
	}
}
