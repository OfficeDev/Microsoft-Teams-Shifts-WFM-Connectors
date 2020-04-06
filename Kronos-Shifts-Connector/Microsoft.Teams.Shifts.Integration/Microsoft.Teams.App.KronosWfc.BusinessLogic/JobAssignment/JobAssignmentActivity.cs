// <copyright file="JobAssignmentActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.JobAssignment
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Service;

    /// <summary>
    /// Job assignment activity class.
    /// </summary>
    [Serializable]
    public class JobAssignmentActivity : IJobAssignmentActivity
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IApiHelper apiHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobAssignmentActivity"/> class.
        /// </summary>
        /// <param name="telemetryClient">Having the telemetry capturing mechanism.</param>
        /// <param name="apiHelper">API helper to fetch tuple response by post soap requests.</param>
        public JobAssignmentActivity(TelemetryClient telemetryClient, IApiHelper apiHelper)
        {
            this.telemetryClient = telemetryClient;
            this.apiHelper = apiHelper;
        }

        /// <summary>
        /// Get Job Assignments.
        /// </summary>
        /// <param name="endPointUrl">End Point Url.</param>
        /// <param name="personNumber">Person Number.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="jSession">J Session.</param>
        /// <returns>Job Assignment response.</returns>
        public async Task<Models.ResponseEntities.JobAssignment.Response> GetJobAssignmentAsync(Uri endPointUrl, string personNumber, string tenantId, string jSession)
        {
            try
            {
                string xmlJobAssignReq = this.CreateJobAssignRequest(personNumber);
                var tupleJobAssignResponse = await this.apiHelper.SendSoapPostRequestAsync(
                    endPointUrl,
                    ApiConstants.SoapEnvOpen,
                    xmlJobAssignReq,
                    ApiConstants.SoapEnvClose,
                    jSession).ConfigureAwait(false);

                Models.ResponseEntities.JobAssignment.Response response = this.ProcessJobAssignResponse(tupleJobAssignResponse.Item1);

                return response;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return null;
            }
        }

        /// <summary>
        /// Create job assignment request.
        /// </summary>
        /// <param name="personNumber">Person Number.</param>
        /// <returns>job assign request.</returns>
        private string CreateJobAssignRequest(string personNumber)
        {
            Models.RequestEntities.JobAssignment.Request request = new Models.RequestEntities.JobAssignment.Request
            {
                JobAssign = new Models.RequestEntities.JobAssignment.JobAssignmentReq
                {
                    Ident = new Models.RequestEntities.JobAssignment.Identity
                    {
                        PersonIdentit = new Models.RequestEntities.JobAssignment.PersonIdentity
                        {
                            PersonNumber = personNumber,
                        },
                    },
                },
                Action = ApiConstants.LoadAction,
            };
            return request.XmlSerialize();
        }

        /// <summary>
        /// Process Job Assign Response.
        /// </summary>
        /// <param name="strResponse">Response string.</param>
        /// <returns>Job Assignment Response.</returns>
        private Models.ResponseEntities.JobAssignment.Response ProcessJobAssignResponse(string strResponse)
        {
            XDocument xDoc = XDocument.Parse(strResponse);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(ApiConstants.Response, StringComparison.Ordinal));
            return XmlConvertHelper.DeserializeObject<Models.ResponseEntities.JobAssignment.Response>(xResponse.ToString());
        }
    }
}