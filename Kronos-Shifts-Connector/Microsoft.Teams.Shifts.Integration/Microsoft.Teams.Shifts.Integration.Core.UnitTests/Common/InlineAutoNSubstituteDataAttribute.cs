using AutoFixture.Xunit2;

namespace Microsoft.Teams.Shifts.Integration.Core.UnitTests.Common
{
    public class InlineAutoNSubstituteDataAttribute : InlineAutoDataAttribute
    {
        public InlineAutoNSubstituteDataAttribute(params object[] objects) 
            : base(new AutoNSubstituteDataAttribute(), objects) { }
    }
}
