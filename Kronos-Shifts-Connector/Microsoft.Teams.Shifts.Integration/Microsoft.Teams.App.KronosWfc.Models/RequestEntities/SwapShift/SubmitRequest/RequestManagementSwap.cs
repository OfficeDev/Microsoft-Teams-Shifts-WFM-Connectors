// <copyright file="RequestManagementSwap.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;

    /// <summary>
    /// Employee request for swapshift.
    /// </summary>
#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable

    public class RequestManagementSwap
#pragma warning restore CA1052 // Static holder types should be Static or NotInheritable
    {
        /// <summary>
        /// This models the request.
        /// </summary>
        [XmlRoot]
#pragma warning disable CA1034 // Nested types should not be visible
        public class Request
#pragma warning restore CA1034 // Nested types should not be visible
        {
            /// <summary>
            /// Gets or sets Request management.
            /// </summary>
            [XmlElement("RequestMgmt")]
            public RequestMgmt RequestMgmt { get; set; }

            /// <summary>
            /// Gets or sets Action associated with request.
            /// </summary>
            [XmlAttribute]
            public string Action { get; set; }
        }

        /// <summary>
        /// This models the RequsestMgmt.
        /// </summary>
#pragma warning disable CA1034 // Nested types should not be visible

        public class RequestMgmt
#pragma warning restore CA1034 // Nested types should not be visible
        {
            /// <summary>
            /// Gets or sets Query date span.
            /// </summary>
            [XmlAttribute]
            public string QueryDateSpan { get; set; }

            /// <summary>
            /// Gets or sets Employee details.
            /// </summary>
            [XmlElement("Employees")]
            public Employee Employees { get; set; }

            /// <summary>
            /// Gets or sets Swap Shift Request Ids.
            /// </summary>
            [XmlElement("RequestIds")]
            public RequestIds RequestIds { get; set; }

            /// <summary>
            /// Gets or sets Swap Shift Request status changes.
            /// </summary>
            [XmlElement("RequestStatusChanges")]
            public RequestStatusChanges RequestStatusChanges { get; set; }
        }

        /// <summary>
        /// This models the RequestStatusChanges.
        /// </summary>
#pragma warning disable CA1034 // Nested types should not be visible

        public class RequestStatusChanges
#pragma warning restore CA1034 // Nested types should not be visible
        {
            /// <summary>
            /// Gets or sets Swap Shift Request status change.
            /// </summary>
            [XmlElement("RequestStatusChange")]
#pragma warning disable CA1819 // Properties should not return arrays
            public RequestStatusChange[] RequestStatusChange { get; set; }

#pragma warning restore CA1819 // Properties should not return arrays
        }

        /// <summary>
        /// This class models the RequestStatusChange.
        /// </summary>
#pragma warning disable CA1034 // Nested types should not be visible

        public class RequestStatusChange
#pragma warning restore CA1034 // Nested types should not be visible
        {
            /// <summary>
            /// Gets or sets Swap Shift Request Id.
            /// </summary>
            [XmlAttribute]
            public string RequestId { get; set; }

            /// <summary>
            /// Gets or sets Swap Shift status name.
            /// </summary>
            [XmlAttribute]
            public string ToStatusName { get; set; }

            /// <summary>
            /// Gets or sets Swap Shift Request comments.
            /// </summary>
            [XmlElement("Comments")]
            public List<Comment> Comments { get; set; }
        }

        /// <summary>
        /// This class models the Employee.
        /// </summary>
#pragma warning disable CA1034 // Nested types should not be visible

        public class Employee
#pragma warning restore CA1034 // Nested types should not be visible
        {
            /// <summary>
            /// Gets or sets Person Identities.
            /// </summary>
            [XmlElement("PersonIdentity")]
            public PersonIdentity PersonIdentity { get; set; }
        }

        /// <summary>
        /// This class models the PersonIdentity.
        /// </summary>
#pragma warning disable CA1034 // Nested types should not be visible

        public class PersonIdentity
#pragma warning restore CA1034 // Nested types should not be visible
        {
            /// <summary>
            /// Gets or sets Person number.
            /// </summary>
            [XmlAttribute]
            public string PersonNumber { get; set; }
        }

        /// <summary>
        /// This class models the RequestIds.
        /// </summary>
#pragma warning disable CA1034 // Nested types should not be visible

        public class RequestIds
#pragma warning restore CA1034 // Nested types should not be visible
        {
            /// <summary>
            /// Gets or sets Request Ids.
            /// </summary>
            [XmlElement("RequestId")]
#pragma warning disable CA1819 // Properties should not return arrays
            public RequestId[] RequestId { get; set; }

#pragma warning restore CA1819 // Properties should not return arrays
        }

        /// <summary>
        /// This class models the RequestId.
        /// </summary>
#pragma warning disable CA1034 // Nested types should not be visible

        public class RequestId
#pragma warning restore CA1034 // Nested types should not be visible
        {
            /// <summary>
            /// Gets or sets Swap Shift Request Id.
            /// </summary>
            [XmlAttribute]
            public string Id { get; set; }
        }
    }
}