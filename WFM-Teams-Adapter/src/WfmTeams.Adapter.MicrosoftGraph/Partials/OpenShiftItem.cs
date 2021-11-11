// ---------------------------------------------------------------------------
// <copyright file="OpenShiftItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public partial class OpenShiftItem : IEquatable<OpenShiftItem>
    {
        public override bool Equals(object obj)
        {
            return Equals(obj as OpenShiftItem);
        }

        public bool Equals(OpenShiftItem other)
        {
            return other != null
                && DisplayName == other.DisplayName
                && Notes == other.Notes
                && StartDateTime == other.StartDateTime
                && EndDateTime == other.EndDateTime
                && Theme == other.Theme
                && Activities.SequenceEqual(other.Activities);
        }

        public override int GetHashCode()
        {
            var hashCode = -1050323901;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DisplayName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Notes);
            hashCode = hashCode * -1521134295 + StartDateTime.GetHashCode();
            hashCode = hashCode * -1521134295 + EndDateTime.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Theme);
            hashCode = hashCode * -1521134295 + EqualityComparer<IList<ShiftActivity>>.Default.GetHashCode(Activities);
            return hashCode;
        }
    }
}
