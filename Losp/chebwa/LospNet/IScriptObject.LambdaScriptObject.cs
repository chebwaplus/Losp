// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;

namespace chebwa.LospNet
{
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

		public bool Set(string key, LospValue value)
		{
			if (_properties.TryGetValue(key, out var prop) && prop.Setter != null)
			{
				prop.Setter(value);
				return true;
			}
			return false;
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
	/// A abstract utility class designed to provide an implementation of
	/// <see cref="IScriptObject"/> for otherwise lightweight classes without a lot of
	/// plumbing. A class can extend <see cref="SelfLambdaScriptObject{T}"/> and
	/// provide the necessary <see cref="TypedObject"/> and <see cref="LambdaProperties"/>
	/// overrides, and the utility class takes care of the rest (...mainly by
	/// delegating the work to a <see cref="LambdaScriptObject{T}"/>).
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class SelfLambdaScriptObject<T> : IScriptObject<T> where T : SelfLambdaScriptObject<T>
	{
		public abstract T TypedObject { get; }
		protected abstract IEnumerable<LambdaProperty> LambdaProperties { get; }

		private LambdaScriptObject<T>? _scriptObj;
		protected LambdaScriptObject<T> ScriptObj
		{
			get { return _scriptObj ??= new(TypedObject, LambdaProperties); }
		}

		public IEnumerable<string> Keys => ScriptObj.Keys;

		public LospValue? Get(string key) => ScriptObj.Get(key);

		public bool Set(string key, LospValue value) => ScriptObj.Set(key, value);

		public Dictionary<string, TValue> ToDictionary<TValue>(Func<KeyValuePair<string, LospValue>, TValue> toValue)
			=> ScriptObj.ToDictionary(toValue);

		public bool TryClear(string key) => ScriptObj.TryClear(key);

		public bool TryKey(string key, [NotNullWhen(true)] out LospValue? value) => ScriptObj.TryKey(key, out value);
	}
}
