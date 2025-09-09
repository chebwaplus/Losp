// SPDX-License-Identifier: MIT

namespace chebwa.LospNet
{
	public partial class LospRunner
	{
		public sealed class LospRunnerInternal()
		{
			public class StackFrame
			{
				public required LospNode Node;
				public LospNode[]? Children;
				public int ChildIndex = 0;
				public LospChildResultDataCollection ChildResults = new(true);
				/// <summary>
				/// Called by the evaluator process once all of the <see cref="Node"/>'s
				/// children have been evaluated and applied to the <see cref="Node"/>
				/// itself.
				/// </summary>
				public required Action<EvalResult> OnComplete;
				public required ScriptVarContext FrameVarsContext;
			}

			private readonly Stack<StackFrame> _stackFrames = [];
			private ScriptVarContext? _vars;
			private AsyncTopResult? _topAsyncProxy;

			private bool _hasRun = false;

			internal EvalResult Eval(LospNode node, ScriptVarContext vars)
			{
				if (_hasRun) throw new InvalidOperationException("Runner already running.");
				_hasRun = true;

				if (node == null) return new ErrorResult(null, "Losp node was null and count not be evaluated");

				return PushAndReturnTop(node, GetChildren(node), vars);
			}

			internal EvalResult Call(LospLambda func, IEnumerable<LospValue> args, ScriptVarContext vars)
			{
				if (_hasRun) throw new InvalidOperationException("Runner already running.");
				_hasRun = true;

				if (func == null) return new ErrorResult(null, "Losp lambda was null and could not be called");

				var argsList = args.ToList();

				var funcVars = new ScriptVarContext(vars);

				for (var i = 0; i < func.ParamNames.Count && i < argsList.Count; i++)
				{
					funcVars.SetVar(func.ParamNames[i], argsList[i]);
				}

				return PushAndReturnTop(new LospLambdaCallNode(func), [.. func.Children], funcVars);
			}

			private EvalResult PushAndReturnTop(LospNode node, LospNode[]? children, ScriptVarContext vars)
			{
				_vars = vars;

				_topAsyncProxy ??= new AsyncTopResult();
				EvalResult? topResult = null;

				// callback used for the top-most (or bottom-most, I guess) item in the
				//  stack; i.e. when this is called, the stack will be empty since it is
				//  popped before its OnComplete() is called. this is notable if the
				//  result is a PushResult, since we'll have to reuse this function
				//  (which is why its defined here and not inline).
				void TopLevelOnComplete(EvalResult result)
				{
					if (result is AsyncResult ar)
					{
						// AsyncResults should be handled before OnComplete()
						//  is called. an AsyncResult that emits another
						//  AsyncResult is considered an error (but that
						//  should be caught outside OnComplete() as well).
						result = new ErrorResult(node, "evaluation error: unexpected " + nameof(AsyncResult));
					}
					else if (result is PushResult pr)
					{
						// since we are pushing onto an empty stack at this point,
						//  this effectively mirrors the initial stack push.
						_stackFrames.Push(new()
						{
							Node = new LospOperatorPushNode(pr),
							Children = [.. pr.Nodes],
							FrameVarsContext = _vars,
							OnComplete = TopLevelOnComplete,
						});
					}
					else if (topResult == null)
					{
						// if still null, the result was returned immediately,
						//  i.e. there were no async operations to delay the result

						topResult = result;
					}
					else
					{
						// if not null, we had to wait for an async operation.
						//  pass along the result and complete the eval

						_topAsyncProxy!.Complete(result);
					}
				}

				_stackFrames.Push(new()
				{
					Node = node,
					Children = children,
					FrameVarsContext = _vars,
					OnComplete = TopLevelOnComplete,
				});

				EvalStack();

				// if not null, that means the eval completed immediately with no
				//  async operations 
				topResult ??= new AsyncResult(_topAsyncProxy);

				return topResult;
			}

			private void EvalStack()
			{
				while (_stackFrames.Count > 0)
				{
					var top = _stackFrames.Pop()!;

					/*****************************************************
					 *****************************************************
					 * first phase: step through each child (if any) of the top-level
					 * node and evaluate it.
					 *****************************************************
					 *****************************************************/
					if (top.Children != null && top.ChildIndex < top.Children.Length)
					{
						var child = top.Children[top.ChildIndex];

						void CurrentTopOnComplete(EvalResult result)
						{
							if (result is AsyncResult ar)
							{
								// AsyncResults should be handled before OnComplete()
								//  is called. an AsyncResult that emits another
								//  AsyncResult is considered an error (but that
								//  should be caught outside OnComplete() as well).
								result = new ErrorResult(child, "evaluation error: unexpected " + nameof(AsyncResult));
							}
							else if (result is PushResult pr)
							{
								_stackFrames.Push(new()
								{
									Node = new LospOperatorPushNode(pr),
									Children = [.. pr.Nodes],
									FrameVarsContext = top.FrameVarsContext,
									OnComplete = CurrentTopOnComplete,
								});
							}
							else if (result is ValueResult vr)
							{
								top.ChildIndex++;

								foreach (var val in vr.Values)
								{
									top.ChildResults.Add(val, vr.Key);
								}
							}
							else if (result is ErrorResult er)
							{
								// ensure we stop processing siblings
								top.ChildIndex = top.Children.Length;
								top.ChildResults.Error = er;
							}
						}

						_stackFrames.Push(top);
						_stackFrames.Push(new()
						{
							Node = child,
							Children = GetChildren(child),
							FrameVarsContext = top.FrameVarsContext,
							OnComplete = CurrentTopOnComplete,
						});
					}
					/*****************************************************
					 *****************************************************
					 * second phase: all children have been evaluated.
					 * now evaluate the top-level node itself.
					 * 
					 * first, check if any errors need to be propagated upward.
					 *****************************************************
					 *****************************************************/
					//TODO: make an exception for LospOpPushNodes so that they can handle errors?
					// (most notably to allow for a CATCH() special op)
					else if (top.ChildResults.Error != null)
					{
						top.OnComplete(top.ChildResults.Error);
					}
					/*
					 * so errors, so continuing second phase.
					 */
					/***************************
					 * basic literals
					 ***************************/
					else if (top.Node is LospLiteralNode lit)
					{
						top.OnComplete(ValueResult.SingleOrNone(lit.Data));
					}
					/***************************
					 * object literals
					 ***************************/
					else if (top.Node is LospObjectLiteralNode objLitNode)
					{
						var obj = LospObjectLiteral.FromCollection(top.ChildResults);
						obj.Tags.AddRange(objLitNode.Tags);

						top.OnComplete(ValueResult.SingleOrNone(new LospScriptable(obj)));
					}
					/***************************
					 * indentifiers (vars)
					 ***************************/
					else if (top.Node is LospIdentifierNode id)
					{
						if (id.Name == ",")
						{
							//TODO: allow a warning setting and indicate the errant comma
							top.OnComplete(ValueResult.None());
						}

						if (top.FrameVarsContext.TryGetVar(id.Name, out var val))
						{
							top.OnComplete(ValueResult.SingleOrNone(val));
						}
						else
						{
							top.OnComplete(ErrorResultHelper.IdNotFound(id));
						}
					}
					/***************************
					 * lists
					 ***************************/
					else if (top.Node is LospListNode list)
					{
						top.OnComplete(ValueResult.SingleOrNone(new LospList(top.ChildResults)));
					}
					/***************************
					 * key/values
					 ***************************/
					else if (top.Node is LospKeyValueNode kv)
					{
						if (top.ChildResults.Count == 0)
						{
							top.OnComplete(ValueResult.SingleOrNone(new LospBool(true), kv.NodeId));
						}
						else
						{
							if (top.ChildResults.Keys.Any())
							{
								var objLit = LospObjectLiteral.FromCollection(top.ChildResults);
								objLit.Tags.AddRange(kv.Tags);

								top.OnComplete(ValueResult.SingleOrNone(new LospScriptable(objLit), kv.NodeId));
							}
							else
							{
								if (top.ChildResults.Count == 1)
								{
									top.OnComplete(ValueResult.SingleOrNone(top.ChildResults[0], kv.NodeId));
								}
								else
								{
									top.OnComplete(ValueResult.SingleOrNone(new LospList(top.ChildResults), kv.NodeId));
								}
							}
						}
					}
					/***************************
					 * lambdas
					 ***************************/
					else if (top.Node is LospFunctionNode funcNode)
					{
						top.OnComplete(ValueResult.SingleOrNone(new LospFunc(LospLambda.FromNode(funcNode))));
					}
					/***************************
					 * operators
					 ***************************/
					else if (top.Node is LospOperatorNode op)
					{
						// for an operator, we first want to see if the node id is a
						//  variable whose value is a lambda
						if (top.FrameVarsContext.TryGetVar(op.NodeId, out var @var) && @var is LospFunc func && func.Value != null)
						{
							var funcVars = new ScriptVarContext(top.FrameVarsContext);

							// assign arguments to the parameters
							for (var i = 0; i < func.Value.ParamNames.Count && i < top.ChildResults.Count; i++)
							{
								funcVars.SetVar(func.Value.ParamNames[i], top.ChildResults[i]);
							}

							_stackFrames.Push(new()
							{
								Node = new LospLambdaCallNode(func.Value),
								Children = [.. func.Value.Children],
								FrameVarsContext = funcVars,
								OnComplete = top.OnComplete,
							});
						}
						// if we didn't find a lambda, we next want to do a normal
						//  operator lookup
						else if (Losp.TryGetOperator(op.NodeId, out var scriptOp))
						{
							EvalResult opResult;
							try
							{
								opResult = scriptOp.Run(top.FrameVarsContext, op, top.ChildResults);
							}
							catch (Exception ex)
							{
								opResult = new ErrorResult(op, ex.Message);
							}

							if (opResult is AsyncResult ar)
							{
								ar.Source.OnAsyncCompleted((result) =>
								{
									if (result is AsyncResult)
									{
										result = new ErrorResult(op, "async processes cannot emit another async result");
									}

									top.OnComplete(result);
									EvalStack();
								});
								return;
							}
							else
							{
								top.OnComplete(opResult);
							}
						}
						else
						{
							top.OnComplete(new ErrorResult(op, "no operator found for name " + op.NodeId));
						}
					}
					/***************************
					 * push pseudo-nodes
					 ***************************/
					else if (top.Node is LospOperatorPushNode def)
					{
						var result = def.DeferredResult.OnComplete(top.ChildResults);

						if (result is AsyncResult ar)
						{
							ar.Source.OnAsyncCompleted((result) =>
							{
								if (result is AsyncResult)
								{
									result = new ErrorResult(def, "async processes cannot emit another async result");
								}

								top.OnComplete(result);
								EvalStack();
							});
							return;
						}
						else
						{
							top.OnComplete(result);
						}
					}
					/***************************
					 * lambda call pseudo-nodes
					 ***************************/
					// (lambda call pseudo-nodes are only created at eval time
					//  when a lambda node is encountered)
					else if (top.Node is LospLambdaCallNode funcCall)
					{
						top.OnComplete(ValueResult.MultipleOrNone(top.ChildResults));
					}
					/***************************
					 * unhandled
					 ***************************/
					else
					{
						//unknown/unhandled node type
						top.OnComplete(ValueResult.None());
					}
				}
			}

			private static LospNode[]? GetChildren(LospNode node)
			{
				if (node is LospFunctionNode)
				{
					return null;
				}

				if (node.Children != null)
				{
					return [.. node.Children];
				}

				return null;
			}

			private class LospLambdaCallNode(LospLambda func) : LospNode()
			{
				public override LospNodeType Type => LospNodeType.Function;
				public readonly LospLambda Func = func;
			}

			private class LospOperatorPushNode(PushResult result) : LospNode()
			{
				public override LospNodeType Type => LospNodeType.Operator;
				public readonly PushResult DeferredResult = result;
			}

			private class AsyncTopResult() : IAsyncProxy
			{
				private Action<EvalResult>? _callback;
				private EvalResult? _result;

				public void Complete(EvalResult result)
				{
					// can only be completed once
					if (_result != null) return;

					_result = result;
					_callback?.Invoke(result);
					_callback = null;
				}

				public void OnAsyncCompleted(Action<EvalResult> callback)
				{
					if (_result != null)
					{
						callback?.Invoke(_result);
					}
					else
					{
						_callback += callback;
					}
				}
			}
		}
	}
}
