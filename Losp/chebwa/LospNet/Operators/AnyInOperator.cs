// SPDX-License-Identifier: MIT

namespace chebwa.LospNet.Operators
{
	/// <summary>
	/// <para>
	/// <c>(ANY-IN [0 1 2] FN([n] (== n 1)))</c>
	/// </para>
	/// <para>
	/// Applies a lambda to each item in a list. The lambda must emit at least one
	/// value; only the first value emitted by the lambda is evaluated.
	/// If any one item evaluates as a true value (see
	/// <see cref="TruthinessOperator.GetTrue(LospValue)"/>, the <c>ANY-IN</c> operator
	/// emits a <see langword="true"/> value. Otherwise--if none of the items evaluate as
	/// true--the operator emits <see langword="false"/>. If the lambda returns an error,
	/// it is treated as a <see langword="false"/> result. Async lambdas are supported.
	/// </para>
	/// </summary>
	public class AnyInOperator() : IScriptOperator
	{
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			if (children.Unkeyed.Count != 2)
			{
				return ErrorResultHelper.NArguments(op, 2, exactly: true);
			}

			var collection = children.Unkeyed[0];
			var lambda = children.Unkeyed[1];

			if (collection is not LospList list)
			{
				return ErrorResultHelper.ArgNIsNotType(op, 0, "list", collection);
			}
			if (lambda is not LospFunc func)
			{
				return ErrorResultHelper.ArgNIsNotType(op, 1, "lambda", lambda);
			}

			var items = list.ToArray();
			var i = 0;

			/*
			 * we have to step through the list and apply the lambda to each item
			 * individually. this means we have to allow for errors and async each time.
			 * maybe there's a better (i.e. already supported) way to do this that
			 * I will realize later.
			 */

			EvalResult TryCurrItem()
			{
				/*
				 * no matching item found, so the operator failed
				 */
				if (i >= items.Length)
				{
					return ValueResult.SingleOrNone(false);
				}

				/*
				 * get the next item and apply the lambda
				 */
				var item = items[i++]; // incrementing here, for any future call to TryCurrItem()
				var result = Losp.Call(func.Value!, [item]);

				/*
				 * determine what to do next depending on the result of the lambda
				 */
				if (result is LospValueResult vr)
				{
					/*
					 * we'll require that the first item is the important one.
					 * if it's `true`, the operator succeeds and we're done.
					 */
					if (vr.TryIndex(0, out var val) && TruthinessOperator.GetTrue(val))
					{
						return ValueResult.SingleOrNone(true);
					}
					/*
					 * otherwise, we continue to the next item
					 */
				}
				else if (result is LospAsyncResult ar)
				{
					/*
					 * sigh. the tough case. it's async.
					 */

					var proxy = new AsyncProxy();

					//TODO: allow for timeout? (do we handle timeouts internally? I forget)
					/*
					 * we gotta go wait for the inner async result
					 */
					ar.OnAsyncCompleted(asyncResult =>
					{
						if (asyncResult is LospValueResult avr)
						{
							/*
							 * evaluate as if a VR, as earlier
							 */
							if (avr.TryIndex(0, out var val) && TruthinessOperator.GetTrue(val))
							{
								/*
								 * done
								 */
								proxy.Complete(ValueResult.SingleOrNone(true));
							}
							else
							{
								/*
								 * try next
								 */
								proxy.Complete(TryCurrItem());
							}
						}
						else
						{
							/*
							 * error; try next
							 */
							proxy.Complete(TryCurrItem());
						}
					});

					/*
					 * immediate result is an outer async
					 */
					return new AsyncResult(proxy);
				}
				/*
				 * note that if the result was an error, we continue to the next item.
				 */
				//TODO: should an error make the entire operator as an error?
				// some sort of option to determine the behavior?

				return TryCurrItem();
			}

			return TryCurrItem();
		}
	}
}
