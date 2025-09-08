using System.Diagnostics.CodeAnalysis;

namespace chebwa.LospNet
{
	public interface IScriptOperator
	{
		EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children);
	}
	public interface ISpecialOperator : IScriptOperator
	{
		/// <summary>
		/// <para>
		/// The <see cref="Prepare(LospOperatorNode)"/> method of an
		/// <see cref="ISpecialOperator"/> is called after the operator's
		/// <see cref="LospOperatorNode"/> is finalized, with all its child nodes,
		/// during the AST building step, but before the node is added to the AST tree.
		/// </para>
		/// <para>
		/// This enables the <see cref="IScriptOperator"/> handler a) to ensure the
		/// node is well-formed and, if not, throw an exception; and b) to transform the
		/// input node as necessary into a resulting <see cref="LospSpecialOperatorNode"/>.
		/// The resulting <see cref="LospSpecialOperatorNode"/> is then added to the
		/// AST tree in place of the input <see cref="LospOperatorNode"/>.
		/// </para>
		/// <para>
		/// A special operator node has <see cref="LospSpecialOperatorNode.SpecialOperatorChildren"/>
		/// which are not evaluated by the Losp evaluator, but can be used as desired once
		/// <see cref="IScriptOperator.Run(IScriptContext, LospOperatorNode, LospChildResultDataCollection)"/>
		/// is invoked by the evaluator. An <see cref="ISpecialOperator"/> is free to
		/// inspect and handle the input node's children as appropriate, placing them into the
		/// output node's standard child list, its special operator child list, or neither.
		/// (Or both!) By using a <see cref="PushResult"/> during <c>Run()</c>, an
		/// <see cref="ISpecialOperator"/> may defer the evaluation of child nodes based
		/// on some criteria; for example, the <c>IF</c> handler defers the evaluation of
		/// the "then" child or "else" child, if present, based on the result of
		/// evaluating its conditions. During <c>Run()</c>, the <see cref="ISpecialOperator"/>
		/// will need to cast the <see cref="LospOperatorNode"/> parameter to a
		/// <see cref="LospSpecialOperatorNode"/> to access its special operator child list.
		/// </para>
		/// </summary>
		/// <param name="node">The input node of the special operator, as it was parsed
		/// from the source.</param>
		/// <returns>The prepared <see cref="LospSpecialOperatorNode"/>.</returns>
		LospSpecialOperatorNode Prepare(LospOperatorNode node);
	}

	public interface IScriptContext
	{
		bool TryGetVar(string varName, [NotNullWhen(true)] out LospValue? value);
		void SetVar(string varName, LospValue value);
	}

	public class Losp
	{
		// high-level process:
		//
		// see also the notes in Tokenizer and ASTBuilder.
		//
		// one potentially maddening point is that symbols/identifiers have three(!)
		// representations during the parse and run process. first, symbols are a type
		// of token; they are basically anything that isn't a specific other type of
		// token, e.g. names and some sigil or sigil combinations. when they go into the
		// AST builder, they are either consumed and removed (typically when they are
		// the names of operators or keys in kv pairs) or they become identifier nodes.
		// finally, when identifier nodes are passed to a runner, their treatment
		// depends on the context. *usually* each is used as a lookup to find whatever
		// value was assigned to it. in some cases (like the parameter list in a
		// function definition), they get consumed in some other way and are not
		// directly evaluated. when an identifier is used as a lookup, the value it
		// resolves to (like all values passed around in a runner) is a ScriptData
		// instance.
		//
		// so the representations are: Symbol token => IdentifierNode => ScriptData

		#region operators

		public static readonly Dictionary<string, IScriptOperator> Operators = [];

		public static void AddOperator(string operatorName, IScriptOperator op)
		{
			ArgumentException.ThrowIfNullOrEmpty(operatorName, nameof(operatorName));
			ArgumentNullException.ThrowIfNull(op, nameof(op));

			Operators[operatorName] = op;
		}

		public static bool TryGetSpecialOperator(string opName, [NotNullWhen(true)] out ISpecialOperator? op)
		{
			if (opName == null)
			{
				op = null;
				return false;
			}

			return LospInternalContext.SpecialOperators.TryGetValue(opName, out op);
		}

		public static bool TryGetOperator(string opName, [NotNullWhen(true)] out IScriptOperator? op)
		{
			if (opName == null)
			{
				op = null;
				return false;
			}

			// don't allow important namespaced operators to be overridden
			if (opName.StartsWith("LOSP:SP:"))
			{
				var result = LospInternalContext.SpecialOperators.TryGetValue(opName, out var sp);
				op = sp;
				return result;
			}
			else if (opName.StartsWith("LOSP:"))
			{
				return LospInternalContext.StandardOperators.TryGetValue(opName, out op);
			}
			else
			{
				// start by looking for user-defined operators
				return Operators.TryGetValue(opName, out op)
					// fall back on standard operators
					|| LospInternalContext.StandardOperators.TryGetValue(opName, out op);
			}
		}

		#endregion operators

		#region vars

		public static bool TryGetGlobalVar(string varName, [NotNullWhen(true)] out LospValue? value)
		{
			return _runner.TryGetVar(varName, out value);
		}
		public static void SetGlobalVar(string varName, LospValue value)
		{
			_runner.SetVar(varName, value);
		}

		#endregion vars

		/// <summary>
		/// Parses the input Losp script and returns a root-level <see cref="LospNode"/>
		/// that can be passed to <see cref="Eval(LospNode)"/> to be evaluated.
		/// </summary>
		/// <param name="input">The Losp script source.</param>
		public static LospNode Parse(string input)
		{
			return ASTBuilder.BuildAST(Tokenizer.Tokenize(input));
		}

		public static EvalResult Eval(string input)
		{
			return Eval(Parse(input));
		}

		private static readonly LospRunner _runner = new();
		public static EvalResult Eval(LospNode root)
		{
			return _runner.Eval(root);
		}

		public static EvalResult Call(LospLambda func, IEnumerable<LospValue> args)
		{
			return _runner.Call(func, args);
		}
	}
}
