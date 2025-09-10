// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;

namespace chebwa.LospNet
{
	public partial class LospRunner
	{
		public readonly ScriptVarContext Vars = new();

		private readonly HashSet<LospRunnerInternal> _runningIntRunners = [];

		public bool TryGetVar(string varName, out LospValue value)
		{
			return Vars.TryGetVar(varName, out value!);
		}
		public void SetVar(string varName, LospValue value)
		{
			Vars.SetVar(varName, value);
		}

		public EvalResult Eval(LospNode node)
		{
			var internalRunner = new LospRunnerInternal();
			var result = internalRunner.Eval(node, new(Vars));
			if (result is AsyncResult ar)
			{
				_runningIntRunners.Add(internalRunner);
				ar.Source.OnAsyncCompleted(res =>
				{
					_runningIntRunners.Remove(internalRunner);
				});
			}
			return result;
		}

		public EvalResult Call(LospLambda func, IEnumerable<LospValue> args)
		{
			var internalRunner = new LospRunnerInternal();
			var result = internalRunner.Call(func, args, new(Vars));
			if (result is AsyncResult ar)
			{
				_runningIntRunners.Add(internalRunner);
				ar.Source.OnAsyncCompleted(res =>
				{
					_runningIntRunners.Remove(internalRunner);
				});
			}
			return result;
		}

		public class ScriptVarContext(ScriptVarContext? parent = null) : IScriptContext
		{
			private readonly ScriptVarContext? Parent = parent;
			// if parent is null, _vars is defined immediately; otherwise,
			//  _vars is only defined if SetVar() is called.
			private Dictionary<string, LospValue>? _vars = parent == null ? [] : null;

			public bool TryGetVar(string varName, [NotNullWhen(true)] out LospValue? value)
			{
				if (_vars != null && _vars.TryGetValue(varName, out value!))
				{
					return true;
				}
				if (Parent != null)
				{
					return Parent.TryGetVar(varName, out value);
				}

				value = null;
				return false;
			}
			public void SetVar(string varName, LospValue value)
			{
				_vars ??= [];
				_vars[varName] = value;
			}
		}
	}
}
