# About Losp

*Losp* is a Lisp-adjacent scripting language with an interpreter written in C#. It has syntactic support for lists and object literals; as such it may be considered closer to something like Clojure. The host app can register operators to expand and customize its operator library. Purists looking for a Lisp that adheres strictly to Lisp dogma will likely be disappointed; there's no concept of cons cells, there's no integral support for tail call optimization. There *is* a concept analogous to special forms, here just called *special operators*.

## What It Looks Like

This example creates an object literal, which has multiple tags and multiple keyed values. Keyed values look like `{ name value }`.

```
{{
	// a comment

	#example // a tag; the first one is known as the "head tag"
	#second-tag #third-tag // tags, like any expression, can be listed inline
	// tags are provided for use by the host app; they are not used internally

	// keyed values are symbol/expression pairs in the simple case
	{ an-int 5 }
	{ a-float 5.0 }
	{ a-bool false }
	{ a-string "hello!" }
	{ a-null null }
	{ a-list [1 2 3 "mixed types" {{ #another-object }}] }
	{ implicit-true-key } // if no value is specified, it becomes `true`

	{ an-operator (+ 1 5) } // will be evaluated so an-operator = 6
	{ a-little-program
		// (LAST) only emits one value (the last one);
		//  necessary here but for reasons beyond the scope of this example
		(LAST 
			=(var 5) // assign a value to a variable named `var`
			=(var2 (* var 11))
			(CONCAT "the value of var2 is " var2 ", which should be 55")
		)
	}
	{ a-lambda FN([param1] param1) } // doesn't do anything except return its parameter

	// a not-simple case, where a keyed value has its own
	// keyed values, and thus becomes its own object literal!

	{ sub-properties
		{ sub-int 55 }
		{ sub-string "sub- sub- sub- properties are possible!" }
	}
}}
```

## In a Nutshell

Losp has Lisp-like operators, lists, and objects. A "Lisp-like operator" means it is enclosed in parentheses and its name (or *symbol*) is listed first, then all arguments to the operator are listed after, with only whitespace between them, no commas.

Native basic data types are `int`, `float`, `bool`, and `string` (and the somewhat special case `null`). As shown above, Losp also supports lists, object literals, and lambdas.

Lists are enclosed in square brackets and object literals are enclosed in double curly brackets. Object literals and operators can have *keyed values*, and keyed values may recursively have keyed values. A keyed value is enclosed in single curly brackets and its naming symbol is listed first.

Lambdas allow for user-defined functions to be created and passed around, with zero or more parameters. Lambda bodies are only evaluated when the lambda is invoked as or by an operator or by a host app. Lambdas must be assigned to a variable to be invoked as an operator.

Host apps can provide *extrinsic* types and can register their own operators.

## More!

Check out [the documentation](docs/using-losp.md)!

## Questions I'd Like to Imagine People Would Ask on Occasion

* **Does Losp have a BNF?**<br />No; Losp is defined using hopes and dreams. If you are particularly motivated to figure one out, you can perhaps look at the token pairs defined in `ASTBuilder` to derive a formal BNF.
* **REPL?**<br />Not prepackaged, but one shouldn't be too hard to build in your environment of choice by combining `Losp.Eval()` and `Losp.Write()`.
* **Do you accept contributions?**<br />In theory, yes. In practice, it remains to be seen if I have the bandwidth and attention to keep up with contributions. Losp is unlikely to be a high priority once it is relatively stable.
  