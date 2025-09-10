// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;

namespace chebwa.LospNet
{
	/// <summary>
	/// An object with values that can be retrieved and/or set via a standard interface,
	/// and thus can be manipulated by the Losp scripting environment.
	/// </summary>
	public interface IScriptObject
	{
		IEnumerable<string> Keys { get; }
		LospValue? Get(string key);
		void Set(string key, LospValue value);
		bool TryKey(string key, [NotNullWhen(true)] out LospValue? value);
		bool TryClear(string key);
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

	public record LambdaProperty(string Key, Func<LospValue> Getter, Action<LospValue>? Setter);

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
