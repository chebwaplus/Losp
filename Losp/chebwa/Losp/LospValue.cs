using System.Diagnostics.CodeAnalysis;

namespace chebwa.LospNet
{
	public abstract record LospValue
	{
		public object? BoxedValue { get; init; }
		internal LospValue(object? boxedValue)
		{
			BoxedValue = boxedValue;
		}

		public abstract bool MatchesType(LospValue other);

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
		public virtual bool TryGetValue<T>([NotNullWhen(true)] out T value) where T : struct
		{
			if (this is LospValue<T> lospVal)
			{
				value = lospVal.Value;
				return true;
			}

			value = default;
			return false;
		}
		public virtual bool TryGetRef<T>([MaybeNullWhen(true)] out T? value) where T : class
		{
			if (BoxedValue is T val)
			{
				value = val;
				return true;
			}

			value = default;
			return false;
		}
		public virtual bool TryGetObjectLiteral([NotNullWhen(true)] out LospObjectLiteral? literal)
		{
			literal = null;
			return false;
		}
		public virtual bool TryGetNonNull<T>([NotNullWhen(true)] out T? value) where T : class
		{
			if (this is LospValue<T> lospVal)
			{
				value = lospVal.Value;
				return value != null;
			}

			if (BoxedValue is T val && val != null)
			{
				value = val;
				return true;
			}

			value = null;
			return false;
		}

		public static implicit operator LospValue(int value) => new LospInt(value);
		public static implicit operator LospValue(float value) => new LospFloat(value);
		public static implicit operator LospValue(bool value) => new LospBool(value);
		public static implicit operator LospValue(string value) => new LospString(value);

		public static LospValue Convert(object? value)
		{
			if (value == null)
			{
				return new LospNull();
			}

			if (value.GetType().IsValueType)
			{
				return value switch
				{
					char c => new LospString(c.ToString()),
					int i => new LospInt(i),
					uint u => new LospInt((int)u),
					float f => new LospFloat(f),
					double d => new LospFloat((float)d),
					bool b => new LospBool(b),
					_ => throw new ArgumentException($"cannot convert value {value} (type {value.GetType()})"),
				};
			}

			return value switch
			{
				string s => new LospString(s),
				IEnumerable<LospValue> list => new LospList(list),
				LospLambda f => new LospFunc(f),
				IScriptObject p => new LospScriptable(p),
				LospValue v => v,
				_ => throw new ArgumentException($"cannot convert type {value} (type {value.GetType()})"),
			};
		}

		public static LospExtrinsic<T> Extrinsic<T>(T value)
		{
			return new(value);
		}
	}

	public abstract record LospValue<T> : LospValue
	{
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
			return other is LospNull;
		}

		public override bool TryGetValue<T>([NotNullWhen(true)] out T value) where T : struct
		{
			value = default;
			return false;
		}
		public override bool TryGetRef<T>([NotNullWhen(true)] out T? value) where T : class
		{
			value = default;
			return false;
		}
		public override bool TryGetNonNull<T>([NotNullWhen(true)] out T? value) where T : class
		{
			value = null;
			return false;
		}

		public override string ToString()
		{
			return "LostValue null";
		}
	}

	/// <summary>
	/// For types not natively supported by Losp.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="Value"></param>
	public record LospExtrinsic<T>(T Value) : LospValue<T>(Value)
	{
		public override string ToString()
		{
			return $"LostValue<{typeof(T).Name}> (extrinsic) {Value}";
		}
	}
}
