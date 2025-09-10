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
	public record ValueResult : EvalResult
	{
		public string? Key { get; init; }
		public IEnumerable<LospValue> Values { get; init; }

		public LospValue? FirstOrDefault()
		{
			return Values.FirstOrDefault();
		}
		/// <summary>
		/// Attempts to retrieve the last value in the list of <see cref="Values"/>.
		/// When the return value is <see langword="true"/>, the <paramref name="last"/>
		/// value was retrieved successfully; otherwise, it was not retrieved.
		/// </summary>
		public bool TryGetLast([NotNullWhen(true)] out LospValue? last)
		{
			last = Values.LastOrDefault();
			return last != null;
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
	public record ErrorResult(LospNode? Source, string? Message) : EvalResult(ResultType.Error);
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
	public record AsyncResult(IAsyncProxy Source) : EvalResult(ResultType.Async)
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
	public record PushResult(IEnumerable<LospNode> Nodes, Func<LospChildResultDataCollection, EvalResult> OnComplete)
		: EvalResult(ResultType.Push);
}
