using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Functions.Options;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Tracing
{
    public class AutoRestTracingInterceptor : IServiceClientTracingInterceptor
    {
        private readonly TracingOptions _options;
        private readonly string[] _traceIgnore;

        public AutoRestTracingInterceptor(TracingOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (!Directory.Exists(_options.TraceFolder))
            {
                Directory.CreateDirectory(_options.TraceFolder);
            }

            if (!string.IsNullOrEmpty(_options.TraceIgnore))
            {
                _traceIgnore = _options.TraceIgnore.Split(";", StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                _traceIgnore = new string[0];
            }

        }

        public void Configuration(string source, string name, string value)
        {
            // not implemented as not used
        }

        public void EnterMethod(string invocationId, object instance, string method, IDictionary<string, object> parameters)
        {
            var paramLines = parameters.Select(kvp => kvp.Key + "=" + kvp.Value != null ? kvp.Value.ToString() : "null");
            var logParams = string.Join("; ", paramLines);
            var line = GetLogLine($"{nameof(EnterMethod)} - {method}", "Params", logParams);
            WriteLogLine(invocationId, line, false);
        }

        public void ExitMethod(string invocationId, object returnValue)
        {
            var line = GetLogLine(nameof(ExitMethod), "Returns", returnValue != null ? returnValue.ToString() : "null");
            WriteLogLine(invocationId, line);
        }

        public void Information(string message)
        {
            // not implemented as not used, no invocationId
        }

        public void ReceiveResponse(string invocationId, HttpResponseMessage response)
        {
            string responseContent = $"({response.StatusCode})";
            if (response.Content != null)
            {
                responseContent += " - " + GetContent(response.Content, response.RequestMessage.RequestUri.ToString());
            }
            var line = GetLogLine(nameof(ReceiveResponse), "Content", responseContent);
            WriteLogLine(invocationId, line);
        }

        public void SendRequest(string invocationId, HttpRequestMessage request)
        {
            string requestContent = $"({request.Method.ToString()}) - {request.RequestUri.ToString()}";
            if (request.Content != null)
            {
                requestContent += " - " + GetContent(request.Content, request.RequestUri.ToString());
            }
            var line = GetLogLine(nameof(SendRequest), "Content", requestContent);
            WriteLogLine(invocationId, line);
        }

        public void TraceError(string invocationId, Exception exception)
        {
            var line = GetLogLine(nameof(TraceError), "Error", exception.Message);
            WriteLogLine(invocationId, line);
        }

        private string GetContent(HttpContent content, string requestUrl)
        {
            // ignore any content containing anything from the TraceIgnore
            if (_traceIgnore.Any(t => requestUrl.Contains(t, StringComparison.OrdinalIgnoreCase)))
            {
                return "[Content Hidden]";
            }
            else
            {
                return content.ReadAsStringAsync().Result;
            }
        }

        private string GetLogLine(string methodName, string additionalDataLabel, string additionalDataValue)
        {
            return $"[{DateTime.Now.AsDateTimeString()}] {methodName} - {additionalDataLabel}: {additionalDataValue}";
        }

        private void WriteLogLine(string invocationId, string line, bool append = true)
        {
            try
            {
                using (var writer = new StreamWriter(Path.Combine(_options.TraceFolder, $"{invocationId}.txt"), append))
                {
                    writer.WriteLine(line);
                }
            }
            catch
            {
                // allow for potential write failures by catching any errors
            }
        }
    }
}
