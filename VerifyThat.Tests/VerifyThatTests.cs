// <copyright file="VerifyThatTests.cs" company="Jason Diamond">
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
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using NUnit.Framework;

    [TestFixture]
    public class Verify_That_Tests
    {
        [Test]
        public void Local_variable_names_are_reported()
        {
            int foo = 1; // This can't be a constant or it will appear as a constant in the expression tree.

            GetFailureMessage(() => foo == 2);

            Verify.That(() => this.message == "Expected foo to be 2 but was 1" );
        }

        [Test]
        public void Field_names_are_reported()
        {
            GetFailureMessage(() => this.field == 2);

            Verify.That(() => this.message == "Expected field to be 2 but was 1");
        }

        [Test]
        public void Property_names_are_reported()
        {
            GetFailureMessage(() => this.Property == 2);

            Verify.That(() => this.message == "Expected Property to be 2 but was 1");
        }

        [Test]
        public void Static_property_names_are_reported()
        {
            GetFailureMessage(() => StaticProperty == 2);

            Verify.That(() => this.message == "Expected " + this.GetType().Name + ".StaticProperty to be 2 but was 1");
        }

        [Test]
        public void Method_names_are_reported()
        {
            GetFailureMessage(() => this.Method(1) == 2);

            Verify.That(() => this.message == "Expected Method(1) to be 2 but was 1");
        }

        [Test]
        public void Method_with_multiple_parameters_are_reported_correctly()
        {
            GetFailureMessage(() => this.Method(1, 2) == 4);

            Verify.That(() => this.message == "Expected Method(1, 2) to be 4 but was 3");
        }

        [Test]
        public void Static_method_names_are_reported()
        {
            GetFailureMessage(() => StaticMethod(1) == 2);

            Verify.That(() => this.message == "Expected " + this.GetType().Name + ".StaticMethod(1) to be 2 but was 1");
        }

        [Test]
        public void Strings_are_quoted()
        {
            GetFailureMessage(() => "1".Trim() == "2"); // Trim is there to avoid a constant in the expression tree.

            Verify.That(() => this.message == "Expected \"1\".Trim() to be \"2\" but was \"1\"");
        }

        [Test]
        public void Special_characters_in_strings_are_escaped()
        {
            GetFailureMessage(() => "\"\\\0\a\b\f\n\r\t\v".ToString() == "x"); // ToString is there to avoid a constant in the expression tree.

            Verify.That(() => this.message == "Expected \"\\\"\\\\\\0\\a\\b\\f\\n\\r\\t\\v\".ToString() to be \"x\" but was \"\\\"\\\\\\0\\a\\b\\f\\n\\r\\t\\v\"");
        }

        [Test]
        public void Invoking_methods_on_int_literals_still_reports_the_literal()
        {
            GetFailureMessage(() => 1.ToString() == "2");

            Verify.That(() => this.message == "Expected 1.ToString() to be \"2\" but was \"1\"");
        }

        [Test]
        public void Invoking_methods_on_string_literals_still_reports_the_literal()
        {
            GetFailureMessage(() => "1".Trim() == "2");

            Verify.That(() => this.message == "Expected \"1\".Trim() to be \"2\" but was \"1\"");
        }

        [Test]
        public void Array_indexers_get_reported_correctly()
        {
            var foo = new[] { 1, 2, 3 };

            GetFailureMessage(() => foo[0] == 2);

            Verify.That(() => this.message == "Expected foo[0] to be 2 but was 1");
        }

        [Test]
        public void Array_lengths_get_reported_correctly()
        {
            var foo = new[] { 1, 2, 3 };

            GetFailureMessage(() => foo.Length == 2);

            Verify.That(() => this.message == "Expected foo.Length to be 2 but was 3");
        }

        [Test]
        public void Object_indexers_get_reported_correctly()
        {
            var foo = new MyObject();

            GetFailureMessage(() => foo[1] == 2);

            Verify.That(() => this.message == "Expected foo[1] to be 2 but was 1");
        }

        [Test]
        public void Typeof_gets_reported_correctly()
        {
            var foo = new MyObject();

            GetFailureMessage(() => foo.GetType() == typeof(string));

            Verify.That(() => this.message == "Expected foo.GetType() to be typeof(string) but was typeof(MyObject)");
        }

        [Test]
        public void The_is_operator_works()
        {
            object foo = new MyObject();

            GetFailureMessage(() => foo is string);

            Verify.That(() => this.message == "Expected foo to be typeof(string) but was typeof(MyObject)");
        }

        [Test]
        public void Date_time_values_are_reported_in_iso_format()
        {
            DateTime today = DateTime.MinValue;

            GetFailureMessage(() => today == DateTime.Today);

            Verify.That(() => this.message == "Expected today to be DateTime.Today but was 0001-01-01T00:00:00");
        }

        [Test]
        public void Casts_get_reported_correctly()
        {
            object foo = 1;

            GetFailureMessage(() => (int)foo == 2);

            Verify.That(() => this.message == "Expected (int)foo to be 2 but was 1");
        }

        [Test]
        public void As_gets_reported_correctly()
        {
            object foo = "1";

            GetFailureMessage(() => foo as string == "2");

            Verify.That(() => this.message == "Expected foo as string to be \"2\" but was \"1\"");
        }

        [Test]
        public void Not_gets_reported_correctly()
        {
            bool foo = true;

            GetFailureMessage(() => !foo);

            Verify.That(() => this.message == "Expected !foo to be true but was false");
        }

        [Test]
        public void Negate_gets_reported_correctly()
        {
            int foo = 1;

            GetFailureMessage(() => -foo == 2);

            Verify.That(() => this.message == "Expected -foo to be 2 but was -1");
        }

        [Test]
        public void Object_initializers_get_reported_correctly()
        {
            var foo = new MyObject { IntProperty = 1, StringProperty = "a" };

            GetFailureMessage(() => foo == new MyObject { IntProperty = 2, StringProperty = "b" });

            Verify.That(() => this.message == "Expected foo to be new MyObject { IntProperty = 2, StringProperty = \"b\" } but was [1, a]");
        }

        [Test]
        public void Conditional_operator_gets_reported_correctly()
        {
            var foo = 1;

            GetFailureMessage(() => (foo == 1 ? "a" : "b") == "c");

            Verify.That(() => this.message == "Expected foo == 1 ? \"a\" : \"b\" to be \"c\" but was \"a\"");
        }

        [Test]
        public void Extension_methods_get_reported_correctly()
        {
            var foo = new[] { 1, 2, 3 };

            GetFailureMessage(() => foo.First() == 2);

            Verify.That(() => this.message == "Expected foo.First() to be 2 but was 1");
        }

        [Test]
        public void Extension_methods_with_multiple_parameters_get_reported_correctly()
        {
            var foo = new[] { 1, 2, 3 };

            GetFailureMessage(() => foo.Contains(4));

            Verify.That(() => this.message == "Expected foo.Contains(4) to be true but was false");
        }

        #region Members used in expressions

        private int field = 1;

        private int Property { get { return 1; } }

        private static int StaticProperty { get { return 1; } }

        private int Method(int value)
        {
            return value;
        }

        private int Method(int value1, int value2)
        {
            return value1 + value2;
        }

        private static int StaticMethod(int value)
        {
            return value;
        }

        private class MyObject
        {
            public int this[int i]
            {
                get { return i; }
            }

            public int IntProperty { get; set; }

            public string StringProperty { get; set; }

            public override string ToString()
            {
                return string.Format("[{0}, {1}]", this.IntProperty, this.StringProperty);
            }
        }

        #endregion

        #region Test helpers

        private string message;

        private void GetFailureMessage(Expression<Func<bool>> expression)
        {
            Verify.That(
                expression,
                m => this.message = m,
                (x, r, e, a) => string.Format("Expected {0} to {1} {2} but was {3}", x, r, e, a));
        }

        #endregion
    }
}
