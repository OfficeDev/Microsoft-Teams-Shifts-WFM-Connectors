// <copyright file="AppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Common
{
    using System;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// AppSettings class.
    /// </summary>
    public class AppSettings
    {
        private readonly IKeyVaultHelper keyVaultHelper;
        private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettings" /> class.
        /// </summary>
        /// <param name="keyVaultHelper">KeyVaultHelper class object.</param>
        /// <param name="configuration">app settings configuration.</param>
        public AppSettings(IKeyVaultHelper keyVaultHelper, IConfiguration configuration)
        {
            if (keyVaultHelper is null)
            {
                throw new ArgumentNullException(nameof(keyVaultHelper));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            this.keyVaultHelper = keyVaultHelper;
            this.configuration = configuration;
            this.StorageConnectionString = this.keyVaultHelper.GetSecretByUri(this.configuration["KeyVault"] + "secrets/" + this.configuration["StorageConnectionString"]);
            this.RedisCacheConfiguration = this.keyVaultHelper.GetSecretByUri(this.configuration["KeyVault"] + "secrets/" + this.configuration["RedisCacheConfiguration"]);
            this.ClientSecret = this.keyVaultHelper.GetSecretByUri(this.configuration["KeyVault"] + "secrets/" + this.configuration["ClientSecret"]);
            this.WfmSuperUsername = this.keyVaultHelper.GetSecretByUri(this.configuration["KeyVault"] + "secrets/" + this.configuration["WfmSuperUsername"]);
            this.WfmSuperUserPassword = this.keyVaultHelper.GetSecretByUri(this.configuration["KeyVault"] + "secrets/" + this.configuration["WfmSuperUserPassword"]);
        }

        /// <summary>
        /// Gets Azure table storage connectionstring.
        /// </summary>
        public string StorageConnectionString { get; }

        /// <summary>
        /// Gets RedisCacheConfiguration.
        /// </summary>
        public string RedisCacheConfiguration { get; }

        /// <summary>
        /// Gets ClientSecret.
        /// </summary>
        public string ClientSecret { get; }

        /// <summary>
        /// Gets or sets WfmSuperUsername.
        /// </summary>
        public string WfmSuperUsername { get; set; }

        /// <summary>
        /// Gets or sets WfmSuperUserPassword.
        /// </summary>
        public string WfmSuperUserPassword { get; set; }

        /// <summary>
        /// Gets ClientId.
        /// </summary>
        public string ClientId
        {
            get => this.configuration["ClientId"];
        }

        /// <summary>
        /// Gets TenantId.
        /// </summary>
        public string TenantId
        {
            get => this.configuration["TenantId"];
        }

        // ********************************* API CONFIG ************************************

        /// <summary>
        /// Gets GraphApiUrl.
        /// </summary>
#pragma warning disable CA1056 // Uri properties should not be strings
        public string GraphApiUrl => this.configuration["GraphApiUrl"];
#pragma warning restore CA1056 // Uri properties should not be strings

        /// <summary>
        /// Gets TeamDepartmentMapping.
        /// </summary>
        public string TeamDepartmentMapping => this.configuration.GetValue<string>("TeamDepartmentMapping");

        /// <summary>
        /// Gets UserToUserMapping.
        /// </summary>
        public string UserToUserMapping => this.configuration.GetValue<string>("UserToUserMapping");

        /// <summary>
        /// Gets ShiftStartDate.
        /// </summary>
        public string ShiftStartDate => this.configuration["ShiftStartDate"];

        /// <summary>
        /// Gets ShiftEndDate.
        /// </summary>
        public string ShiftEndDate => this.configuration["ShiftEndDate"];

        /// <summary>
        /// Gets ShiftTheme.
        /// </summary>
        public string ShiftTheme => this.configuration["ShiftTheme"];

        /// <summary>
        /// Gets the TransferredShiftTheme.
        /// </summary>
        public string TransferredShiftTheme => this.configuration["TransferredShiftTheme"];

        /// <summary>
        /// Gets Instance.
        /// </summary>
        public string Instance => this.configuration["Instance"];

        /// <summary>
        /// Gets Domain.
        /// </summary>
        public string Domain => this.configuration["Domain"];

        /// <summary>
        /// Gets KronosTimeZone.
        /// </summary>
        public string KronosTimeZone => this.configuration["KronosTimeZone"];

        /// <summary>
        /// Gets ShiftsTimeZone.
        /// </summary>
        public string ShiftsTimeZone => this.configuration["ShiftsTimeZone"];

        /// <summary>
        /// Gets ProcessNumberOfUsersInBatch.
        /// </summary>
        public string ProcessNumberOfUsersInBatch => this.configuration["ProcessNumberOfUsersInBatch"];

        /// <summary>
        /// Gets ProcessNumberOfOrgJobsInBatch.
        /// </summary>
        public string ProcessNumberOfOrgJobsInBatch => this.configuration["ProcessNumberOfOrgJobsInBatch"];

        /// <summary>
        /// Gets ShiftMappingEntity.
        /// </summary>
        public string ShiftMappingEntity => this.configuration.GetValue<string>("ShiftMappingEntity");

        /// <summary>
        /// Gets ShiftMappingEntity.
        /// </summary>
        public string SwapShiftMappingEntity => this.configuration.GetValue<string>("SwapShiftMappingEntity");

        /// <summary>
        /// Gets OpenShiftTheme.
        /// </summary>
        public string OpenShiftTheme => this.configuration["OpenShiftTheme"];

        /// <summary>
        /// Gets Kronos query name.
        /// </summary>
        public string KronosUserDetailsQuery => this.configuration["KronosUserDetailsQuery"];

        /// <summary>
        /// Gets excel content type.
        /// </summary>
        public string ExcelContentType => this.configuration["ExcelContentType"];

        /// <summary>
        /// Gets template name.
        /// </summary>
        public string KronosShiftUserMappingTemplateName => this.configuration["KronosShiftUserMappingTemplateName"];

        /// <summary>
        /// Gets template container name.
        /// </summary>
        public string TemplatesContainerName => this.configuration["TemplatesContainerName"];

        /// <summary>
        /// Gets shift tempalte name.
        /// </summary>
        public string KronosShiftTeamDeptMappingTemplateName => this.configuration["KronosShiftTeamDeptMappingTemplateName"];

        /// <summary>
        /// Gets the Kronos query date span date format.
        /// </summary>
        public string KronosQueryDateSpanFormat => this.configuration["KronosQueryDateSpanFormat"];

        /// <summary>
        /// Gets the number of seconds to set the time to live when caching a kronos session token.
        /// </summary>
        public string AuthTokenCacheLifetimeInSeconds => this.configuration["AuthTokenCacheLifetimeInSeconds"];

        /// <summary>
        /// Gets whether manager CRUD within Teams is enabled.
        /// </summary>
        public string AllowManagersToModifyScheduleInTeams => this.configuration["AllowManagersToModifyScheduleInTeams"];

        /// <summary>
        /// Gets the number of org job path section sto use as the activity display name.
        /// </summary>
        public string NumberOfOrgJobPathSectionsForActivityName => this.configuration["NumberOfOrgJobPathSectionsForActivityName"];

        /// <summary>
        /// Gets the transferred shift display name value.
        /// </summary>
        public string TransferredShiftDisplayName => this.configuration["TransferredShiftDisplayName"];

        /// <summary>
        /// Gets the shift notes comment text value.
        /// </summary>
        public string ShiftNotesCommentText => this.configuration["ShiftNotesCommentText"];

        /// <summary>
        /// Gets the manager time off request comment text value.
        /// </summary>
        public string ManagerTimeOffRequestCommentText => this.configuration["ManagerTimeOffRequestCommentText"];

        /// <summary>
        /// Gets the sender time off request comment text value.
        /// </summary>
        public string SenderTimeOffRequestCommentText => this.configuration["SenderTimeOffRequestCommentText"];

        /// <summary>
        /// Gets the manager swap request comment text value.
        /// </summary>
        public string ManagerSwapRequestCommentText => this.configuration["ManagerSwapRequestCommentText"];

        /// <summary>
        /// Gets the sender swap request comment text value.
        /// </summary>
        public string SenderSwapRequestCommentText => this.configuration["SenderSwapRequestCommentText"];

        /// <summary>
        /// Gets the recipient swap request comment text value.
        /// </summary>
        public string RecipientSwapRequestCommentText => this.configuration["RecipientSwapRequestCommentText"];

        /// <summary>
        /// Gets the manager open shift request comment text value.
        /// </summary>
        public string ManagerOpenShiftRequestRequestCommentText => this.configuration["ManagerOpenShiftRequestCommentText"];

        // **************************************CONFIGURATION PROJECT*********************************************

        /// <summary>
        /// Gets CallbackPath.
        /// </summary>
        public string CallbackPath => this.configuration["CallbackPath"];

        /// <summary>
        /// Gets the IntegrationApiUrl.
        /// </summary>
#pragma warning disable CA1056 // Uri properties should not be strings
        public string IntegrationApiUrl => this.configuration["IntegrationApiUrl"];
#pragma warning restore CA1056 // Uri properties should not be strings

        /// <summary>
        /// Gets Authority.
        /// </summary>
        public string Authority => this.configuration.GetValue<string>("Authority");

        /// <summary>
        /// Gets PostLogoutRedirectUri.
        /// </summary>
#pragma warning disable CA1056 // Uri properties should not be strings
        public string PostLogoutRedirectUri => this.configuration.GetValue<string>("PostLogoutRedirectUri");
#pragma warning restore CA1056 // Uri properties should not be strings

        /// <summary>
        /// Gets ResponseType.
        /// </summary>
        public string ResponseType => this.configuration.GetValue<string>("ResponseType");

        /// <summary>
        /// Gets RedisCacheInstanceName.
        /// </summary>
        public string RedisCacheInstanceName => this.configuration.GetValue<string>("RedisCacheInstanceName");

        /// <summary>
        /// Gets the base address for the first time sync.
        /// </summary>
        public string BaseAddressFirstTimeSync => this.configuration["BaseAddressFirstTimeSync"];

        /// <summary>
        /// Gets the number of days in the past to sync.
        /// </summary>
        public string SyncFromPreviousDays => this.configuration["SyncFromPreviousDays"];

        /// <summary>
        /// Gets OrgJobPath.
        /// </summary>public string OrgJobPath => this.configuration["OrgJobPath"];
        public string SyncToNextDays => this.configuration["SyncToNextDays"];

        /// <summary>
        /// Gets the number of days in the future to sync swap shift eligiblity for.
        /// </summary>
        public string FutureSwapEligibilityDays => this.configuration["FutureSwapEligibilityDays"];

        /// <summary>
        /// Gets the configuration for the polling delay in the sync functionality.
        /// </summary>
        public string SyncDelayForPolling => this.configuration["SyncDelayForPolling"];

        /// <summary>
        /// Gets the configuration for the number of seconds to wait between deleting a shift in Teams
        /// And auto sharing the schedule.
        /// </summary>
        public string AutoShareScheduleWaitTime => this.configuration["AutoShareScheduleWaitTime"];

        /// <summary>
        /// Gets security group name for managers.
        /// </summary>
        public string AdSecurityGroupName => this.configuration["SecurityGroupName"];

        /// <summary>
        /// Gets security group id for managers.
        /// </summary>
        public string AdSecurityGroupId => this.configuration["SecurityGroupId"];

        /// <summary>
        /// Gets the correctedDateSpanForOutboundCalls.
        /// </summary>
        public string CorrectedDateSpanForOutboundCalls => this.configuration["CorrectedDateSpanForOutboundCalls"];

        // *********************************************************************************

        /// <summary>
        /// This method can be used to set secret in key vault.
        /// </summary>
        /// <param name="secretName">secret name.</param>
        /// <param name="secretValue">secret value.</param>
        /// <returns>boolean result.</returns>
        public bool SetConfigToKeyVault(string secretName, string secretValue)
        {
            var val = this.keyVaultHelper.SetKeyVaultSecret(this.configuration["KeyVault"], secretName, secretValue);
            if (val.Equals(secretValue, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }
    }
}