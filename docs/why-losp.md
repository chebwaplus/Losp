This document is broadly about the design history and decisions behind Losp. It is absolutely not required reading. What is the opposite of required reading? It's that.

# How It Started

Working on a project, I needed a language that, ideally, provided two major features: structural data and scripting.

When working with structural data, two obvious choices come to mind: JSON and XML. Both are ubiquitous and easy to include in most projects. JSON has a major edge in concision; XML has an advantage in markup and describing metadata. Neither is a *programming* language, which was the other feature I wanted. They can be beaten to look and act like a scripting language, with the host app doing heavy lifting for much of the logic, but it's not pretty or simple.

For embedding a scripting language in a C# project, there tend to be three common options: Lua, C# itself (or some .NET language), and JavaScript. All of these are pretty comparable as far as simple scripting features go. The language VM does most of the work; the host app just needs to hook into it. But none of these are great when you just want to write structural data, e.g. if you want to define some sort of configuration data. It's possible but you're bringing missles to a thumb war.

> One might notice that JavaScript appears on both lists in some sense. JSON is valid JS, so using a JS interpreter should be sufficient for both purposes. And frankly, I'm not sure I have a strong argument against this option. I had spent some time, for legacy reasons, attempting to wrestle JSON into a monstrous, Lisp-like abomination. By that point I believe my brain just shut out the idea of JSON (and, by extension, JS) entirely.
>
> If I had a time machine, I might go back and tell me to run a JS interpreter. But we're still here, in the Losp timeline.

When fans of programming languages think about data, code, and perhaps how one is the same as the other, they probably think of [(Common) Lisp](https://en.wikipedia.org/wiki/Common_Lisp). I might argue that these people are crazy; representing data in Lisp is bonkers and not readily parsable by humans. Lisp, though, has the significant benefit of being dead simple to parse by machine. One might even suggest that Lisp was developed to benefit from its syntactic simplicity.

# So Lisp I Guess? or: S-Express Yourself

## Parsing

A programmer who's had any experience writing a parser can probably write one for Lisp in a few days, perhaps even a few hours. Even without experience, there are apparently several places around the web that offer step-by-step guides, of varying quality and completeness.

After taking care of the basics, it's fairly simple to add various *accoutrement* to support special structures. Square brackets for lists; curly brackets for objects. With enough sugar dust in place, you've got a ~~Clojure~~ Losp.

Writing a parser is one thing; writing an evaluator is another.

> As of *this* writing, I have not checked to see if [Clojure](https://en.wikipedia.org/wiki/Clojure) has a .NET implementation. I'm not sure I can bear to find out that I let a second potential candidate go unexplored. That said, I suspect there are enough *weirdnesses* with Clojure that I would not have chosen it. Defining an object literal is notably and sufficiently funky.

## Evaluation

Lisp, from the outside, is quite straightforward if slightly perplexing to view: it is a parade of parentheses, symbols, literals, and parentheses. Its innards betray this simplicity, as the decades since Lisp's invention have allowed Data Scientists to find the most efficient ways to represent its data structures internally and to process the data through algorithms. Here is where, as a developer of a potential Lisp-like, I struggled. Every time I thought I had a good grasp on Lisp inner workings, I had a moment that sundered my faith. I fear this confession may hurt my credentials as a developer, but I have accepted this risk.

I understood [cons cells](https://en.wikipedia.org/wiki/Cons), insomuch as they are part of a concept (the cons cell concept?) that is not complicated. What I failed to grasp was how implementing them benefitted me or my language. (Although I am using the past tense, rest assured that, as of this writing, it is still the case.) They sounded neat and pure, in the way good Data Science things sound, but also they didn't seem useful in the real world, also just like good Data Science things.

Skipping over that I had to face the shibboleth of [tail call optimization](https://en.wikipedia.org/wiki/Tail_call). Similar to cons cells, I felt like I understood the thing itself, but how to implement it eluded me. Perhaps if the Losp AST were faithfully built from cons cells, the power of TCO would unfurl before and around me, dazzling me like the lights of spacetime as I ascend to become a star child.

As I believe most developers understand, my choices were: deep and perhaps unending study, or Getting It Done. Also like most developers, I chose the latter. The small beauty of Getting It Done is that you can always (for certain values of "always") go back and toil at the concepts later, and that might even be the better time to do it, as now you have grown through the pain of experience and are in a better position to apply your studies to a specific implementation. Chosing deep and perhaps unending study means you might never Get It Done; at least, not in the window in which you need it. There is a future one can imagine where the Losp parser and evaluator are revisited, perhaps seeing fantastical performance improvements by using the Lisp secret herbs and spices.

Losp gets by with a relatively simple, stack-based iterative loop. It does not recurse except in certain pathological scenarios. As it walks down the AST, the stack grows. For each child of a node, it tries to evaluate that child, pushing it on the stack and restarting the loop. When no child is left, it attempts to finalize the node. When the node is finalized, its value is passed to its parent and it is popped off the stack. (Not strictly true; it was already off the stack by that point.) When the stack has completed, the full AST has been (hopefully) evaluated and the final result is returned to the client.

Internal data structures are, mostly, C# Lists and Dictionaries. Occasionally arrays; occasionally a custom IEnumerable type. Nothing elaborate; nothing exotic. (I'm sure the smart people who worked hard to make those data structures efficient would disagree. It would be great irony if Lists were implemented with cons cells.)

Cons cells and TCO are internal matters; a writer of Lisp scripts may never have to think about them. What is *not* an internal matter, and something I have come to find very disagreeable about Lisp, is something called *special forms*.

## `(THROW)`

Special forms are essentially a concession to a realization about Lisp that is observably true: you can't do all the Programming Language Things you want to do in "pure" Lisp. You need a way to reason about language-level constructs that are intrinsic in other languages but aren't directly available to you in Lisp.

Lisp proponents will argue the other way, and not without success: special forms give you a way to essentially fold the language in on itself, to take code and make it data. Most languages make this hard or impossible.

Special forms allow the interpreter to break the rules about the language. They give operators access to the script itself; to inspect the AST as it is being interpreted.

This is all good; necessary, even. Breaking the fourth wall of code sounds rad. The way Lisp does it, though, is a tremendous disappointment to me: Lisp does it *in secret*.

If you tell me you added a special quote symbol to your language that breaks you out into special form mode, I'd say cool, that's certainly one way to do it. If you then told me that you simply transformed the quote symbol into a `(quote)` operator, that looks like any other operator, and which can be used in a script directly, I would become skeptical.

Herein lies my biggest(?) problem with Lisp: A person casually reading a Lisp script *cannot know which operators are special forms without looking them up*. In almost any random operator `(op a)`, `a` gets evaluated as whatever value that was assigned to it. But in `(quote a)`, `a` is evaluated as `a`. It's immediately clear when writing Lisp that this is fundamentally necessary: you can't assign a value to `a` if `a` is always getting evaluated into something else.

Clojure does not improve Lisp in this regard. `(defn)`, for example, is still a magic invocation that turns AST nodes into a lambda.

I realized that I would still need to do something analogous to special forms in Losp. It hews close enough to Lisp to encounter the same issues. I decided that the least I could do was make special forms obvious. My solution was to use a syntax that is unconventional in Lisp but quite conventional elsewhere: have the operator name *precede* the opening parenthesis. If `(quote)` existed in Losp (it doesn't, currently), it would be written `quote()`. You're right; that *does* look like a normal function call. It isn't! It is decidedly abnormal, because it is a Losp special operator, akin to a Lisp special form.

Assignments, flow control statements, lambda definitions; these are all implemented as special operators in Losp.

# How It's Going

Broadly speaking, I like Losp. I don't know that I made all the right decisions, but I think I made decisions that were at least *okay*. Would I have been better served to just bolt a JavaScript interpreter onto my project and point it at some `.js` files? Quite possibly. As is often the case, there is something to be said about the journey and that, at the end of the journey, I can say "holy crap, I actually made a working, mildly novel Lisp dialect... please hire me. please? someone? no, not you, you have bad ethics. crap, I actually need the money... now *I* have bad ethics."

Curse you, Losp, and the hypothetical bad ethics you may lead me to!
