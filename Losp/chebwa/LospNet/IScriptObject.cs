// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;

namespace chebwa.LospNet
{
	public static class ScriptObjectExt
	{
		/// <summary>
		/// Equivalent to calling <see cref="IScriptObject.TryKey(string, out LospValue?)"/>
		/// then <see cref="LospValue.TryGet{T}(out T)"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj">The <see cref="IScriptObject"/> context.</param>
		/// <param name="key">The key name associated with the value.</param>
		/// <param name="value">The <see cref="LospValue"/> to retrieve.</param>
		/// <returns>A <see langword="bool"/> indicating whether a <paramref name="value"/>
		/// of type <typeparamref name="T"/> was retrieved from the <see cref="LospValue"/>
		/// associated with the <paramref name="key"/>.</returns>
		public static bool TryKeyOf<T>(this IScriptObject obj, string key, out T? value)
		{
			if (!obj.TryKey(key, out var val))
			{
				value = default;
				return false;
			}

			return val.TryGet(out value);
		}

		/// <summary>
		/// Equivalent to calling <see cref="IScriptObject.TryKey(string, out LospValue?)"/>
		/// then <see cref="LospValue.TryGetNonNull{T}(out T)"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj">The <see cref="IScriptObject"/> context.</param>
		/// <param name="key">The key name associated with the value.</param>
		/// <param name="value">The <see cref="LospValue"/> to retrieve.</param>
		/// <returns>A <see langword="bool"/> indicating whether a <paramref name="value"/>
		/// of type <typeparamref name="T"/> was retrieved from the <see cref="LospValue"/>
		/// associated with the <paramref name="key"/>.</returns>
		public static bool TryKeyNonNull<T>(this IScriptObject obj, string key, [NotNullWhen(true)] out T? value) where T : class
		{
			if (!obj.TryKey(key, out var val))
			{
				value = default;
				return false;
			}

			return val.TryGetNonNull(out value);
		}
	}

	/// <summary>
	/// An object with values that can be retrieved and/or set via a standard interface,
	/// and thus can be manipulated by the Losp scripting environment.
	/// </summary>
	public interface IScriptObject
	{
		IEnumerable<string> Keys { get; }
		/// <summary>
		/// <para>
		/// Returns a <see cref="LospValue"/> associated with the <paramref name="key"/>,
		/// if any, or <see langword="null"/> if no associated value exists.
		/// </para>
		/// <para>
		/// Classes that implement <see cref="IScriptObject"/> should return
		/// <see langword="null"/> as opposed to throwing an exception when no value
		/// for <paramref name="key"/> exists.
		/// </para>
		/// </summary>
		/// <param name="key">The key name associated with the value.</param>
		LospValue? Get(string key);
		/// <summary>
		/// Attempts to store the <paramref name="value"/> using the <paramref name="key"/>.
		/// Implementations are free to ignore or refuse to set a key or value, as long
		/// as they return a result appropriately indicating whether the value was
		/// applied.
		/// </summary>
		/// <param name="key">The key name associated with the value.</param>
		/// <param name="value">The value to be applied.</param>
		/// <returns>A value indicating whether the key/value pair was applied.</returns>
		bool Set(string key, LospValue value);
		/// <summary>
		/// Attempts to retrieve the value associated with the <paramref name="key"/>.
		/// </summary>
		/// <param name="key">The key name associated with the value.</param>
		/// <param name="value">The <see cref="LospValue"/> to retrieve.</param>
		/// <returns>A <see langword="bool"/> indicated whether a <paramref name="value"/>
		/// was retrieved.</returns>
		bool TryKey(string key, [NotNullWhen(true)] out LospValue? value);
		/// <summary>
		/// Attempts to remove the <paramref name="key"/> and any associated value
		/// from the object. Returns a value indicating whether the removal was a success.
		/// Some implementations of <see cref="IScriptObject"/> was not support
		/// clearing values, in which case <see cref="TryClear(string)"/> will always
		/// fail.
		/// </summary>
		/// <param name="key">The key name associated with the value.</param>
		bool TryClear(string key);
		//TODO: a SupportsClear() method/prop?
		/// <summary>
		/// <para>
		/// Generates a dictionary from the underlying data, using all <see cref="Keys"/>
		/// that have been made available.
		/// </para>
		/// <para>
		/// The <paramref name="toValue"/> map function is applied to each key/value pair
		/// of this <see cref="IScriptObject"/>, and the key is mapped to the result from
		/// <paramref name="toValue"/>. In this way, the caller has control over how
		/// each <see cref="LospValue"/> is mapped in the final dictionary.
		/// </para>
		/// <code>
		/// // in the simplest case, each LospValue is returned directly, without being mapped
		/// var directMap = obj.ToDictionary&lt;LostValue&gt;((kv) => kv.Value);
		/// 
		/// // in this case, the BoxedValue of each LospValue is extracted
		/// var valueMap = obj.ToDictionary&lt;object&gt;((kv) => kv.Value.BoxedValue);
		/// 
		/// // in this case, only string values are mapped; all other values types become null.
		/// var stringsOnly = obj.ToDictionary&lt;string&gt;((kv) => kv.Value.BoxedValue is string str ? str : null);
		/// 
		/// // note that you cannot omit (i.e. skip) keys; all keys will be present in the
		/// // resulting dictionary even if you wish to ignore them.
		/// </code>
		/// </summary>
		/// <typeparam name="TValue">The type to be returned by <paramref name="toValue"/>.</typeparam>
		/// <param name="toValue">A map function that takes a source key/value pair and
		/// transforms the pair into the output value for the resulting dictionary.
		/// </param>
		/// <returns></returns>
		Dictionary<string, TValue> ToDictionary<TValue>(Func<KeyValuePair<string, LospValue>, TValue> toValue);
	}
	public interface IScriptObject<T> : IScriptObject
	{
		T TypedObject { get; }
	}

	/// <summary>
	/// A <see cref="LospObjectLiteral"/> is a Losp-native <see cref="IScriptObject{T}"/>
	/// that is self-contained and does not wrap any underlying object.
	/// </summary>
	public class LospObjectLiteral() : IScriptObject<LospObjectLiteral>
	{
		public readonly Dictionary<string, LospValue> Map = [];
		/// <summary>
		/// Gets the first item in the list of <see cref="Tags"/>, if any.
		/// </summary>
		public string? HeadTag => Tags.Count > 0 ? Tags[0] : null;
		public readonly List<string> Tags = [];

		public LospObjectLiteral TypedObject => this;

		public LospValue this[string key]
		{
			get => Map[key];
			set => Map[key] = value;
		}

		public IEnumerable<string> Keys => Map.Keys;

		public LospValue? Get(string key) => Map.TryGetValue(key, out var value) ? value : null;

		public bool Set(string key, LospValue value)
		{
			this[key] = value;
			return true;
		}

		/// <summary>
		/// <para>
		/// Attempts to retrieve the value associated with the <paramref name="key"/>.
		/// Used if you are unsure which type of <see cref="LospValue"/> will be returned.
		/// </para>
		/// <para>
		/// If you know the type of the value contained in the <see cref="LospValue"/>,
		/// consider using <see cref="TryKeyValueAs{T}(string, out T)"/> instead to
		/// retrieve that value directly, instead of inside the <see cref="LospValue"/>
		/// wrapper.
		/// </para>
		/// <para>
		/// If you know the specific <see cref="LospValue"/> type and want the wrapper
		/// itself, consider using <see cref="TryKeyAs{T}(string, out T)"/> to retrieve
		/// that specific wrapper type.
		/// </para>
		/// <para>
		/// In some cases, such as with a <see cref="LospList"/>, it may be more
		/// convenient to use <see cref="TryKeyAs{T}(string, out T)"/> instead of
		/// <see cref="TryKeyValueAs{T}(string, out T)"/>. Consider:
		/// <code>
		/// obj.TryKeyAs&lt;LospList&gt;("key", out var list);
		/// </code>
		/// versus:
		/// <code>
		/// obj.TryKeyAsValue&lt;IEnumerable&lt;LospValue&gt;&gt;("key", out var list);
		/// </code>
		/// </para>
		/// </summary>
		/// <param name="key">The key associated with the value to retrieve.</param>
		/// <param name="value">The value, if any exists.</param>
		/// <returns>A value indicating whether a value was retrieved.</returns>
		public bool TryKey(string key, [NotNullWhen(true)] out LospValue? value)
		{
			return Map.TryGetValue(key, out value);
		}
		/// <summary>
		/// <para>
		/// Attempts to retrieve the value associated with the <paramref name="key"/>.
		/// Used if you know the specific <see cref="LospValue"/> type that will be returned.
		/// </para>
		/// <para>
		/// Use <see cref="TryKeyValueAs{T}(string, out T)"/> if you instead want
		/// to retrieve the type <i>inside</i> the <see cref="LospValue"/> instead of the
		/// <see cref="LospValue"/> wrapper itself. Note that using <see cref="TryKeyAs{T}(string, out T)"/>
		/// for e.g. a <see cref="LospList"/> can be more ergonomic.
		/// </para>
		/// </summary>
		/// <typeparam name="T">The <see cref="LospValue"/> type to be retrieved.</typeparam>
		/// <param name="key">The key associated with the value to retrieve.</param>
		/// <param name="value">The value, if any exists.</param>
		/// <returns>A value indicating whether a <see cref="LospValue"/> of the desired
		/// type was retrieved.</returns>
		public bool TryKeyAs<T>(string key, [NotNullWhen(true)] out T? value) where T : LospValue
		{
			if (!TryKey(key, out var val))
			{
				value = null;
				return false;
			}

			if (val is not T valT)
			{
				value = null;
				return false;
			}

			value = valT;
			return true;
		}
		//TODO: I believe this is equivalent to ScriptObjectExt.TryKeyOf()
		// so get rid of one? probably this one?
		/// <summary>
		/// <para>
		/// Attempts to retrieve the value associated with the <paramref name="key"/>.
		/// Used if you know the specific type stored inside the <see cref="LospValue"/>
		/// associated with the <paramref name="key"/> and want that value directly
		/// instead of the <see cref="LospValue"/> wrapper.
		/// </para>
		/// <para>
		/// Use <see cref="TryKeyAs{T}(string, out T)"/> if you instead know and want
		/// the type of <see cref="LospValue"/> wrapper instead. Note that using
		/// <see cref="TryKeyAs{T}(string, out T)"/> for e.g. a <see cref="LospList"/>
		/// can be more ergonomic.
		/// </para>
		/// </summary>
		/// <typeparam name="T">The value type to be retrieved.</typeparam>
		/// <param name="key">The key associated with the value to retrieve.</param>
		/// <param name="value">The value, if any exists.</param>
		/// <returns>A value indicating whether a value of the desired type was retrieved.</returns>
		public bool TryKeyValueAs<T>(string key, [MaybeNullWhen(true)] out T? value)
		{
			if (!TryKey(key, out var val))
			{
				value = default;
				return false;
			}

			if (val is not LospValue<T> valT)
			{
				value = default;
				return false;
			}

			value = valT.Value;
			return true;
		}

		public bool TryClear(string key)
		{
			return Map.Remove(key);
		}

		public Dictionary<string, T> ToDictionary<T>(Func<KeyValuePair<string, LospValue>, T> toValue)
		{
			return Map.ToDictionary(kv => kv.Key, toValue);
		}

		public static LospObjectLiteral FromCollection(LospChildResultDataCollection results)
		{
			var lit = new LospObjectLiteral();

			foreach (var kv in results.KeyedPairs)
			{
				lit[kv.Key] = kv.Value;
			}

			return lit;
		}
	}
}
