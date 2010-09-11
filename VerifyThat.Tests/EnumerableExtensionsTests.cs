// <copyright file="EnumerableExtensionsTests.cs" company="Jason Diamond">
//
// Copyright (c) 2010 Jason Diamond
//
// This source code is released under the MIT License.
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//
// </copyright>

namespace VerifyThat.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class EnumerableExtensionsTests : VerifyThatTestsBase
    {
        [Test]
        public void IsEmpty()
        {
            var foo = new[] { 1, 2, 3 };

            GetFailureMessage(() => foo.IsEmpty());

            Verify.That(() => this.message == "Expected foo to be empty but contained 3 items");
        }

        [Test]
        public void IsSubsetOf_NoElementsIntersect()
        {
            var foo = new[] { 1, 2, 3 };

            GetFailureMessage(() => foo.IsSubsetOf(new[] { 4, 5, 6 }));

            Verify.That(() => this.message == "Expected foo to be subset of {4, 5, 6} but difference was {1, 2, 3}");
        }

        [Test]
        public void IsSubsetOf_OneElementIntersects()
        {
            var foo = new[] { 1, 2, 4 };

            GetFailureMessage(() => foo.IsSubsetOf(new[] { 4, 5, 6 }));

            Verify.That(() => this.message == "Expected foo to be subset of {4, 5, 6} but difference was {1, 2}");
        }

        [Test]
        public void IsSubsetOf_TwoElementsIntersect()
        {
            var foo = new[] { 1, 4, 5 };

            GetFailureMessage(() => foo.IsSubsetOf(new[] { 4, 5, 6 }));

            Verify.That(() => this.message == "Expected foo to be subset of {4, 5, 6} but difference was {1}");
        }
    }
}