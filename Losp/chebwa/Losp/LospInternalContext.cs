using chebwa.Losp.Operators;
using chebwa.LospNet.Operators;

namespace chebwa.LospNet
{
	internal class DoublePushTestOperator : IScriptOperator
	{
		public EvalResult Run(IScriptContext context, LospOperatorNode op, LospChildResultDataCollection children)
		{
			var push1 = new LospLiteralNode()
			{
				Data = new LospInt(1),
			};
			var push2 = new LospLiteralNode()
			{
				Data = new LospInt(2),
			};

			return new PushResult([push1], (res1) =>
			{
				var r1 = (int)res1[0].BoxedValue!;
				return new PushResult([push2], (res2) =>
				{
					var r2 = (int)res2[0].BoxedValue!;
					return ValueResult.SingleOrNone(r1 + r2);
				});
			});
		}
	}

	/// <summary>
	/// Stores the mapping of all the built-in operator names to their handlers.
	/// </summary>
	internal static class LospInternalContext
	{
		internal static readonly Dictionary<string, ISpecialOperator> SpecialOperators = [];
		internal static readonly Dictionary<string, IScriptOperator> StandardOperators = [];

		internal const string LospSpecialOperatorPrefix = "LOSP:SP:";
		public const string LospAssignmentOpName = LospSpecialOperatorPrefix + "=";
		public const string LospIfOpName = LospSpecialOperatorPrefix + "IF";
		public const string LospForOpName = LospSpecialOperatorPrefix + "FOR";
		public const string LospForIOpName = LospSpecialOperatorPrefix + "FORI";
		public const string LospIncOpName = LospSpecialOperatorPrefix + "++";
		public const string LospDecOpName = LospSpecialOperatorPrefix + "--";
		public const string LospWaitOpName = LospSpecialOperatorPrefix + "WAIT";

		static LospInternalContext()
		{
			// special
			SpecialOperators[LospAssignmentOpName] = new AssignmentOperator();
			SpecialOperators[LospIfOpName] = new IfOperator();
			SpecialOperators[LospForOpName] = new ForOperator();
			SpecialOperators[LospForIOpName] = new ForIOperator();
			SpecialOperators[LospIncOpName] = IncDecOperator.Instance;
			SpecialOperators[LospDecOpName] = IncDecOperator.Instance;
			SpecialOperators[LospWaitOpName] = new WaitMSOperator();

			// lists
			StandardOperators["COUNT"] = new CountListOperator();
			StandardOperators["ANY"] = AnyOperator.Instance;
			StandardOperators["ALL"] = AllOperator.Instance;
			StandardOperators["?"] = AllOperator.Instance;

			// objects
			StandardOperators["."] = new PropertyOperator();
			// COUNT also works on the keys of object literals

			// math
			StandardOperators["+"] = MathOperator.Instance;
			StandardOperators["-"] = MathOperator.Instance;
			StandardOperators["/"] = MathOperator.Instance;
			StandardOperators["*"] = MathOperator.Instance;
			StandardOperators["%"] = MathOperator.Instance;
			StandardOperators["^"] = MathOperator.Instance;
			StandardOperators["PI"] = new PiOperator();

			// comparison
			StandardOperators["=="] = ComparisonOperator.Instance;
			StandardOperators["!="] = ComparisonOperator.Instance;
			StandardOperators["<"] = ComparisonOperator.Instance;
			StandardOperators["<="] = ComparisonOperator.Instance;
			StandardOperators[">"] = ComparisonOperator.Instance;
			StandardOperators[">="] = ComparisonOperator.Instance;
			//
			StandardOperators["1"] = TruthinessOperator.Instance;
			StandardOperators["~1"] = TruthinessOperator.Instance;
			StandardOperators["0"] = TruthinessOperator.Instance;
			StandardOperators["~0"] = TruthinessOperator.Instance;
			//
			StandardOperators["!"] = NotOperator.Instance;
			StandardOperators["~!"] = NotOperator.Instance;

			// strings
			StandardOperators["CONCAT"] = new ConcatOperator();
			StandardOperators["LINE"] = new LineOperator();
			StandardOperators["STR-INT"] = new StrToIntOperator();
			StandardOperators["TO-STR"] = new ToStringOperator();
			StandardOperators[HasSubstringOperator.StartsWithOpName] = HasSubstringOperator.Instance;
			StandardOperators[HasSubstringOperator.EndsWithOpName] = HasSubstringOperator.Instance;
			StandardOperators[HasSubstringOperator.ContainsOpName] = HasSubstringOperator.Instance;

			// misc
			StandardOperators["RUN"] = ContainerOperator.UnmutedInstance;
			StandardOperators["LAST"] = ContainerLastResultOperator.Instance;
			StandardOperators["MUTE"] = ContainerOperator.MutedInstance;
			StandardOperators["EXPAND"] = new ExpandOperator();
			StandardOperators["COLLAPSE"] = new CollapseOperator();
			StandardOperators["LOSP:TEST:DBLPUSH"] = new DoublePushTestOperator();
		}
	}
}
