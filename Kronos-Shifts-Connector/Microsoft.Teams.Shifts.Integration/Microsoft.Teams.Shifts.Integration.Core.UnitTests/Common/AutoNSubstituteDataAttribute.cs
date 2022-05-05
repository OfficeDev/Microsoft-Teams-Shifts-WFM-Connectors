using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;

namespace Microsoft.Teams.Shifts.Integration.Core.UnitTests.Common
{
    public class AutoNSubstituteDataAttribute : AutoDataAttribute
    {
        public AutoNSubstituteDataAttribute()
            : base(() => new Fixture()
                .Customize(new AutoNSubstituteCustomization()))
        {
        }
    }
}