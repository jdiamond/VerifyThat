VerifyThat is a really small library for doing assertions in your
unit tests. It requires .NET 3.5 or higher.

You can build the solution and reference the assembly or just add the
single VerifyThat.cs file to your unit test project.

You can use it with any unit test framework even though the examples
below refer to using it with NUnit. It has no dependencies on any
assemblies not included with .NET.

The only method you need to use is `Verify.That`:

    Verify.That(() => /* Put your assertion here! */);

It uses expression trees and "parses" them to figure out a meaningful
error message without forcing you to manually type one in.

Instead of doing this:

   Assert.AreEqual(42, foo, "foo was not 42!");

Or this:

   Assert.That(foo, Is.EqualTo(42), "foo was not 42!");

You can state your assertion in "normal" C# like this:

   Verify.That(() => foo == 42);

The only ugly part is that `() =>` bit in there, but there's nothing
we can do about that.

If foo is 13 instead of 42, the message you'll get is this:

 Expected: foo
    to be: 42
  but was: 13

With NUnit's assertions, it will tell you that 13 is not 42, but you
lose the fact that it was the variable named foo that you wanted to
have the value 42. This might be very useful information which is why
many people resort to manually creating messages to help NUnit report
them to them.

VerifyThat even supports complex expressions in the assertion:

   Verify.That(() => foo.Bar(baz).Quux + 1 == 42);

That could report something like this if the assertion failed:

 Expected: foo.Bar(baz).Quux + 1
    to be: 42
  but was: 43

There's still a lot to do so that it reports every type of expression
correctly, but it can already handle the majority of cases.

Feel free to fork the project on GitHub and submit a pull request
if you make any changes.

http://github.com/jdiamond/VerifyThat
