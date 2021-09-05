// ---------------------------------------------------------------------------
// <copyright file="BlueYonderBaseService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Services
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Localization;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;
    using WfmTeams.Connector.BlueYonder.Models;
    using WfmTeams.Connector.BlueYonder.Options;

    public abstract class BlueYonderBaseService
    {
        protected readonly BlueYonderPersonaOptions _options;
        protected readonly IStringLocalizer<BlueYonderConfigService> _stringLocalizer;
        private readonly ISecretsService _secretsService;
        private readonly IBlueYonderClientFactory _clientFactory;
        private CredentialsModel _credentials;

        protected BlueYonderBaseService(BlueYonderPersonaOptions options, ISecretsService secretsService, IBlueYonderClientFactory clientFactory, IStringLocalizer<BlueYonderConfigService> stringLocalizer)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
            _stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
        }

        protected async Task<IBlueYonderClient> CreatePublicClientAsync()
        {
            if (_credentials == null)
            {
                _credentials = await _secretsService.GetCredentialsAsync().ConfigureAwait(false);
                _credentials.BaseAddress = _options.BlueYonderBaseAddress;
            }

            return _clientFactory.CreatePublicClient(_credentials, BYConstants.WfmProviderId, _options.RetailWebApiPath);
        }

        protected IBlueYonderClient CreateEssPublicClient(EmployeeModel employee)
        {
            var credentialsModel = CredentialsModel.FromLoginName(employee.WfmLoginName);
            credentialsModel.BaseAddress = _options.FederatedAuthBaseAddress;

            return _clientFactory.CreateUserClient(credentialsModel, employee.WfmEmployeeId, _options.EssApiPath);
        }

        protected IBlueYonderClient CreateSiteManagerPublicClient(EmployeeModel employee)
        {
            var credentialsModel = CredentialsModel.FromLoginName(employee.WfmLoginName);
            credentialsModel.BaseAddress = _options.FederatedAuthBaseAddress;

            return _clientFactory.CreateUserClient(credentialsModel, employee.WfmEmployeeId, _options.SiteManagerApiPath);
        }
    }
}
