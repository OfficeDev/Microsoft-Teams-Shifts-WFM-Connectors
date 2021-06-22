<<<<<<< HEAD:Kronos-Shifts-Connector/Microsoft.Teams.Shifts.Integration/Microsoft.Teams.App.KronosWfc.Models/RequestEntities/Common/RequestIds.cs
﻿// <copyright file="RequestIds.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Xml.Serialization;

    /// <summary>
    /// Model representing a list of Request Ids.
    /// </summary>
    public class RequestIds
    {
        /// <summary>
        /// Gets or Sets the Id for a request.
        /// </summary>
        [XmlAttribute]
        public string Id { get; set; }
    }
}
=======
﻿// <copyright file="PersonIdentityTag.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Xml.Serialization;

    /// <summary>
    /// The PersonIdentity tag.
    /// </summary>
    public class PersonIdentity
    {
        /// <summary>
        /// Gets or Sets the Person Number attribute.
        /// </summary>
        [XmlAttribute]
        public string PersonNumber { get; set;  }
    }
}
>>>>>>> master:Kronos-Shifts-Connector/Microsoft.Teams.Shifts.Integration/Microsoft.Teams.App.KronosWfc.Models/RequestEntities/TimeOffRequests/CancelTimeOff/PersonIdentity.cs
