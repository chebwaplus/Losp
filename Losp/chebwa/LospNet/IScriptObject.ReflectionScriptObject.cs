// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;

namespace chebwa.LospNet
{
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

		private IEnumerable<string>? _cachedKeys;

		public IEnumerable<string> Keys
		{
			get
			{
				_cachedKeys ??= GetKeys();
				return _cachedKeys;
			}
		}

		private IEnumerable<string> GetKeys()
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

		public LospValue? Get(string key)
		{
			TryKey(key, out var value);
			return value;
		}

		public bool Set(string key, LospValue value)
		{
			var propInfo = TType.GetProperty(key);
			if (propInfo != null)
			{
				propInfo.SetValue(this, value);
				return true;
			}

			var fieldInfo = TType.GetField(key);
			if (fieldInfo != null)
			{
				fieldInfo.SetValue(this, value);
				return true;
			}

			return false;
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
