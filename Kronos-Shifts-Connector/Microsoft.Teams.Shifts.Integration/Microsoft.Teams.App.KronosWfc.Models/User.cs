// <copyright file="User.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// This class is used to create login request to Kronos.
    /// </summary>
    public class User
    {
        /// <summary>
        /// Gets or sets UserName.
        /// </summary>
        [Required]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets Password.
        /// </summary>
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets TenantId.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets TeamsUserId.
        /// </summary>
        public string TeamsUserId { get; set; }

        /// <summary>
        /// Gets or sets PersonNumber.
        /// </summary>
        public string PersonNumber { get; set; }

        /// <summary>
        /// Gets or sets ConversationId.
        /// </summary>
        public string ConversationId { get; set; }
    }
}