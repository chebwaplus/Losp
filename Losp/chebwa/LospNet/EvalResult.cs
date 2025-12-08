// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;

namespace chebwa.LospNet
{
	public interface IAsyncProxy
	{
		void OnAsyncCompleted(Action<EvalResult> callback);
	}

	public class AsyncProxy() : IAsyncProxy
	{
		private EvalResult? _result = null;
		private Action<EvalResult>? _callback;

		public void OnAsyncCompleted(Action<EvalResult> callback)
		{
			if (_result != null)
			{
				callback(ValueResult.None());
			}
			else
			{
				_callback += callback;
			}
		}

		public void Complete(EvalResult result)
		{
			// can only be completed once
			if (_result != null) return;

			_result = result;
			_callback?.Invoke(result);
			_callback = null;
		}
	}

	public enum ResultType
	{
		/// <summary>
		/// A result type indicating that an error occurred, as well as a string
		/// describing the error. See <see cref="ErrorResult"/>.
		/// </summary>
		Error,
		/// <summary>
		/// A result type indicating that an operation was successful, but that no values
		/// were emitted. See <see cref="ValueResult"/>, specifically
		/// <see cref="ValueResult.None"/>.
		/// </summary>
		SuccessNoEmit,
		/// <summary>
		/// A result type indicating that an operation was successful, and that one or
		/// more values were emitted. See <see cref="ValueResult"/>, specifically
		/// <see cref="ValueResult.SingleOrNone(LospValue, string?)"/> and
		/// <see cref="ValueResult.MultipleOrNone(IEnumerable{LospValue}, string?)"/>.
		/// </summary>
		SuccessEmit,
		/// <summary>
		/// A result type that indicates the result will not be provided immediately,
		/// and is pending some asynchronous process. The result also provides a
		/// callback which will be triggered when the process has completed. See
		/// <see cref="AsyncResult"/>.
		/// </summary>
		Async,
		/// <summary>
		/// A result type that indicates further evaluation is required, and which
		/// provides the <see cref="LospNode"/>s to be evaluated. See
		/// <see cref="PushResult"/>.
		/// </summary>
		Push,
	}

	/// <summary>
	/// The basis of several result types that can be returned when evaluating a
	/// <see cref="LospNode"/>.
	/// </summary>
	/// <param name="Type"></param>
	public abstract record EvalResult(ResultType Type);
	/// <summary>
	/// A result type that indicates successful evaluation of an expression.
	/// The result may include zero or more values, depending on the expression
	/// type and its evaluation. If no values were emitted, the result's
	/// <see cref="EvalResult.Type"/> will be <see cref="ResultType.SuccessNoEmit"/>;
	/// otherwise, it will be <see cref="ResultType.SuccessEmit"/> and its
	/// <see cref="Values"/> will contain at least one item.
	/// </summary>
	public sealed record ValueResult : EvalResult
	{
		/// <summary>
		/// The key name associated with the <see cref="Values"/> of this result, if any.
		/// </summary>
		public string? Key { get; init; }
		/// <summary>
		/// T
		/// </summary>
		public IEnumerable<LospValue> Values { get; init; }

		private LospValue[]? _cachedArray;
		private LospValue[] GetCachedArray()
		{
			return _cachedArray ??= [.. Values];
		}

		//TODO: standardize the Try..Of() and Try..As() naming conventions

		/// <summary>
		/// Attempts to retrieve the <see cref="LospValue"/> in the collection of
		/// all <see cref="Values"/> at the given <paramref name="index"/>.
		/// </summary>
		/// <param name="index">The index of the desired <see cref="LospValue"/>.</param>
		/// <param name="value">The retrieved <see cref="LospValue"/>, if successful.</param>
		/// <returns>A value indicating whether the <paramref name="value"/> was
		/// successfully retrieved.</returns>
		public bool TryIndex(int index, [NotNullWhen(true)] out LospValue? value)
		{
			var ar = GetCachedArray();
			if (ar.Length < index)
			{
				value = null;
				return false;
			}

			value = ar[index];
			return true;
		}
		/// <summary>
		/// Attempts to retrieve a value of type <typeparamref name="T"/> from the
		/// <see cref="LospValue"/> at the given <paramref name="index"/>. (See also
		/// <see cref="TryIndex(int, out LospValue?)"/>.) If no <see cref="LospValue"/>
		/// is retrieved, or it does not store a value of type <typeparamref name="T"/>,
		/// the method fails.
		/// </summary>
		/// <typeparam name="T">The desired type.</typeparam>
		/// <param name="index">The index of the desired <see cref="LospValue"/>.</param>
		/// <param name="value">The retrieved <typeparamref name="T"/>, if successful.</param>
		/// <returns>A value indicating whether the <paramref name="value"/> was
		/// successfully retrieved.</returns>
		public bool TryIndexAs<T>(int index, [NotNullWhen(true)] out T? value)
		{
			if (TryIndex(index, out var val) && val.BoxedValue is T v)
			{
				value = v;
				return true;
			}

			value = default;
			return false;
		}

		/// <summary>
		/// Returns the first value in the list of <see cref="Values"/>,
		/// if there are more than zero. Otherwise, returns <see langword="null"/>.
		/// </summary>
		public LospValue? FirstOrDefault()
		{
			return Values.FirstOrDefault();
		}
		/// <summary>
		/// Attempts to retrieve the last value in the list of <see cref="Values"/>.
		/// When the return value is <see langword="true"/>, the <paramref name="last"/>
		/// value was retrieved successfully; otherwise, it was not retrieved.
		/// </summary>
		public bool TryLast([NotNullWhen(true)] out LospValue? last)
		{
			last = Values.LastOrDefault();
			return last != null;
		}
		/// <summary>
		/// Attempts to retrieve the last value in the list of <see cref="Values"/>
		/// and extract its inner value as the desired type, <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The expected type of the last emitted value.</typeparam>
		/// <param name="last">The resulting value, if successful.</param>
		/// <returns>A value indicating whether the value was successfully retrieved.</returns>
		public bool TryLastAs<T>(out T? last)
		{
			if (TryLast(out var value))
			{
				return value.TryGet(out last);
			}

			last = default;
			return false;
		}
		/// <summary>
		/// Returns the last value in the list of <see cref="Values"/>,
		/// if there are more than zero. Otherwise, returns <see langword="null"/>.
		/// </summary>
		public LospValue? LastOrDefault()
		{
			return Values.LastOrDefault();
		}

		/// <summary>
		/// Attempts to interpret the <see cref="Values"/> as a tuple of specific types.
		/// If the elements of the <see cref="Values"/> match the desired types, the
		/// resulting <see cref="ValueTuple{T1, T2}"/> is assigned to
		/// <paramref name="tuple"/>.
		/// </summary>
		/// <typeparam name="T1">The first element type.</typeparam>
		/// <typeparam name="T2">The second element type.</typeparam>
		/// <param name="tuple">The result tuple, if successful.</param>
		/// <returns>A value indicating whether a tuple of the desired types was created.</returns>
		public bool TryAsTuple<T1, T2>([NotNullWhen(true)] out (T1, T2)? tuple)
		{
			var ar = GetCachedArray();
			if (ar.Length >= 2 && ar[0].BoxedValue is T1 v1 && ar[1].BoxedValue is T2 v2)
			{
				tuple = (v1, v2);
				return true;
			}

			tuple = null;
			return false;
		}

		/// <summary>
		/// Attempts to interpret the <see cref="Values"/> as a tuple of specific types.
		/// If the elements of the <see cref="Values"/> match the desired types, the
		/// resulting <see cref="ValueTuple{T1, T2, T3}"/> is assigned to
		/// <paramref name="tuple"/>.
		/// </summary>
		/// <typeparam name="T1">The first element type.</typeparam>
		/// <typeparam name="T2">The second element type.</typeparam>
		/// <typeparam name="T3">The third element type.</typeparam>
		/// <param name="tuple">The result tuple, if successful.</param>
		/// <returns>A value indicating whether a tuple of the desired types was created.</returns>
		public bool TryAsTuple<T1, T2, T3>([NotNullWhen(true)] out (T1, T2, T3)? tuple)
		{
			var ar = GetCachedArray();
			if (ar.Length >= 3
				&& ar[0].BoxedValue is T1 v1
				&& ar[1].BoxedValue is T2 v2
				&& ar[2].BoxedValue is T3 v3
				)
			{
				tuple = (v1, v2, v3);
				return true;
			}

			tuple = null;
			return false;
		}

		/// <summary>
		/// Attempts to interpret the <see cref="Values"/> as a tuple of specific types.
		/// If the elements of the <see cref="Values"/> match the desired types, the
		/// resulting <see cref="ValueTuple{T1, T2, T3, T4}"/> is assigned to
		/// <paramref name="tuple"/>.
		/// </summary>
		/// <typeparam name="T1">The first element type.</typeparam>
		/// <typeparam name="T2">The second element type.</typeparam>
		/// <typeparam name="T3">The third element type.</typeparam>
		/// <typeparam name="T4">The fourth element type.</typeparam>
		/// <param name="tuple">The result tuple, if successful.</param>
		/// <returns>A value indicating whether a tuple of the desired types was created.</returns>
		public bool TryAsTuple<T1, T2, T3, T4>([NotNullWhen(true)] out (T1, T2, T3, T4)? tuple)
		{
			var ar = GetCachedArray();
			if (ar.Length >= 4
				&& ar[0].BoxedValue is T1 v1
				&& ar[1].BoxedValue is T2 v2
				&& ar[2].BoxedValue is T3 v3
				&& ar[3].BoxedValue is T4 v4
				)
			{
				tuple = (v1, v2, v3, v4);
				return true;
			}

			tuple = null;
			return false;
		}

		/// <summary>
		/// <para>
		/// Attempts to cast all values in <see cref="Values"/> to <typeparamref name="T"/>
		/// and provide the resulting array. All values must be castable to <typeparamref name="T"/>
		/// for the method to succeed. Use when you assume and require that all returned
		/// values must conform to a certain type.
		/// </para>
		/// <para>
		/// If you instead want to gather all return values of a certain type, see also
		/// <see cref="FilterAsList{T}"/>, which will create a list containing only values
		/// of the desired type.
		/// </para>
		/// </summary>
		/// <typeparam name="T">The desired type.</typeparam>
		/// <param name="list">The list </param>
		/// <returns>A value indicating whether the method was successful.</returns>
		public bool TryAsList<T>([NotNullWhen(true)] out List<T>? list)
		{
			var ar = GetCachedArray();
			for (var i = 0; i < ar.Length; i++)
			{
				if (ar[i].BoxedValue is not T)
				{
					list = null;
					return false;
				}
			}

			list = new(ar.Length);
			for (var i = 0; i < ar.Length; i++)
			{
				list.Add((T)ar[i].BoxedValue!);
			}

			return true;
		}

		/// <summary>
		/// <para>
		/// For each value in <see cref="Values"/>, if the value is castable to
		/// <typeparamref name="T"/>, it is added to a new list which is then returned.
		/// Therefore all values of type <typeparamref name="T"/>, and only those values,
		/// are included in the returned list.
		/// </para>
		/// <para>
		/// If you assume and require that all values must conform to type <typeparamref name="T"/>,
		/// see also <see cref="TryAsList{T}(out List{T}?)"/>.
		/// </para>
		/// </summary>
		/// <typeparam name="T">The desired type.</typeparam>
		/// <returns>A list that contains all values of type <typeparamref name="T"/>.</returns>
		public List<T> FilterAsList<T>()
		{
			var ar = GetCachedArray();
			var list = new List<T>();

			for (var i = 0; i < ar.Length; i++)
			{
				if (ar[i].BoxedValue is T v)
				{
					list.Add(v);
				}
			}

			return list;

		}

		/// <summary>
		/// Creates a <see cref="ValueResult"/> with zero or more <paramref name="values"/>.
		/// If <paramref name="values"/> contains at least one item, the result's
		/// <see cref="EvalResult.Type"/> is set to <see cref="ResultType.SuccessEmit"/>;
		/// otherwise, it is set to <see cref="ResultType.SuccessNoEmit"/>.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="key"></param>
		private ValueResult(IEnumerable<LospValue> values, string? key = null)
			: base(values.Any() ? ResultType.SuccessEmit : ResultType.SuccessNoEmit)
		{
			Key = key;
			Values = values;
		}

		/// <summary>
		/// Creates a <see cref="ValueResult"/> that passes no values to the calling or
		/// parent context, creating a <see cref="ResultType.SuccessNoEmit"/> result.
		/// </summary>
		public static ValueResult None()
		{
			return new ValueResult([]);
		}
		/// <summary>
		/// Creates a <see cref="ValueResult"/> that passes one value to the calling or
		/// parent context, creating a <see cref="ResultType.SuccessEmit"/> result. If
		/// the <see cref="LospValue"/> is <see langword="null"/> (that is, there is
		/// no <see cref="LospValue"/> because it is missing or some error occurred),
		/// then no values are passed up and a <see cref="ResultType.SuccessNoEmit"/>
		/// result is created instead.
		/// </summary>
		/// <param name="value">The value to up pass to the context.</param>
		/// <param name="key">A key name for the value; the use of the key is
		/// context-dependent.</param>
		/// <returns></returns>
		public static ValueResult SingleOrNone(LospValue? value, string? key = null)
		{
			return new ValueResult(value == null ? [] : [value], key);
		}
		/// <summary>
		/// Creates a <see cref="ValueResult"/> that passes zero or more values to the
		/// calling or parent context, creating a <see cref="ResultType.SuccessEmit"/>
		/// result. If the list is empty or <see langword="null"/> (that is, there are
		/// no list because it is missing or some error occurred), then no values are
		/// passed up and a <see cref="ResultType.SuccessNoEmit"/> result is created
		/// instead.
		/// </summary>
		/// <param name="values">A list of zero or more values to the context.</param>
		/// <param name="key">A key name of the value; the use of the key is
		/// context-dependent and may be ignored for multiple values.</param>
		/// <returns></returns>
		public static ValueResult MultipleOrNone(IEnumerable<LospValue> values, string? key = null)
		{
			return new ValueResult(values ?? [], key);
		}
	}
	/// <summary>
	/// A result type indicating an error in evaluating an expression.
	/// </summary>
	/// <param name="Source">The <see cref="LospNode"/> involved with the error, if any.</param>
	/// <param name="Message">A message describing the error.</param>
	public sealed record ErrorResult(LospNode? Source, string? Message) : EvalResult(ResultType.Error);
	/// <summary>
	/// <para>
	/// A result type that indicates the expression cannot be evaluated immediately, i.e.
	/// it requires some asynchronous process before it can complete.
	/// </para>
	/// <para>
	/// When an <see cref="AsyncResult"/> is returned, its <see cref="Source"/> provides
	/// a callback hook (<see cref="IAsyncProxy.OnAsyncCompleted(Action{EvalResult})"/>)
	/// that should be used to receive the final evaluation result.
	/// <see cref="OnAsyncCompleted(Action{EvalResult})"/> can be called on the
	/// <see cref="AsyncResult"/> directly instead; it simply forwards the callback to
	/// the <see cref="Source"/>.
	/// </para>
	/// <para>
	/// In typical uses of Losp (calling <see cref="Losp.Eval(LospNode)"/> or
	/// <see cref="Losp.Call(LospLambda, IEnumerable{LospValue})"/>) a top-level
	/// <see cref="AsyncResult"/> only occurs once at most. If no internal expression
	/// returns an <see cref="AsyncResult"/>, the top-level evaluation will also not
	/// return one. But if any internal expression returns an <see cref="AsyncResult"/>
	/// then that result and all further asynchronous evaluations are wrapped up in a
	/// single top-level <see cref="AsyncResult"/>; the top-level <see cref="AsyncResult"/>
	/// can only be resolved with a <see cref="ValueResult"/> or an
	/// <see cref="ErrorResult"/>. (In other words, callbacks do not need to handle
	/// cases of recursive <see cref="AsyncResult"/>s.)
	/// </para>
	/// </summary>
	/// <param name="Source">The <see cref="IAsyncProxy"/> that can be used to handle
	/// the final evaluated result of the expression.</param>
	public sealed record AsyncResult(IAsyncProxy Source) : EvalResult(ResultType.Async)
	{
		/// <summary>
		/// Registers the <paramref name="callback"/> to be invoked once all asynchronous
		/// processes are completed. See the <see cref="AsyncResult"/> documentation for
		/// more details.
		/// </summary>
		/// <param name="callback">The delegate to call when a result is available.</param>
		public void OnAsyncCompleted(Action<EvalResult> callback)
		{
			Source.OnAsyncCompleted(callback);
		}
	}
	/// <summary>
	/// <para>
	/// A result type that indicates the expression requires nodes to be evaluated before
	/// evaluation of the expression can be completed. This is mostly an internal result
	/// type; end users of Losp should not see this result type in typical evaluation
	/// scenarios.
	/// </para>
	/// <para>
	/// Internally, the result pushes one or more <see cref="LospNode"/>s to queue them
	/// for evaluation. The results are passed to the <see cref="OnComplete"/> callback.
	/// </para>
	/// </summary>
	/// <param name="Nodes">The nodes to be evaluated.</param>
	/// <param name="OnComplete">The callback invoked once the nodes have been evaluated.</param>
	public sealed record PushResult(IEnumerable<LospNode> Nodes, Func<LospChildResultDataCollection, EvalResult> OnComplete)
		: EvalResult(ResultType.Push);

	public abstract record LospResult
	{
		public ResultType Type { get; init; }
		internal LospResult(ResultType type) { Type = type; }
	}
	public abstract record LospTerminalResult : LospResult
	{
		internal LospTerminalResult(ResultType Type) : base(Type) { }
	}
	/// <summary>
	/// A result type that indicates successful evaluation of an expression.
	/// The result may include zero or more values, depending on the expression
	/// type and its evaluation. If no values were emitted, the result's
	/// <see cref="EvalResult.Type"/> will be <see cref="ResultType.SuccessNoEmit"/>;
	/// otherwise, it will be <see cref="ResultType.SuccessEmit"/> and its
	/// <see cref="Values"/> will contain at least one item.
	/// </summary>
	public sealed record LospValueResult : LospTerminalResult
	{
		private readonly ValueResult _result;

		/// <summary>
		/// The key name associated with the <see cref="Values"/> of this result, if any.
		/// </summary>
		public string? Key => _result.Key;
		/// <summary>
		/// The zero or more <see cref="LospValue"/>s 
		/// </summary>
		public IEnumerable<LospValue> Values => _result.Values;

		/// <summary>
		/// Creates a <see cref="LospValueResult"/> with zero or more <see cref="Values"/>.
		/// All properties are derived from the underlying <paramref name="result"/>.
		/// </summary>
		/// <param name="result">The underlying <see cref="ValueResult"/>.</param>
		internal LospValueResult(ValueResult result)
			: base(result.Type)
		{
			_result = result;
		}

		/// <inheritdoc cref="ValueResult.TryIndex(int, out LospValue?)"/>
		public bool TryIndex(int index, [NotNullWhen(true)] out LospValue? value)
		{
			return _result.TryIndex(index, out value);
		}
		/// <inheritdoc cref="ValueResult.TryIndexAs{T}(int, out T)"/>
		public bool TryIndexAs<T>(int index, [NotNullWhen(true)] out T? value)
		{
			return _result.TryIndexAs(index, out value);
		}

		/// <inheritdoc cref="ValueResult.FirstOrDefault"/>
		public LospValue? FirstOrDefault()
		{
			return _result.FirstOrDefault();
		}
		/// <inheritdoc cref="ValueResult.TryLast(out LospValue?)"/>
		public bool TryLast([NotNullWhen(true)] out LospValue? last)
		{
			return _result.TryLast(out last);
		}
		/// <inheritdoc cref="ValueResult.TryLastAs{T}(out T)"/>
		public bool TryLastAs<T>(out T? last)
		{
			return _result.TryLastAs(out last);
		}
		/// <inheritdoc cref="ValueResult.LastOrDefault"/>
		public LospValue? LastOrDefault()
		{
			return _result.LastOrDefault();
		}
		/// <inheritdoc cref="ValueResult.TryAsTuple{T1, T2}(out ValueTuple{T1, T2}?)"/>
		public bool TryAsTuple<T1, T2>([NotNullWhen(true)] out (T1, T2)? tuple)
		{
			return _result.TryAsTuple(out tuple);
		}
		/// <inheritdoc cref="ValueResult.TryAsTuple{T1, T2, T3}(out ValueTuple{T1, T2, T3}?)"/>
		public bool TryAsTuple<T1, T2, T3>([NotNullWhen(true)] out (T1, T2, T3)? tuple)
		{
			return _result.TryAsTuple(out tuple);
		}
		/// <inheritdoc cref="ValueResult.TryAsTuple{T1, T2, T3, T4}(out ValueTuple{T1, T2, T3, T4}?)"/>
		public bool TryAsTuple<T1, T2, T3, T4>([NotNullWhen(true)] out (T1, T2, T3, T4)? tuple)
		{
			return _result.TryAsTuple(out tuple);
		}
		/// <inheritdoc cref="ValueResult.TryAsList{T}(out List{T}?)"/>
		public bool TryAsList<T>([NotNullWhen(true)] out List<T>? list)
		{
			return _result.TryAsList(out list);
		}
		/// <inheritdoc cref="ValueResult.FilterAsList{T}"/>
		public List<T> FilterAsList<T>()
		{
			return _result.FilterAsList<T>();
		}
	}
	/// <summary>
	/// A result type indicating an error in evaluating an expression.
	/// </summary>
	/// <param name="Source">The <see cref="LospNode"/> involved with the error, if any.</param>
	/// <param name="Message">A message describing the error.</param>
	public sealed record LospErrorResult : LospTerminalResult
	{
		/// <inheritdoc cref="ErrorResult.Source"/>
		public LospNode? Source { get; init; }
		/// <inheritdoc cref="ErrorResult.Message"/>
		public string? Message { get; init; }

		internal LospErrorResult(ErrorResult result) : base(result.Type)
		{
			Source = result.Source;
			Message = result.Message;
		}
	}
	/// <summary>
	/// <para>
	/// A result type that indicates the expression cannot be evaluated immediately, i.e.
	/// it requires some asynchronous process before it can complete.
	/// </para>
	/// <para>
	/// When an <see cref="AsyncResult"/> is returned, its <see cref="Source"/> provides
	/// a callback hook (<see cref="IAsyncProxy.OnAsyncCompleted(Action{EvalResult})"/>)
	/// that should be used to receive the final evaluation result.
	/// <see cref="OnAsyncCompleted(Action{LospTerminalResult})"/> can be called on the
	/// <see cref="AsyncResult"/> directly instead; it simply forwards the callback to
	/// the <see cref="Source"/>.
	/// </para>
	/// <para>
	/// In typical uses of Losp (calling <see cref="Losp.Eval(LospNode)"/> or
	/// <see cref="Losp.Call(LospLambda, IEnumerable{LospValue})"/>) a top-level
	/// <see cref="AsyncResult"/> only occurs once at most. If no internal expression
	/// returns an <see cref="AsyncResult"/>, the top-level evaluation will also not
	/// return one. But if any internal expression returns an <see cref="AsyncResult"/>
	/// then that result and all further asynchronous evaluations are wrapped up in a
	/// single top-level <see cref="AsyncResult"/>; the top-level <see cref="AsyncResult"/>
	/// can only be resolved with a <see cref="ValueResult"/> or an
	/// <see cref="ErrorResult"/>. (In other words, callbacks do not need to handle
	/// cases of recursive <see cref="AsyncResult"/>s.)
	/// </para>
	/// </summary>
	/// <param name="Source">The <see cref="IAsyncProxy"/> that can be used to handle
	/// the final evaluated result of the expression.</param>
	public sealed record LospAsyncResult : LospResult
	{
		private readonly AsyncResult _result;
		internal LospAsyncResult(AsyncResult result) : base(result.Type)
		{
			_result = result;
		}

		/// <summary>
		/// Registers the <paramref name="callback"/> to be invoked once all asynchronous
		/// processes are completed. See the <see cref="AsyncResult"/> documentation for
		/// more details.
		/// </summary>
		/// <param name="callback">The delegate to call when a result is available.</param>
		public void OnAsyncCompleted(Action<LospTerminalResult> callback)
		{
			_result.OnAsyncCompleted(result =>
			{
				LospTerminalResult lospResult = result switch
				{
					ValueResult vr => new LospValueResult(vr),
					ErrorResult er => new LospErrorResult(er),
					_ => new LospErrorResult(new(null, "unexpected result type: " + result.GetType().Name)),
				};

				callback(lospResult);
			});
		}
	}
}
