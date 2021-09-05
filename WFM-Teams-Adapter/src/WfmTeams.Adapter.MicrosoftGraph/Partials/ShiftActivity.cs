// ---------------------------------------------------------------------------
// <copyright file="ShiftActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Models
{
    using System;
    using System.Collections.Generic;

    public partial class ShiftActivity : IEquatable<ShiftActivity>
    {
        public override bool Equals(object obj)
        {
            return Equals(obj as ShiftActivity);
        }

        public bool Equals(ShiftActivity other)
        {
            return other != null
                && EqualityComparer<DateTime?>.Default.Equals(StartDateTime, other.StartDateTime)
                && EqualityComparer<DateTime?>.Default.Equals(EndDateTime, other.EndDateTime)
                && Code == other.Code
                && DisplayName == other.DisplayName
                && Theme == other.Theme;
        }

        public override int GetHashCode()
        {
            var hashCode = -833447283;
            hashCode = hashCode * -1521134295 + EqualityComparer<DateTime?>.Default.GetHashCode(StartDateTime);
            hashCode = hashCode * -1521134295 + EqualityComparer<DateTime?>.Default.GetHashCode(EndDateTime);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Code);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DisplayName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Theme);
            return hashCode;
        }
    }
}
