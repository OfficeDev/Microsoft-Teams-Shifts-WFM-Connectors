// <copyright file="LoginResponse.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models
{
    using System;

    /// <summary>
    /// This class is used to parse the response received from Kronos.
    /// </summary>
    [Serializable]
    public class LoginResponse
    {
        /// <summary>
        /// Gets or sets the Session Id.
        /// </summary>
        public string JsessionID { get; set; }

        /// <summary>
        /// Gets or sets the PersonNumber.
        /// </summary>
        public string PersonNumber { get; set; }

        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        public string Name { get; set; }
    }
}