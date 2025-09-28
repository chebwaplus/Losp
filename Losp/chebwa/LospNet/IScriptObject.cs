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
		void Set(string key, LospValue value);
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

		public void Set(string key, LospValue value)
		{
			this[key] = value;
		}

		public bool TryKey(string key, [NotNullWhen(true)] out LospValue? value)
		{
			return Map.TryGetValue(key, out value);
		}
		public bool TryKeyAs<T>(string key, [NotNullWhen(true)] out LospValue<T>? value)
		{
			if (!TryKey(key, out var val))
			{
				value = null;
				return false;
			}

			if (val is not LospValue<T> valT)
			{
				value = null;
				return false;
			}

			value = valT;
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

	/// <summary>
	/// A record type that is used by <see cref="OpaqueLambdaScriptObject"/> and
	/// <see cref="LambdaScriptObject{T}"/> to make values from underlying data
	/// available to the Losp runtime.
	/// </summary>
	public sealed record LambdaProperty(string Key, Func<LospValue> Getter, Action<LospValue>? Setter = null);

	/// <summary>
	/// An <see cref="IScriptObject"/> which does not expose its underlying object
	/// but presents a subset of members as scriptable via a list of
	/// <see cref="LambdaProperty"/> entries.
	/// A <see cref="LambdaScriptObject{T}"/> can be used instead to expose the
	/// underlying object.
	/// </summary>
	/// <param name="properties"><see cref="LambdaProperty"/> entries used to expose
	/// an object to the scripting environment.</param>
	public class OpaqueLambdaScriptObject(IEnumerable<LambdaProperty> properties) : IScriptObject
	{
		private readonly Dictionary<string, LambdaProperty> _properties = CreateDict(properties);

		public IEnumerable<string> Keys => _properties.Keys;

		public LospValue? Get(string key)
		{
			TryKey(key, out var value);
			return value;
		}

		public void Set(string key, LospValue value)
		{
			if (_properties.TryGetValue(key, out var prop) && prop.Setter != null)
			{
				prop.Setter(value);
			}
		}

		public bool TryKey(string key, [NotNullWhen(true)] out LospValue? value)
		{
			if (_properties.TryGetValue(key, out var prop))
			{
				value = prop.Getter();
				return true;
			}

			value = default;
			return false;
		}

		public bool TryClear(string key)
		{
			return false;
		}

		public Dictionary<string, T> ToDictionary<T>(Func<KeyValuePair<string, LospValue>, T> toValue)
		{
			var dict = new Dictionary<string, T>();
			foreach (var prop in _properties.Values)
			{
				dict[prop.Key] = toValue(new(prop.Key, prop.Getter()));
			}

			return dict;
		}

		public static Dictionary<string, LambdaProperty> CreateDict(IEnumerable<LambdaProperty> props)
		{
			var dict = new Dictionary<string, LambdaProperty>();

			foreach (var prop in props)
			{
				dict[prop.Key] = prop;
			}

			return dict;
		}
	}

	/// <summary>
	/// A variant of <see cref="OpaqueLambdaScriptObject"/> that exposes the underlying
	/// object.
	/// </summary>
	/// <param name="value">The underlying object.</param>
	/// <param name="properties"><see cref="LambdaProperty"/> entries used to expose
	/// an object to the scripting environment.</param>
	public class LambdaScriptObject<T>(T value, IEnumerable<LambdaProperty> properties)
		: OpaqueLambdaScriptObject(properties), IScriptObject<T>
	{
		public T TypedObject { get; } = value;
	}

	/// <summary>
	/// An <see cref="IScriptObject"/> which does not expose its underlying object
	/// but presents all public fields and properties as scriptable through the
	/// <see cref="IScriptObject"/> interface.
	/// A <see cref="ReflectionScriptObject{T}"/> can be used instead to expose the
	/// underlying object.
	/// </summary>
	/// <param name="value">The underlying object.</param>
	public class OpaqueReflectionScriptObject<T>(T value) : IScriptObject where T : class
	{
		protected readonly T Value = value;
		protected readonly Type TType = value.GetType();

		public IEnumerable<string> Keys
		{
			get
			{
				foreach (var f in TType.GetFields(System.Reflection.BindingFlags.Public))
				{
					yield return f.Name;
				}

				foreach (var p in TType.GetProperties(System.Reflection.BindingFlags.Public))
				{
					if (p.CanRead)
					{
						yield return p.Name;
					}
				}
			}
		}

		public LospValue? Get(string key)
		{
			TryKey(key, out var value);
			return value;
		}

		public void Set(string key, LospValue value)
		{
			var propInfo = TType.GetProperty(key);
			if (propInfo != null)
			{
				propInfo.SetValue(this, value);
				return;
			}

			var fieldInfo = TType.GetField(key);
			if (fieldInfo != null)
			{
				fieldInfo.SetValue(this, value);
				return;
			}
		}

		public bool TryKey(string key, [NotNullWhen(true)] out LospValue? value)
		{
			var propInfo = TType.GetProperty(key);
			if (propInfo != null)
			{
				value = LospValue.Convert(propInfo.GetValue(Value));
				return true;
			}

			var fieldInfo = TType.GetField(key);
			if (fieldInfo != null)
			{
				value = LospValue.Convert(fieldInfo.GetValue(Value));
				return true;
			}

			value = default;
			return false;
		}

		public Dictionary<string, TValue> ToDictionary<TValue>(Func<KeyValuePair<string, LospValue>, TValue> toValue)
		{
			var dict = new Dictionary<string, TValue>();

			foreach (var f in TType.GetFields(System.Reflection.BindingFlags.Public))
			{
				dict[f.Name] = toValue(new(f.Name, LospValue.Convert(f.GetValue(Value))));
			}

			foreach (var p in TType.GetProperties(System.Reflection.BindingFlags.Public))
			{
				if (p.CanRead)
				{
					dict[p.Name] = toValue(new(p.Name, LospValue.Convert(p.GetValue(Value))));
				}
			}

			return dict;
		}

		public bool TryClear(string key)
		{
			return false;
		}
	}

	/// <summary>
	/// A variant of <see cref="OpaqueReflectionScriptObject{T}"/> which exposes the
	/// underlying object.
	/// </summary>
	/// <param name="value">The underlying object.</param>
	public class ReflectionScriptObject<T>(T value)
		: OpaqueReflectionScriptObject<T>(value), IScriptObject<T> where T : class
	{
		public T TypedObject => Value;
	}
}
