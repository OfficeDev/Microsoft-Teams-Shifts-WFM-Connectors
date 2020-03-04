// <copyright file="ConnectController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.Shifts.Encryption.Encryptors;
    using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI.Incoming;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Newtonsoft.Json;

    /// <summary>
    /// This controller will be responsible for listening for a request from Shifts.
    /// </summary>
    [Route("/v1/connect")]
    [ApiController]
    public class ConnectController : ControllerBase
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IConfigurationProvider configurationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectController"/> class.
        /// </summary>
        /// <param name="telemetryClient">The telemetry and logging mechanism.</param>
        /// <param name="configurationProvider">The configuration provider to get configuration settings.</param>
        public ConnectController(
            TelemetryClient telemetryClient,
            IConfigurationProvider configurationProvider)
        {
            this.telemetryClient = telemetryClient;
            this.configurationProvider = configurationProvider;
        }

        /// <summary>
        /// This method will properly decrypt the connection request that is coming from Microsoft Graph.
        /// </summary>
        /// <param name="secretKeyBytes">The symmetric key established, and having the necessary storage done.</param>
        /// <param name="request">The HTTP request that is coming in from Microsoft Graph.</param>
        /// <returns>A unit of execution that contains a type of <see cref="ConnectRequest"/>.</returns>
        public static async Task<ConnectRequest> DecryptConnectionRequest(
            byte[] secretKeyBytes,
            HttpRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string decryptedRequestBody = null;

            // Step 1 - using a memory stream for the processing of the request.
            using (MemoryStream ms = new MemoryStream())
            {
                await request.Body.CopyToAsync(ms).ConfigureAwait(false);
                byte[] encryptedRequestBytes = ms.ToArray();
                Aes256CbcHmacSha256Encryptor decryptor = new Aes256CbcHmacSha256Encryptor(secretKeyBytes);
                byte[] decryptedRequestBodyBytes = decryptor.Decrypt(encryptedRequestBytes);
                decryptedRequestBody = Encoding.UTF8.GetString(decryptedRequestBodyBytes);
            }

            // Step 2 - Parse the decrypted request into the correct model.
            return JsonConvert.DeserializeObject<ConnectRequest>(decryptedRequestBody);
        }

        /// <summary>
        /// The method to return an OK response when Shifts calls the Integration Service API.
        /// </summary>
        /// <returns>The OK response.</returns>
        public async Task<ActionResult> EstablishHandshake()
        {
            var establishHandshakeProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            // Step 1 - Obtain the secret from the database.
            var configurationEntities = await this.configurationProvider.GetConfigurationsAsync().ConfigureAwait(false);
            var configurationEntity = configurationEntities?.FirstOrDefault();

            byte[] secretKeyBytes = Encoding.UTF8.GetBytes(configurationEntity?.WorkforceIntegrationSecret);

            var connectModel = await DecryptConnectionRequest(secretKeyBytes, this.Request).ConfigureAwait(false);

            if (connectModel?.TenantId == configurationEntity?.TenantId &&
                connectModel?.UserId == configurationEntity?.AdminAadObjectId)
            {
                establishHandshakeProps.Add("TenantId", connectModel?.TenantId);
                establishHandshakeProps.Add("UserId", connectModel?.UserId);

                this.telemetryClient.TrackTrace(Resource.EstablishHandshake, establishHandshakeProps);

                return this.Ok();
            }

            return this.BadRequest();
        }
    }
}