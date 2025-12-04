// SPDX-License-Identifier: MIT

using chebwa.Losp;
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

	/// <summary>
	/// The main interface to the Losp scripting library. It provides means to parse,
	/// evaluate, and write (as output) various elements of the Losp.
	/// </summary>
	public static class Losp
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

		/// <summary>
		/// The list of all natively supported Losp standard operator names.
		/// </summary>
		public static IReadOnlyCollection<string> NativeStandardOperators => LospInternalContext.StandardOperators.Keys;
		/// <summary>
		/// <para>
		/// The list of all natively supported Losp special operator names.
		/// </para>
		/// <para>
		/// Note that special operators have a prefix which is ignored for the purposes
		/// of matching an operator name to a handler.
		/// </para>
		/// </summary>
		public static IReadOnlyCollection<string> NativeSpecialOperators => LospInternalContext.SpecialOperators.Keys;

		/// <summary>
		/// Registers a standard operator handler to the <paramref name="operatorName"/>.
		/// Host apps may override native standard operator handlers, but appropriate
		/// caution must be exercised.
		/// </summary>
		/// <param name="operatorName">The name of the operator.</param>
		/// <param name="op">The handler invoked to perform the operator.</param>
		public static void AddOperator(string operatorName, IScriptOperator op)
		{
			ArgumentException.ThrowIfNullOrEmpty(operatorName, nameof(operatorName));
			ArgumentNullException.ThrowIfNull(op, nameof(op));

			//TODO: indicate an error is op name starts with "LOSP:"?

			Operators[operatorName] = op;
		}

		//TODO: register special handlers; enforce that the op name begins with "$"

		/// <summary>
		/// Attempts to retrieve the handler for a special operator by its
		/// <paramref name="opName"/>.
		/// </summary>
		/// <param name="opName">The name of the special operator.</param>
		/// <param name="op">The handlers for the special operator, if any.</param>
		public static bool TryGetSpecialOperator(string opName, [NotNullWhen(true)] out ISpecialOperator? op)
		{
			if (opName == null)
			{
				op = null;
				return false;
			}

			return LospInternalContext.SpecialOperators.TryGetValue(opName, out op);
		}

		/// <summary>
		/// Attempts to retrieve the handler for a standard or special operator by its
		/// <paramref name="opName"/>. Special operators are checked first, then standard
		/// operators. For standard operators, handlers registered by the host app are
		/// checked before native handlers.
		/// </summary>
		/// <param name="opName">The name of the operator.</param>
		/// <param name="op">The handlers for the operator, if any.</param>
		public static bool TryGetOperator(string opName, [NotNullWhen(true)] out IScriptOperator? op)
		{
			if (opName == null)
			{
				op = null;
				return false;
			}

			// handles special operators, which cannot be overridden by a host app
			if (LospInternalContext.SpecialOperators.TryGetValue(opName, out var sp))
			{
				op = sp;
				return true;
			}
			// handles namespaced standard operators that should not be overridden by a host app
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

		/// <summary>
		/// Attempts to retrieve the <see cref="LospValue"/> globally associated with the
		/// <paramref name="varName"/>.
		/// </summary>
		/// <param name="varName">The name of the variable to check.</param>
		/// <param name="value">The retrieved <see cref="LospValue"/>, if any.</param>
		/// <returns>A value indicating whether the <paramref name="value"/> was
		/// successfully retreived.</returns>
		//TOOD: reword <returns>?
		public static bool TryGetGlobalVar(string varName, [NotNullWhen(true)] out LospValue? value)
		{
			return _runner.TryGetVar(varName, out value);
		}
		/// <summary>
		/// Attempts to associate the <paramref name="value"/> globally with the
		/// <paramref name="varName"/>. The association persists across all invocations
		/// of the evaluator. Global variables can be accessed from any script as long
		/// there does not exist a more locally-scoped variable defined with the same
		/// <paramref name="varName"/>.
		/// </summary>
		/// <param name="varName">The name of the variable to set.</param>
		/// <param name="value">The value to associated with the <paramref name="varName"/>.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public static void SetGlobalVar(string varName, LospValue value)
		{
			ArgumentNullException.ThrowIfNull(value, nameof(value));
			_runner.SetVar(varName, value);
		}

		#endregion vars

		/// <summary>
		/// Parses the input Losp script and returns a root-level <see cref="LospNode"/>
		/// that can be passed to <see cref="Eval(LospNode)"/> to be evaluated.
		/// </summary>
		/// <param name="input">The Losp script source.</param>
		/// <exception cref="SyntaxException">The parser was unable to build an AST from the input.</exception>
		public static LospNode Parse(string input)
		{
			return ASTBuilder.BuildAST(Tokenizer.Tokenize(input));
		}

		/// <summary>
		/// Parses the input Losp script (by invoking <see cref="Parse(string)"/>) and
		/// evaluates the returned AST (by invoking <see cref="Eval(LospNode)"/>).
		/// </summary>
		/// <param name="input">The Losp script source.</param>
		/// <returns>The result of evaluating the parsed input.</returns>
		public static LospResult Eval(string input)
		{
			try
			{
				return Eval(Parse(input));
			}
			catch (Exception e)
			{
				return new LospErrorResult(new(null, e.Message));
			}
		}

		private static readonly LospRunner _runner = new();

		/// <summary>
		/// Evaluates the AST represented by the <paramref name="root"/>. Returned
		/// <see cref="EvalResult"/> types are:
		/// <list type="bullet">
		/// <item><see cref="ValueResult"/> - the evaluation was successful the
		/// <paramref name="root"/> emitted zero or more values</item>
		/// <item><see cref="AsyncResult"/> - the evaluation is pending one or
		/// more asynchronous operations and the host must register a callback
		/// to receive the result</item>
		/// <item><see cref="ErrorResult"/> - the evaluator was unable to
		/// evaluate the script due to a specified error</item>
		/// </list>
		/// </summary>
		/// <param name="root">The root or subtree of the AST to evaluate.</param>
		/// <returns>The result of evaluating <paramref name="root"/> and its AST.</returns>
		public static LospResult Eval(LospNode root)
		{
			return _runner.Eval(root);
		}

		/// <summary>
		/// <para>
		/// Parses the input Losp script (by invoking <see cref="Parse(string)"/>) and
		/// evaluates the returned AST (by invoking <see cref="Eval(LospNode)"/>).
		/// </para>
		/// <para>
		/// Unlike <see cref="Eval(string)"/>, async results are handled for you such
		/// that the only possible results are a <see cref="LospValueResult"/> or
		/// <see cref="LospErrorResult"/>.
		/// </para>
		/// </summary>
		/// <param name="input">The Losp script source.</param>
		/// <returns>The asynchronous result of evaluating the parsed input.</returns>
		public static Task<LospTerminalResult> EvalAsync(string input)
		{
			return EvalAsync(Parse(input));
		}

		/// <summary>
		/// <para>
		/// Evaluates the AST represented by the <paramref name="root"/>. Unlike
		/// <see cref="Eval(LospNode)"/>, possible returned <see cref="LospResult"/>
		/// types are:
		/// <list type="bullet">
		/// <item><see cref="LospValueResult"/> - the evaluation was successful the
		/// <paramref name="root"/> emitted zero or more values</item>
		/// <item><see cref="LospErrorResult"/> - the evaluator was unable to evaluate
		/// the script due to a specified error</item>
		/// </list>
		/// </para>
		/// </summary>
		/// <param name="root">The root or subtree of the AST to evaluate.</param>
		/// <returns>The asynchronous result of evaluating <paramref name="root"/> and its AST.</returns>
		public static Task<LospTerminalResult> EvalAsync(LospNode root)
		{
			var result = _runner.Eval(root);

			if (result is LospAsyncResult ar)
			{
				var tcs = new TaskCompletionSource<LospTerminalResult>();

				ar.OnAsyncCompleted(tcs.SetResult);

				return tcs.Task;
			}

			return Task.FromResult((LospTerminalResult)result);
		}

		/// <summary>
		/// Invokes a Losp lambda with the provided arguments.
		/// </summary>
		/// <param name="lambda">The Losp lambda to invoke and evaluate.</param>
		/// <param name="args">The collection of zero or more arguments to pass to the
		/// <paramref name="lambda"/>.</param>
		/// <returns>The result of evaluating the <paramref name="lambda"/>.</returns>
		public static LospResult Call(LospLambda lambda, IEnumerable<LospValue> args)
		{
			return _runner.Call(lambda, args);
		}

		/// <summary>
		/// <para>
		/// Invokes a Losp lambda with the provided arguments.
		/// </para>
		/// <para>
		/// Unlike <see cref="Call(LospLambda, IEnumerable{LospValue})"/>, async results
		/// are handled for you such that the only possible results are a
		/// <see cref="LospValueResult"/> or <see cref="LospErrorResult"/>.
		/// </para>
		/// </summary>
		/// <param name="lambda">The Losp lambda to invoke and evaluate.</param>
		/// <param name="args">The collection of zero or more arguments to pass to the
		/// <paramref name="lambda"/>.</param>
		/// <returns>The asynchronous result of evaluating the <paramref name="lambda"/>.</returns>
		public static Task<LospTerminalResult> CallAsync(LospLambda lambda, IEnumerable<LospValue> args)
		{
			var result = Call(lambda, args);

			if (result is LospAsyncResult ar)
			{
				var tcs = new TaskCompletionSource<LospTerminalResult>();

				ar.OnAsyncCompleted(tcs.SetResult);

				return tcs.Task;
			}

			return Task.FromResult((LospTerminalResult)result);
		}

		/// <summary>
		/// Prints the AST represented by the <paramref name="node"/>. If the AST
		/// was built from the Losp parser, any <see cref="LospOperatorNode"/>s that
		/// are special operators have already been invoked to transform their nodes;
		/// as such, the output may not directly mirror the source script.
		/// </summary>
		/// <param name="node">The <see cref="LospNode"/> describing the root
		/// or subtree of an AST.</param>
		/// <returns>A stringified version of <paramref name="node"/> and its AST.</returns>
		public static string Write(LospNode node)
		{
			return LospWriter.WriteNode(node).ToString();
		}

		/// <summary>
		/// Prints an <see cref="LospResult"/> as a string intended to be used e.g. as
		/// output to a REPL. For a <see cref="LospValueResult"/>, the verbosity of the
		/// printed values is controlled by <paramref name="underlyingValueOnly"/>.
		/// </summary>
		/// <param name="result">The result type to print.</param>
		/// <param name="underlyingValueOnly">When printing <see cref="LospValue"/>s,
		/// determines whether it value is annotated with its type. When <see langword="true"/>,
		/// the <see cref="LospValue"/>'s underlying value is printed with no type
		/// annotation.</param>
		/// <returns>The <paramref name="result"/> as a string.</returns>
		public static string Write(LospResult result, bool underlyingValueOnly = false)
		{
			return LospWriter.WriteResult(result, underlyingValueOnly);
		}

		/// <summary>
		/// Prints an <see cref="EvalResult"/> as a string intended to be used e.g. as
		/// output to a REPL. For a <see cref="ValueResult"/>, the verbosity of the
		/// printed values is controlled by <paramref name="underlyingValueOnly"/>.
		/// </summary>
		/// <param name="result">The result type to print.</param>
		/// <param name="underlyingValueOnly">When printing <see cref="LospValue"/>s,
		/// determines whether it value is annotated with its type. When <see langword="true"/>,
		/// the <see cref="LospValue"/>'s underlying value is printed with no type
		/// annotation.</param>
		/// <returns>The <paramref name="result"/> as a string.</returns>
		public static string Write(EvalResult result, bool underlyingValueOnly = false)
		{
			return LospWriter.WriteResult(result, underlyingValueOnly);
		}

		/// <summary>
		/// Prints a <see cref="LospValue"/> as a string intended to be used e.g. as
		/// output to a REPL. The verbosity of the printed value (and any nested values)
		/// is controlled by <paramref name="underlyingValueOnly"/>.
		/// </summary>
		/// <param name="value">The value to print.</param>
		/// <param name="underlyingValueOnly">Determines whether the output is annotated
		/// with types. When <see langword="true"/>, the <paramref name="value"/> and any
		/// nested values are printed with no type annotation.</param>
		/// <returns>The <paramref name="value"/> as a string.</returns>
		public static string Write(LospValue value, bool underlyingValueOnly = false)
		{
			return LospWriter.WriteValue(value, underlyingValueOnly);
		}
	}
}
