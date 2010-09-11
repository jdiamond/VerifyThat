using System;
using System.Linq.Expressions;

namespace VerifyThat.Tests
{
    public class VerifyThatTestsBase
    {
        protected string message;

        protected void GetFailureMessage(Expression<Func<bool>> expression)
        {
            Verify.That(
                expression,
                m => this.message = m,
                (x, b, e, w, a) => string.Format("Expected {0} to {1} {2} but {3} {4}", x, b, e, w, a));
        }
    }
}