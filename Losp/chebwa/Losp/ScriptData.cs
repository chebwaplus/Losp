using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace chebwa.LospNet
{
	public enum ScriptDataType
	{
		Int,
		Float,
		Bool,
		String,
		List,
		Func,
		Object,
		Extrinsic,
	}

	public readonly struct ScriptData : IEquatable<ScriptData>
	{
		public readonly ScriptDataType DataType;
		public readonly object? Value;

		private ScriptData(ScriptDataType type, object? value)
		{
			DataType = type;
			Value = value;
		}

		public static ScriptData FromJsonValue(JsonValue value)
		{
			switch (value.GetValueKind())
			{
				case JsonValueKind.Undefined:
				case JsonValueKind.Null:
					return new ScriptData();
				case JsonValueKind.String:
					return value.GetValue<string>();
				case JsonValueKind.Number:
					if (value.TryGetValue(out uint u))
					{
						return u;
					}
					else if (value.TryGetValue(out int i))
					{
						return i;
					}
					else if (value.TryGetValue(out double d))
					{
						return d;
					}
					else if (value.TryGetValue(out float f))
					{
						return f;
					}
					break;
				case JsonValueKind.True:
				case JsonValueKind.False:
					return value.GetValue<bool>();
			}

			throw new ArgumentException($"cannot convert type {value}");
		}

		public static ScriptData Convert(object? value)
		{
			if (value == null)
			{
				return new ScriptData();
			}

			if (value.GetType().IsValueType)
			{
				return value switch
				{
					char c => new ScriptData(c.ToString()),
					int i => new ScriptData(i),
					uint u => new ScriptData((int)u),
					float f => new ScriptData(f),
					double d => new ScriptData((float)d),
					bool b => new ScriptData(b),
					_ => throw new ArgumentException($"cannot convert value {value} (type {value.GetType()})"),
				};
			}

			return value switch
			{
				JsonValue v => FromJsonValue(v),
				string s => new ScriptData(s),
				IEnumerable<ScriptData> list => new ScriptData(list),
				LospLambda f => new ScriptData(f),
				IScriptObject p => new ScriptData(p),
				_ => throw new ArgumentException($"cannot convert type {value} (type {value.GetType()})"),
			};
		}

		#region int

		public ScriptData(int value) : this(ScriptDataType.Int, value) { }

		public bool TryGetInt(out int val)
		{
			if (DataType != ScriptDataType.Int)
			{
				val = default;
				return false;
			}

			val = (int)Value!;
			return true;
		}

		public static implicit operator ScriptData(int value) => new(value);
		public static implicit operator ScriptData(uint value) => new((int)value);

		#endregion int

		#region float

		public ScriptData(float value) : this(ScriptDataType.Float, value) { }

		public bool TryGetFloat(out float val)
		{
			if (DataType != ScriptDataType.Float)
			{
				val = default;
				return false;
			}

			val = (float)Value!;
			return true;
		}

		public static implicit operator ScriptData(float value) => new(value);
		public static implicit operator ScriptData(double value) => new((float)value);

		#endregion float

		#region bool

		public ScriptData(bool value) : this(ScriptDataType.Bool, value) { }

		public bool TryGetBool(out bool val)
		{
			if (DataType != ScriptDataType.Bool)
			{
				val = default;
				return false;
			}

			val = (bool)Value!;
			return true;
		}

		public static implicit operator ScriptData(bool value) => new(value);

		#endregion bool

		#region string

		public ScriptData(string value) : this(ScriptDataType.String, value) { }

		public bool TryGetString([MaybeNullWhen(true)] out string? val)
		{
			if (DataType != ScriptDataType.String)
			{
				val = null;
				return false;
			}

			val = Value as string;
			return true;
		}

		public bool TryGetNonNullString([NotNullWhen(true)] out string? val)
		{
			if (DataType != ScriptDataType.String)
			{
				val = null;
				return false;
			}

			val = Value as string;
			return Value != null;
		}

		public static implicit operator ScriptData(string value) => new(value);

		#endregion string

		#region object

		public ScriptData(IScriptObject provider)
		{
			DataType = ScriptDataType.Object;
			Value = provider;
		}

		public bool TryGetObject([MaybeNullWhen(true)] out IScriptObject? val)
		{
			if (DataType != ScriptDataType.Object)
			{
				val = null;
				return false;
			}

			val = Value as IScriptObject;
			return true;
		}
		public bool TryGetNonNullObject([NotNullWhen(true)] out IScriptObject? val)
		{
			if (DataType != ScriptDataType.Object)
			{
				val = null;
				return false;
			}

			val = Value as IScriptObject;
			return val != null;
		}

		#endregion object

		#region list

		public ScriptData(IEnumerable<ScriptData> value) : this(ScriptDataType.List, value) { }

		public IEnumerable<ScriptData> AsList()
		{
			if (TryGetNonNullList(out var list))
			{
				return list;
			}

			throw new Exception("TODO: not a list");
		}

		public bool TryGetList([MaybeNullWhen(true)] out IEnumerable<ScriptData>? val)
		{
			if (DataType != ScriptDataType.List)
			{
				val = null;
				return false;
			}

			val = Value as IEnumerable<ScriptData>;
			return true;
		}
		public bool TryGetNonNullList([NotNullWhen(true)] out IEnumerable<ScriptData>? val)
		{
			if (DataType != ScriptDataType.List)
			{
				val = null;
				return false;
			}

			val = Value as IEnumerable<ScriptData>;
			return Value != null;
		}

		#endregion list

		#region func

		public ScriptData(LospLambda value) : this(ScriptDataType.Func, value) { }

		public bool TryGetFunc([MaybeNullWhen(true)] out LospLambda? val)
		{
			if (DataType != ScriptDataType.Func)
			{
				val = default;
				return false;
			}

			val = Value as LospLambda;
			return true;
		}
		public bool TryGetNonNullFunc([NotNullWhen(true)] out LospLambda? val)
		{
			if (DataType != ScriptDataType.Func)
			{
				val = default;
				return false;
			}

			val = Value as LospLambda;
			return Value != null;
		}

		public static implicit operator ScriptData(LospLambda value) => new(value);

		#endregion func

		#region extrisic

		public static ScriptData FromExtrinsic<T>(T value)
		{
			return new ScriptData(ScriptDataType.Extrinsic, value);
		}

		public bool TryGetExtrinsic([MaybeNullWhen(true)] out object? value)
		{
			if (DataType != ScriptDataType.Extrinsic)
			{
				value = default;
				return false;
			}

			value = Value;
			return true;
		}
		public bool TryGetNonNullExtrinsic([NotNullWhen(true)] out object? value)
		{
			if (DataType != ScriptDataType.Extrinsic)
			{
				value = default;
				return false;
			}

			value = Value;
			return Value != null;
		}

		public bool TryGetExtrinsic<T>([MaybeNullWhen(true)] out T? value)
		{
			if (DataType != ScriptDataType.Extrinsic)
			{
				value = default;
				return false;
			}

			if (Value is not T val)
			{
				value = default;
				return false;
			}

			value = val;
			return true;
		}
		public bool TryGetNonNullExtrinsic<T>([NotNullWhen(true)] out T? value)
		{
			if (DataType != ScriptDataType.Extrinsic)
			{
				value = default;
				return false;
			}

			if (Value is not T val)
			{
				value = default;
				return false;
			}

			value = val;
			return val != null;
		}

		#endregion extrinsic

		#region typed

		/// <summary>
		/// <para>
		/// A unified way to attempt to retrieve a value of type <typeparamref name="T"/>
		/// either from a typed <see cref="IScriptObject{T}"/> (where <see cref="DataType"/>
		/// is <see cref="ScriptDataType.Object"/>) or from a host-defined type (where
		/// <see cref="DataType"/> is <see cref="ScriptDataType.Extrinsic"/>).
		/// </para>
		/// <para>
		/// Note that this method will still fail if the <see cref="Value"/> is one of
		/// the supported data types (in the <see cref="ScriptDataType"/> enum) and
		/// the <see cref="DataType"/> is not one of the values mentioned above.
		/// For example, if <see cref="DataType"/> is <see cref="ScriptDataType.Int"/>,
		/// <c>TryGetType&lt;int&gt;(out int? val)</c> will fail.
		/// </para>
		/// </summary>
		/// <typeparam name="T">The type of class or struct to be retrieved.</typeparam>
		/// <param name="value">The retrieve value, if present.</param>
		/// <returns>A value indicating if the value of type <typeparamref name="T"/> was found.</returns>
		public bool TryGetType<T>([MaybeNullWhen(true)] out T? value)
		{
			if (TryGetObject(out var obj))
			{
				if (obj is IScriptObject<T> soT)
				{
					value = soT.TypedObject;
					return true;
				}
			}
			else if (TryGetExtrinsic<T>(out var extrinsic))
			{
				value = extrinsic;
				return true;
			}

			value = default;
			return false;
		}
		/// <summary>
		/// See <see cref="TryGetType{T}(out T)"/>.
		/// </summary>
		public bool TryGetNonNullType<T>([NotNullWhen(true)] out T? value)
		{
			if (TryGetObject(out var obj))
			{
				if (obj is IScriptObject<T> soT)
				{
					value = soT.TypedObject;
					return value != null;
				}
			}
			else if (TryGetExtrinsic<T>(out var extrinsic))
			{
				value = extrinsic;
				return value != null;
			}

			value = default;
			return false;
		}

		#endregion typed

		public override string ToString()
		{
			if (DataType == ScriptDataType.List)
			{
				TryGetList(out var list);
				if (list != null)
				{
					return $"ScriptData [{string.Join(", ", list.Select(v => v.ToString()))}]";
				}
			}
			else if (DataType == ScriptDataType.Func)
			{
				return "ScriptData (function type)";
			}
			
			// not using `else if` in case list is null for some reason
			if (Value == null)
			{
				return "ScriptData (null)";
			}

			return $"ScriptData ({Value})";
		}

		public override bool Equals(object? obj)
		{
			if (obj is ScriptData other)
			{
				return Equals(other);
			}
			return obj == Value;
		}

		public bool Equals(ScriptData other)
		{
			if (DataType != other.DataType) return false;
			if (Value == null)
			{
				return other.Value == null;
			}
			return Value.Equals(other.Value);
		}

		public static bool operator ==(ScriptData left, ScriptData right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ScriptData left, ScriptData right)
		{
			return !(left == right);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(DataType, Value);
		}
	}
}
