using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace chebwa.LospNet
{
	public class LospChildResultDataCollection : IEnumerable<LospValue>
	{
		public class UnkeyedCollection(LospChildResultDataCollection list)
		{
			public int Count => list._unkeyedChildren.Count;

			public LospValue this[int index]
			{
				get
				{
					return list[list._unkeyedChildren[index]];
				}
			}

			public bool TryIndex<T>(int index, [NotNullWhen(true)] out T? value) where T : LospValue
			{
				if (list._unkeyedChildren.Count > index)
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
			public bool TryIndexOf<T>(int index, out T? value)
			{
				if (list._unkeyedChildren.Count > index)
				{
					return this[index].TryGet(out value);
				}

				value = default;
				return false;
			}
			public bool TryIndexOfNonNull<T>(int index, [NotNullWhen(true)] out T? value) where T : class
			{
				if (list._unkeyedChildren.Count > index)
				{
					return this[index].TryGetNonNull(out value);
				}

				value = default;
				return false;
			}
		}

		public LospChildResultDataCollection(bool keysAllowed)
		{
			KeysAllowed = keysAllowed;
			Unkeyed = new(this);
		}

		public ErrorResult? Error;

		/// <summary>
		/// Indicates whether <see cref="LospKeyValueNode"/> nodes may be added to the
		/// collection. If allowed, the node's key string is associated with the node
		/// itself via this instance's various key-related members.
		/// </summary>
		public readonly bool KeysAllowed;

		private readonly List<LospValue> _list = [];
		/// <summary>
		/// Returns the sequential list of child nodes contained by this collection.
		/// To add a child node, use <see cref="Add(LospValue)"/>.
		/// </summary>
		public IReadOnlyList<LospValue> List => _list;

		private readonly List<int> _unkeyedChildren = [];

		private readonly Dictionary<string, int> _keyedChildren = [];
		/// <summary>
		/// A map of keys and the index of the child to which each key points.
		/// Use <see cref="KeyedPairs"/> to enumerate over each key with its
		/// corresponding <see cref="LospValue"/>.
		/// </summary>
		public IReadOnlyDictionary<string, int> KeyedChildren => _keyedChildren;

		/// <summary>
		/// The number of child nodes in the collection.
		/// </summary>
		public int Count => _list.Count;

		/// <summary>
		/// A subcollection of only unkeyed values. Can be used to reference an unkeyed
		/// value by ordinal position among unkeyed values. For example, consider an
		/// operator with a source script <c>(SPEAK {mood "angry"} "How dare you!?")</c>.
		/// The "mood" key is optional, and perhaps one of several optional keys.
		/// Instead of having to loop over each index to find the first one that is not
		/// keyed, you can use <c>collection.Unkeyed[0]</c> to directly retrieve its
		/// value. This is slightly more natural and simple than using e.g.
		/// <c>collection.<see cref="UnkeyedValues">UnkeyedValues</see>.First()</c>.
		/// </summary>
		public readonly UnkeyedCollection Unkeyed;

		/// <summary>
		/// Adds the <paramref name="node"/> as a child node to the collection.
		/// If the <paramref name="node"/> is a <see cref="LospValue"/>,
		/// the node's key is mapped to the new child's index.
		/// </summary>
		/// <param name="node">The <see cref="LospValue"/> to add as a child node.</param>
		public void Add(LospValue node, string? key = null)
		{
			if (key != null)
			{
				if (KeysAllowed)
				{
					_keyedChildren[key] = _list.Count;
					_list.Add(node);
				}

				return;
			}

			// handle all non-kv cases

			_unkeyedChildren.Add(_list.Count);
			_list.Add(node);
		}

		/// <summary>
		/// <para>
		/// Attempts to retrieve a child <see cref="LospValue"/> via a key
		/// string. If the key exists, the node is assigned to <paramref name="kv"/>
		/// and <see langword="true"/> is returned. Otherwise <see langword="false"/>
		/// is returned.
		/// </para>
		/// <para>
		/// Note that if <see cref="KeysAllowed"/> is <see langword="false"/>, no keys
		/// will be associated with a child node.
		/// </para>
		/// </summary>
		/// <param name="key">The key string used to look up the child node.</param>
		/// <param name="kv">The retrieved child node, if successful.</param>
		public bool TryKey(string key, [NotNullWhen(true)] out LospValue? kv)
		{
			if (_keyedChildren.TryGetValue(key, out var i))
			{
				kv = _list[i];
				return true;
			}

			kv = null;
			return false;
		}
		public bool TryKeyOf<T>(string key, out T? value)
		{
			if (TryKey(key, out var kv) && kv is LospValue<T> v)
			{
				value = v.Value;
				return true;
			}

			value = default;
			return false;
		}

		public bool TryIndex<T>(int index, [NotNullWhen(true)] out T? value) where T : LospValue
		{
			if (_list.Count > index)
			{
				if (_list[index] is T val)
				{
					value = val;
					return true;
				}
			}

			value = null;
			return false;
		}
		public bool TryIndexOf<T>(int index, out T? value)
		{
			if (_list.Count > index)
			{
				return _list[index].TryGet(out value);
			}

			value = default;
			return false;
		}
		public bool TryIndexOfNonNull<T>(int index, [NotNullWhen(true)] out T? value) where T : class
		{
			if (_list.Count > index)
			{
				return _list[index].TryGetNonNull(out value);
			}

			value = default;
			return false;
		}

		public IEnumerator<LospValue> GetEnumerator()
		{
			return ((IEnumerable<LospValue>)_list).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_list).GetEnumerator();
		}

		/// <summary>
		/// Retrieves a child node by its index.
		/// </summary>
		public LospValue this[int index]
		{
			get { return _list[index]; }
		}
		/// <summary>
		/// Attempts to retrieve a child node by its key string. If the retrieval
		/// fails, <see langword="null"/> is returned. See also
		/// <see cref="TryKey(string, out LospValue)"/>.
		/// </summary>
		public LospValue? this[string key]
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

		/// <summary>
		/// Enumerates all key strings defined by <see cref="LospValue"/>
		/// child nodes.
		/// </summary>
		public IEnumerable<string> Keys => _keyedChildren.Keys;

		/// <summary>
		/// Enumerates all <see cref="LospValue"/> child nodes, each with its
		/// key string.
		/// </summary>
		public IEnumerable<KeyValuePair<string, LospValue>> KeyedPairs
		{
			get
			{
				foreach (var kv in _keyedChildren)
				{
					yield return new(kv.Key, this[kv.Value]);
				}
			}
		}

		public IEnumerable<int> UnkeyedIndices => _unkeyedChildren;

		public IEnumerable<LospValue> UnkeyedValues
		{
			get
			{
				foreach (var idx in _unkeyedChildren)
				{
					yield return this[idx];
				}
			}
		}
	}
}
