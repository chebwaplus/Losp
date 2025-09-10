// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;

namespace chebwa.LospNet
{
	/// <summary>
	/// The abstract base class from which all <see cref="LospValue"/> types are derived.
	/// Only the provided <see cref="LospValue"/> subclasses can be used as possible
	/// <see cref="LospValue"/> instances. To use a type not natively supported by Losp,
	/// use <see cref="Extrinsic{T}(T)"/> to create a <see cref="LospExtrinsic{T}"/>
	/// instance.
	/// </summary>
	public abstract record LospValue
	{
		/// <summary>
		/// The untyped value wrapped by this <see cref="LospValue"/> instance.
		/// </summary>
		public object? BoxedValue { get; init; }
		internal LospValue(object? boxedValue)
		{
			BoxedValue = boxedValue;
		}

		/// <summary>
		/// Determines if the <paramref name="other"/> <see cref="LospValue"/> is
		/// of the same type as this instance. This does not handle cases of
		/// polymorphism, for example comparing a <c>LospValue&lt;BaseClass&gt;</c> to a
		/// <c>LospValue&lt;Subclass&gt;</c>.
		/// </summary>
		/// <param name="other">The other <see cref="LospValue"/> for comparison.</param>
		/// <returns>A value indicating whether the two instances having matching types.</returns>
		public abstract bool MatchesType(LospValue other);

		/// <summary>
		/// Attempts to retrieve a value of type <typeparamref name="T"/>. If the
		/// underlying value is of type <typeparamref name="T"/>, the value is provided
		/// and <see langword="true"/> is returned; otherwise, <see langword="false"/>
		/// is returned.
		/// </summary>
		/// <typeparam name="T">The desired value type.</typeparam>
		/// <param name="value">The value to be provided, if present.</param>
		public virtual bool TryGet<T>([MaybeNullWhen(true)] out T? value)
		{
			if (BoxedValue is T val)
			{
				value = val;
				return true;
			}

			value = default;
			return false;
		}
		public virtual bool TryGetNonNull<T>([NotNullWhen(true)] out T? value) where T : class
		{
			if (BoxedValue != null && BoxedValue is T val)
			{
				value = val;
				return true;
			}

			value = null;
			return false;
		}
		/// <summary>
		/// Attempts to retrieve a <see cref="LospObjectLiteral"/>. If the
		/// underlying value is a <see cref="LospObjectLiteral"/>, the value is provided
		/// and <see langword="true"/> is returned; otherwise, <see langword="false"/>
		/// is returned.
		/// </summary>
		/// <param name="value">The underlying <see cref="LospObjectLiteral"/>, if present.</param>
		public virtual bool TryGetObjectLiteral([NotNullWhen(true)] out LospObjectLiteral? literal)
		{
			// note LospScriptable provides the only override that may return an actual value
			literal = null;
			return false;
		}

		public static implicit operator LospValue(int value) => new LospInt(value);
		public static implicit operator LospValue(float value) => new LospFloat(value);
		public static implicit operator LospValue(bool value) => new LospBool(value);
		public static implicit operator LospValue(string value) => new LospString(value);

		/// <summary>
		/// Compares the type of <paramref name="value"/> to known and supported
		/// <see cref="LospValue"/> types. Certain types without matching native
		/// <see cref="LospValue"/> types (<see langword="char"/>, <see langword="uint"/>,
		/// and <see langword="double"/>) are converted to native types
		/// (<see langword="string"/>, <see langword="int"/>, and <see langword="float"/>).
		/// An exception is thrown if the type is not supported. If supported, the
		/// value is wrapped in the appropriate <see cref="LospValue"/> type. For
		/// example, an <see langword="int"/> is wrapped in a <see cref="LospInt"/>. If
		/// <paramref name="value"/> is a <see cref="LospValue"/>, it is returned as-is.
		/// </summary>
		/// <param name="value">The value to wrap into a <see cref="LospValue"/>.</param>
		/// <returns>The <paramref name="value"/> wrapped in a <see cref="LospValue"/>.</returns>
		/// <exception cref="ArgumentException"></exception>
		public static LospValue Convert(object? value)
		{
			return TryConvert(value)
				?? throw new ArgumentException($"cannot convert value {value} (type {value?.GetType()})");
		}
		/// <summary>
		/// <para>
		/// Behaves similarly to <see cref="Convert(object?)"/>, but instead of throwing
		/// an exception for unsupported types, it creates a <see cref="LospExtrinsic{T}"/>,
		/// which is only possible because we have the type information.
		/// </para>
		/// <para>
		/// This should only be used with generic types where the specific type is unknown.
		/// If the specific type is known, either use the appropriate <see cref="LospValue"/>
		/// directly or call <see cref="Extrinsic{T}(T)"/> for non-native types.
		/// </para>
		/// </summary>
		/// <typeparam name="T">The type of <paramref name="value"/>.</typeparam>
		/// <param name="value">The value to wrap into a <see cref="LospValue"/>.</param>
		public static LospValue Convert<T>(T? value)
		{
			// note that is `value` is null, TryConvert() would return a LospNull;
			//  therefore we can assert that it is non-null when creating the extrinsic
			return TryConvert(value) ?? new LospExtrinsic<T>(value!);
		}

		/// <summary>
		/// <para>
		/// Attempts to create a <see cref="LospExtrinsic{T}"/> to wrap the provided
		/// <paramref name="value"/>. If <typeparamref name="T"/> is natively supported
		/// by Losp, an exception is thrown. This is done to ensure all native types use
		/// their matching native <see cref="LospValue"/> types.
		/// </para>
		/// <para>
		/// Use <see cref="Convert{T}(T)"/> instead if a generic type may be either
		/// native or non-native.
		/// </para>
		/// </summary>
		/// <typeparam name="T">The non-native type.</typeparam>
		/// <param name="value">The value to wrap ito a <see cref="LospExtrinsic{T}"/>.</param>
		/// <exception cref="ArgumentException"><typeparamref name="T"/> was a native type.</exception>
		public static LospExtrinsic<T> Extrinsic<T>(T value)
		{
			return value switch
			{
				int or uint or
				float or double or
				bool or
				string or char or
				IEnumerable<LospValue> or
				LospLambda or
				IScriptObject or
				LospValue => throw new ArgumentException(
					$"type {typeof(T)} is natively supported by Losp; use Convert<T>() instead if the type may be native or non-native"),
				_ => new(value),
			};
		}

		/// <summary>
		/// Attempts to create a <see cref="LospValue"/> for natively supported types.
		/// If the type is unsupported, <see langword="null"/> is returned.
		/// </summary>
		/// <param name="value">The value to wrap into a <see cref="LospValue"/>.</param>
		/// <returns>A <see cref="LospValue"/> or <see langword="null"/>.</returns>
		internal static LospValue? TryConvert(object? value)
		{
			if (value == null)
			{
				return new LospNull();
			}

			return value switch
			{
				// value types
				char c => new LospString(c.ToString()),
				int i => new LospInt(i),
				uint u => new LospInt((int)u),
				float f => new LospFloat(f),
				double d => new LospFloat((float)d),
				bool b => new LospBool(b),
				// ref types (included boxed interfaces)
				string s => new LospString(s),
				IEnumerable<LospValue> list => new LospList(list),
				LospLambda f => new LospFunc(f),
				IScriptObject p => new LospScriptable(p),
				LospValue v => v,
				// unsupported
				_ => null,
			};
		}
	}

	/// <summary>
	/// The abstract base class froom which all typed <see cref="LospValue"/> types are
	/// derived. Only the provided <see cref="LospValue"/> subclasses can be used as
	/// possible <see cref="LospValue"/> instances. To use a type not natively supported
	/// by Losp, use <see cref="LospValue.Extrinsic{T}(T)"/> to create a
	/// <see cref="LospExtrinsic{T}"/> instance.
	/// </summary>
	/// <typeparam name="T">The type of the underlying value.</typeparam>
	public abstract record LospValue<T> : LospValue
	{
		/// <summary>
		/// The typed value wrapped by this <see cref="LospValue"/> instance.
		/// </summary>
		public T? Value { get; init; }

		internal LospValue(T? value) : base(value)
		{
			Value = value;
		}

		public override bool MatchesType(LospValue other)
		{
			return other is LospValue<T>;
		}
	}

	public sealed record LospInt(int Value) : LospValue<int>(Value)
	{
		public override string ToString()
		{
			return "LospValue<int> " + Value.ToString();
		}
	}
	public sealed record LospFloat(float Value) : LospValue<float>(Value)
	{
		public override string ToString()
		{
			return "LospValue<float> " + Value.ToString();
		}
	}
	public sealed record LospBool(bool Value) : LospValue<bool>(Value)
	{
		public override string ToString()
		{
			return "LospValue<bool> " + Value.ToString();
		}
	}

	public sealed record LospString(string Value) : LospValue<string>(Value)
	{
		public override string ToString()
		{
			return "LospValue<string> " + Value;
		}
	}
	public sealed record LospList(IEnumerable<LospValue> Value) : LospValue<IEnumerable<LospValue>>(Value ?? [])
	{
		public override string ToString()
		{
			return "IEnumerable<LospValue> " + string.Join(", ", (Value ?? []).Select(v => v.ToString()));
		}
	}
	public sealed record LospScriptable(IScriptObject Value) : LospValue<IScriptObject>(Value)
	{
		public override bool TryGetObjectLiteral([NotNullWhen(true)] out LospObjectLiteral? literal)
		{
			if (Value is LospObjectLiteral lit)
			{
				literal = lit;
				return true;
			}

			literal = null;
			return false;
		}
		public override string ToString()
		{
			return $"LospValue<{nameof(IScriptObject)}> {Value}";
		}
	}
	public sealed record LospFunc(LospLambda Value) : LospValue<LospLambda>(Value)
	{
		public override string ToString()
		{
			return $"LospValue<{nameof(LospLambda)}> {(Value == null ? "null" : "func")}";
		}
	}
	public sealed record LospNull() : LospValue<object>(null as object)
	{
		public override bool MatchesType(LospValue other)
		{
			// note we aren't checking if other.Value == null
			return other is LospNull;
		}

		public override bool TryGet<T>([MaybeNullWhen(true)] out T? value) where T : default
		{
			// no reason to do a type check
			value = default;
			return false;
		}
		public override bool TryGetNonNull<T>([NotNullWhen(true)] out T? value) where T : class
		{
			// no reason to do a type check
			value = null;
			return false;
		}

		public override string ToString()
		{
			return "LostValue null";
		}
	}

	/// <summary>
	/// For types not natively supported by Losp. Use <see cref="LospValue.Extrinsic{T}(T)"/>
	/// or <see cref="LospValue.Convert{T}(T)"/> to create an instance.
	/// </summary>
	/// <typeparam name="T">The non-native value type.</typeparam>
	public record LospExtrinsic<T> : LospValue<T>
	{
		internal LospExtrinsic(T value) : base(value) { }

		public override string ToString()
		{
			return $"LostValue<{typeof(T).Name}> (extrinsic) {Value}";
		}
	}
}
