This document broadly describes the Losp syntax and how to use Losp in a host application.

A list of native operators and special operators is TODO; apologies.

# The Basics

## Syntax

### Whitespace

For the most part, whitespace is insignificant. Whitespace *is* required to separate literals, since commas aren't used, but this is pretty intuitive. In a few cases--particularly with object literals and special operators--some care has to be taken regarding whitespace, but for the most part expressions can be placed on one line, or multiple lines, or padded out with extra space.

Some conventions are that object literals and object keyed values are padded with whitespace

```
{{ { name "Losp" } { planet "Earth" } }}
```

but operators and lists are not.

```
[1 2 3 (/ 7.0 2.0)]
```

Keyed values in operators are often not padded, just to keep the expression more concise.

```
(CONCAT {delim ","} 1 2 3 4 5)
```

Large object literals, and keyed values that are themselves object literals, are spread across several lines.

```
{{
	{ name "Losp" }
	{ planet "Earth" }
	{ supports
		{ literals true }
		{ lists true }
		{ cons-cells false }
		{ TCO false }
		{ hamburgers false } // but host apps can!
	}
}}
```

### Literals and Symbols

Losp supports literals for integers, floats, Booleans, and strings.

Support for integer literals is basically anything that C# will parse as an `int`: `0 6 -100`.

Support for float literals is basically anything that C# will parse as an `float` *but doesn't parse as an `int`*. This is because Losp will first attempt to parse a value as an `int` and will only try to parse it as a `float` if unsuccessful. In practice, this means floats should have the decimal dot to disambiguate them from `int`s: `0.0 6.1 -100.55545567`. The dot is *required* and is culture-invariant; a comma or any other separator cannot be used.

The supported Boolean literals are `true` and `false`, both case-sensitive.

Losp supports a null value, whose literal is `null`, also case-sensitive.

String literals can be enclosed in double-quotes or in backticks: ``"this is a string" `this is also a string` ``.

Symbols are bare strings (not enclosed in quotes or backticks) that in general follow these rules:

* cannot contain whitespace
* cannot contain an enclosing character (``[](){}`"``)
* is not a reserved name
* cannot evaluate as a literal

Otherwise, symbols can include almost any character, including dahses, underscores, commas, colons, and semicolons. `-_,:;` is a legal symbol. Symbols are used as operator names, key names, and as variable names.

When a symbol is not used as an operator or key name, the interpreter will treat the symbol as a variable and attempt to look up the value associated with it; if none is found, it will generate an error.

### Comments

Line comments are supported using double-slashes. The slashes and everything after on that line are ignored by the parser.

```
< "popcorn" // this is a comment and is ignored
> "popcorn"
```

There are currently no comment blocks.

### Operators

Losp does the typical Lisp thing of operators that are enclosed in parentheses and start with the operator name as a symbol. An addition operator might look like `(+ 5 6)`. Note that Losp doesn't use commas (or semicolons for that matter).

```
< (+ 5 6)
> 11
```

Losp operators (like Lisp) are evaluated inside-out.

```
< (+ (- 8 3) 6)
> 11
```

### Lists

Losp supports lists. Lists can hold any kind of Losp expression except keyed values (which haven't been covered yet). Lists are defined by enclosing square brackets.

```
< [5 (+ 5 6) "hello"]
> [5 11 "hello"]
```

### Object Literals, Keyed Values, and Tags

Losp supports object literals, analogous to object literals in JavaScript and JSON. Unlike lists, object literals *do* support keyed values and, in fact, *only* support keyed values as direct child expressions (with the minor exception of tags).

Object literals are defined by enclosing double-curly-brackets. Keyed values are defined by single-curly-brackets.

An bare example (not being run in an interpreter):

```
{{
	{ name "Losp" }
	{ age 0 }
	{ flag-type-key }
	{ nested-expression (* 3 2) }
}}
```

Keyed values can be used in one of three basic ways. The typical use is in `{ key-name value }` pairs, where `value` can be any expression that emits a single value. (Operators can emit zero or more values; this hasn't been covered yet.)

Keys without a value implicitly evaluate to `true`. Therefore, `{ flag-type-key }` above is short for `{ flag-type-key true }`.

Finally, a keyed value can have its own keyed values. If a keyed value has *any* keyed value, all unkeyed values are ignored. When a keyed value has its own keyed values, it is evaluated as its own object literal. The following examples are equivalent.

```
{{
	{ name "Losp" }
	{ key-with-props
		#subobject
		{ planet "Earth" }
		{ fave-food "stuffing" }
	}
}}

{{
	{ name "Losp" }
	{ key-with-props {{
		#subobject
		{ planet "Earth" }
		{ fave-food "stuffing" }
	}} }
}}
```

Object literals can certainly be specified inline, but because of the syntax precedence of double-curlies, care has to be taken. Note that in the second example below, `}}}` will be interpreted as `}} }` instead of the intended `} }}`.

```
< {{ { name "Losp" } }}
> <object literal>

< {{{ name "Losp" }}}
> syntax error
```

Object literals also support *tags*, which are exclusive to object literals. Tags must be listed before keyed values. Tags are provided for host apps to use as desired; they are not used by Losp internally.

```
{{
	#lang-description
	{ name "Losp" }
	{ data-types ["literal" "list" "object literal" "lambda"] }
}}
```

#### Keyed Values in Operators

Although keyed values are not supported in lists, they *are* supported in operators.

```
< (CONCAT "one" "two")
> "onetwo"

< (CONCAT {delim ", "} "one" "two")
> "one, two"
```

The API for operators supports indexing arguments as a simple list (`[0]: { delim ", " }, [1]: "one", [2]: "two"`) or by unkeyed arguments (`[0]: "one", [1]: "two"`) or by keyed arguments (`[0]: { delim ", " }`). Keyed arguments can be looked up by key name as well. This makes it simpler to support multiple, optional arguments.

## Special Operators

A *special operator* is an elevated type of operator that is allowed to inspect and transform itself before evaluation. They are analogous to Lisp special forms. Flow control expressions are defined using special operators.

Syntactically, they look like operators but with an important difference: the operator name *precedes* the opening parenthesis.

```
< IF((? true) "yes, this is true" "no, this is false")
> "yes, this is true"

< =(id 5) // assignment, which also returns the value assigned
> 5
```

Unlike common operators, host apps cannot register operator handlers to replace the internal handlers. Host apps *can* define their own special operators, but they must start with a dollar sign (`$`), must have a valid symbol name, and cannot be *only* a dollar sign.

```
< $USER-DEFINED-SP-OP(5 6 7)
> "lol numbers"

< $(5 6 7)
> syntax error
```

## Lambdas (Anonymous Functions)

Lambdas are created using the `FN` special operator. The first parameter of a lambda *must* be its parameter list, even if it is an empty list. Only symbols may be included in the parameter list. Lamdas may have one or more body expressions.

```
< FN([param1 param2] param1 param2) // simply returns the paramters given
> <lambda>

< FN((+ 1 2)) // did not start with the param list!
> syntax error

< FN([] (+ 1 2))
> <lambda>

< FN([0] (+ 1 2)) // only symbols are allowed in the param list!
> syntax error
```

The body expressions of a lambda are not evaluated immediately. If a lambda is assigned to a symbol (i.e. a variable), it becomes available as an operator to any operator expression within the variable's scope.

```
< =(lm FN([name] (CONCAT "hello " name "!")))
> <lambda>
< (lm "cool user") // using the assigned lambda and providing an argument
> "hello cool user!"
```

Lambdas returned to the host app can also be invoked with values from the app as arguments.

## Expression Expansion

... TODO (talk about emitted values, esp. when zero or multiple values are emitted)

# API

## Parsing and Evaluating

Losp strings can be evaluated directly:

```csharp
using chebwa.Losp;

var result = Losp.Eval("(+ 3 5)");
```

Losp can also be parsed first into an Abstract Syntax Tree (AST), and the AST can be evaluated later.

```csharp
var ast = Losp.Parse("(+ 3 5)");
var result = Losp.Eval(ast);
```

`Losp.Parse()` will throw an exception if the source string cannot be parsed.

Evaluating a Losp expression can result in three `EvalResult` types: `ValueResult`, `AsyncResult`, and `ErrorResult`.

To get it out of the way first, an `ErrorResult` will be returned if there is an evaluation error (e.g. the arguments to an operator were not of the correct type, or a symbol could not be resolved). If `Losp.Eval()` is called with a source string, any exception is caught and wrapped in an `ErrorResult`.

### ValueResult

A `ValueResult` means the expression was evaluated successfully (and synchronously). However, expressions can emit *zero or more* values when evaluated.

Literals and data structures will always emit a single value.

```
< 6
> 6
< "six"
> "six"
< [6]
> [6]
< {{ {num 6} }}
> <object literal>
```

Operators can emit zero or more values; this is dependent both on the operator and on the expressions provided to the operator.

```
< (RUN 27 (+ 1 5) (/ 6.0 3.0)) // (RUN) just emits all its child expressions
> 27 6 2.0
< (RUN)
> <success>
```

You can check if a `ValueResult` emitted a value by checking its `Type`.

```csharp
if (valueResult.Type == ResultType.SuccessNoEmit)
{
	// no values
}
else // Type == ResultType.SuccessEmit
{
	// one or more values
}
```

Or, you can just check its `Values` list. However, this list is an `IEnumerable` and thus a small penalty may be incurred when calling `Count()`. `Values` always exists, even when no values were emitted (in which case it is an empty list).

```csharp
foreach (var value in valueResult.Values)
{
	// inspect each value
}
```

### AsyncResult

In contrast to a `ValueResult`, an `AsyncResult` indicates that the operator must perform an asynchronous process before it can emit a value. An `AsyncResult` provides a `Source` member that implements an `OnAsyncCompleted()` callback hook. The `AsyncResult` provides this hook as well for convenience.

When a callback is passed to `OnAsyncCompleted()`, it is triggered with the final, evaluated `ValueResult` of the expression or with an `ErrorResult`. An `AsyncResult` *cannot* resolve with another `AsyncResult`. This is enforced at all levels of the interpreter, and is particularly true at the top level, as invoked by the host app.

To emphasize the consequence of this: if mulitple operators in a Losp script trigger an `AsyncResult`, only one top-level `AsyncResult` is emitted to the host app. All asynchronous operators are handled internally and fully resolved before the top-level `AsyncResult` is resolved.

## Losp Values

Values in Losp are wrapped in a subclass of `LospValue`. Each literal has its own type (e.g. `LospInt` which is a `LospValue<int>`, `LospBool` which is a `LospValue<bool>`, etc.).

Lists are `LospList`, which is a `LospValue<IEnumerable<LospValue>>`. Lambdas are `LospFunc`, which is a `LospValue<LospLambda>`. Null values are represented by `LospNull` which is a `LospValue<object>`.

Object literals are represented by `LospScriptable`, which is a `LospValue<IScriptObject>`. Object literals implement the `IScriptObject` interface and thus can be used directly in that context. Host apps can also supply their own values that implement `IScriptObject`, or use a wrapper object to expose values via the interface. (Losp provides a few, basic wrapper types, particularly `LambdaScriptObject` and `ReflectionScriptObject`, but apps can create their own.)

### Extrinsics

Host apps can also pass any value to Losp using `LospExtrinsic<T>`. This class provides the app with any `LospValue<T>` it requires. Although native Losp operators likely cannot use extrinsic types, host apps are free to register operators that can operate on the extrinsic type. Instances of `LospExtrinsic<T>` are created by calling `LospValue.Extrinsic<T>()`.

### Creating

If you know your data type, and it's a native Losp type, you can create its enclosing `LospValue` directly, e.g. `new LospInt(5)` or `new LospString("interesting text")`. If you don't know the type, you can call `LospValue.Convert()` with the value, and it will attempt to create the correct `LospValue` type. `Convert()` cannot create a `LospExtrinsic`, however, as a type parameter is required. If an unsupported type is passed to `Convert()`, an exception is throw.

`Convert()` will cast or convert some types, which is important to know if this behavior is not desired. `char` is converted to `string`, `uint` is cast to `int`, and `double` is cast to `float`.

### Inspection

In simple scenarios (e.g. with value types or strings), the type of a `LospValue` can be evaluated with the C# `is` operator.

```csharp
if (value is LospValue<int> i)
{
	// i.Value is an int
}
```

One can also use pattern matching in a `switch` expression.

```csharp
return value switch {
	LospValue<int> i => i.Value.ToString(),
	LospValue<float> f => f.Value.ToString(),
	LospValue<string> s => s.Value,
	_ => "unsupported type",
};
```

For more complex scenarios (for example, you want a base class type but the extrinsic is a subclass type, or vice versa), one can use `TryGet()` for all types or `TryGetNonNull()` for reference types. `LospValue` has several helper methods for retrieving specific underlying types.

```csharp
public class Monster() : Creature();

// ...

// `value` is a `LospExtrinsic<Creature>` (and thus a `LospValue<Creature>`);
// `value is LospValue<Monster>` wouldn't work,
// since `LospValue<Creature>` can't be cast to `LospValue<Monster>`
if (value.TryGetNonNull(out Monster monster))
{
	// value stores a Monster
}
```

## Registering Operators

Standard operator handlers must implement the `IScriptOperator` interface. Special operator handlers must implement `ISpecialOperator`, which extends `IScriptOperator`.

### `IScriptOperator`

The `IScriptOperator` interface is very simple: it defines a `Run()` method that is called when the evaluator has evaluated all child expressions and is ready to evaluate the operator.

`Run()` provides the following parameters:

* `IScriptContext` - the context in which the operator is being run; mostly this provides a means to get and set variables. This is typically uncommon in `IScriptOperator`s, because all child expressions have already been evaluated, but useful for `ISpecialOperator`s.
* `LospOperatorNode` - the node, part of the AST, describing the operator expression. Also not typically used, although inspecting the node's `Name` is sometimes useful if the handler is registered to multiple operator names.
* `LospChildResultDataCollection` - the evaluated results of the operator's child expressions. This is where most of the action happens.

#### Child Values

The `LospChildResultDataCollection` (hereafter just "result collection") provides means to access child results, and even skip over the `LospValue` wrappers and get straight to the underlying value.

The result collection provides three lists. First is an unfiltered list of all child results; the result collection can be indexed directly using an `int` index. This returns all keyed and unkeyed chlid results. Methods like `TryIndex()`, `TryIndexOf()`, and `TryIndexOfNonNull()` provide ways to access values more ergonomically.

Second is list of keyed values only. The result collection can be indexed directly using a `string` key to access keyed values. You can get the list of key strings through `Keys`, key/value pairs via `KeyedPairs`, and you can use methods like `TryKey()` and `TryKeyOf()` for more convenience.

Third is a list of unkeyed values provided through the `Unkeyed` and `UnkeyedValues` members. Like the result collection itself, `Unkeyed` can be indexed with an `int` index to access unkeyed values only. Note that these indices are ordinal values based on the position of the unkeyed result. Consider this example:

```
(CONCAT {delim " "} "first" "second")
```

Using `children[0]` will retrieve the keyed value `" "`. Using `children.Unkeyed[0]` will retrieve the unkeyed value `"first"` instead, which is typically what an operator wants when operating on its parameters. Moreover, the position of the keyed value can change.

```
(CONCAT "first" "second" {delim " "})
```

Both `children[0]` and `children.Unkeyed[0]` will retrieve the unkeyed value `"first"` in this example. As such, using `Unkeyed` is the more reliable way to access unkeyed values.

#### Operating

The operator is free to operate on the values it has access to. Feel free to look at operator implementations in the Losp "standard library" (for lack of a better term) for examples.

What an operator supports or does not support is up to the operator. It may support a specified number of arguments, or a dynamic and unlimited number. (As often used for examples, `(CONCAT)` supports any number of arguments.) An operator can allow for any number of optional keyed parameters as well. In general, operators should try to find the right balance of simplicity vs. usefulness.

Also look at `ErrorResultHelper` for some standard error messages regarding unsupported argument counts or types.

#### Return Results

As described in [Parsing and Evaluating](#parsing-and-evaluating), an operator can return one of multiple `EvalResult` types. When implementing `IScriptOperator`, it is your responsibility to return the correct type. Typically this is straightforward.

One cannot instantiate a `ValueResult` using `new ValueResult()`. Instead, `ValueResult` has several static methods that instantiate a result. One can use `new ErrorResult()` or `new AsyncResult()`, however.

An operator does not *have* to emit a value, although typically it will. Some operators can simply do a task and note if the task succeeded or failed. In the former case, they can use `ValueResult.None()` to indicate that they succeeded without emitting a value. In the latter case, an `ErrorResult` would be used as usual. To emit one or more values, see below.

When using an `AsyncResult`--for example, if you need to wait for user input, or you are making an HTTP request--then you must be careful to complete the operation once the asynchronous process has completed. Furthermore, if there are multiple asynchronous steps, they must *all* be completed before the `AsyncResult` is notified as complete.

An `AsyncResult` takes an `IAsyncProxy`; this interface simply provides a hook to the evaluator (or to the host app) for listening to completion of the asynchronous operator.

A simple `AsyncProxy` implementation is provided by Losp. Using it is straightforward: create the `AsyncProxy` and pass it to `new AsyncResult`, which you then return as the `EvalResult` of the operator. When your asynchronous process has completed (or the series of asynchronous processes has completed), call `Complete()` on the `AsyncProxy` object with the new (and final) `EvalResult`. Only `ValueResult` or `ErrorResult` should be used. (There is an exception here, but it will be discussed under `ISpecialOperator` since it is typically irrevelant for an `IScriptOperator`.)

For emphasis, it will again be noted that *completing an `AsyncResult` with another `AsyncResult` is treated as an error*.

#### Emitting Values

... TODO (discuss emitting multiple values)

### `ISpecialOperator`

... TODO
