using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Renci.SshNet;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace IFTTT_RunSshCommand
{
    public static class Functions
    {
        #region Run SSH Command function

        [FunctionName("run_ssh_command")]
        public static async Task<IActionResult> RunSshCommand([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "actions/run_ssh_command")] HttpRequest req, ILogger log, ExecutionContext context)
        {
            try
            {
                // Make sure the request was send from the IFTTT service
                if (req.Headers["IFTTT-Service-Key"] != GetIFTTTServiceKey(context))
                    return CreateErrorResponse("Unable to validate IFTTT Service Key", StatusCodes.Status401Unauthorized);

                //
                // Parse the request
                //
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                JObject jObject = JObject.Parse(requestBody);

                IActionResult errorActionResult = null;

                //
                // Load all the parameters
                //
                if (!TryLoadActionField(jObject, "hostname", out string hostname, out errorActionResult)) return errorActionResult;

                if (!TryLoadActionField(jObject, "port", out string stringPort, out errorActionResult)) return errorActionResult;
                if (!int.TryParse(stringPort, out int port))
                    return CreateErrorResponse($"Unable to parse action field port value '{stringPort}' as an integer", StatusCodes.Status400BadRequest);

                if (!TryLoadActionField(jObject, "username", out string username, out errorActionResult)) return errorActionResult;

                if (!TryLoadActionField(jObject, "password", out string password, out errorActionResult)) return errorActionResult;

                if (!TryLoadActionField(jObject, "command", out string command, out errorActionResult)) return errorActionResult;

                //
                // Connect the SSH Client, and send the request
                //
                if (!IsTestRequest(hostname))
                {
                    using (var client = new SshClient(hostname, port, username, password))
                    {
                        client.Connect();
                        SshCommand sshCommand = client.RunCommand(command);
                        string execResult = sshCommand.Result;
                        client.Disconnect();

                        log.LogInformation($"Command executed with result {execResult}");
                    }
                }

                //
                // Create the response, and send it
                //
                dynamic result = new JObject();
                result.data = new JArray();
                (result.data as JArray).Add(new JObject());
                result.data[0].id = "1";

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Exception: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        private static bool TryLoadActionField(JObject request, string actionFieldName, out string actionFieldValue, out IActionResult errorActionResult)
        {
            actionFieldValue = request["actionFields"]?[actionFieldName]?.ToString();

            if (string.IsNullOrEmpty(actionFieldValue))
            {
                errorActionResult = CreateErrorResponse($"Unable to find actionFields {actionFieldName}", StatusCodes.Status400BadRequest);
                return false;
            }

            errorActionResult = null;
            return true;
        }

        #endregion

        #region Test setup function

        /// <summary>
        /// Required by IFTTT to validate the endpoint
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        [FunctionName("testsetup")]
        public static async Task<IActionResult> RunTestSetup([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "test/setup")] HttpRequest req, ILogger log, ExecutionContext context)
        {
            // Make sure the request was send from the IFTTT service
            if (req.Headers["IFTTT-Service-Key"] != GetIFTTTServiceKey(context))
                return CreateErrorResponse("Unable to validate IFTTT Service Key", StatusCodes.Status401Unauthorized);

            dynamic result = new JObject();
            result.data = new JObject();
            result.data.accessToken = "Test";

            result.data.samples = new JObject();
            result.data.samples.actions = new JObject();
            result.data.samples.actions.run_ssh_command = new JObject();
            result.data.samples.actions.run_ssh_command.hostname = "testhost";
            result.data.samples.actions.run_ssh_command.port = "22";
            result.data.samples.actions.run_ssh_command.username = "testuser";
            result.data.samples.actions.run_ssh_command.password = "testpassword";
            result.data.samples.actions.run_ssh_command.command = "testcommand";
            
            return new OkObjectResult(result);
        }

        private static bool IsTestRequest(string hostname)
        {
            return hostname == "testhost";
        }

        #endregion

        [FunctionName("status")]
        public static async Task<IActionResult> RunStatus([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "status")] HttpRequest req, ILogger log, ExecutionContext context)
        {
            // Make sure the request was send from the IFTTT service
            if (req.Headers["IFTTT-Service-Key"] != GetIFTTTServiceKey(context))
                return CreateErrorResponse("Unable to validate IFTTT Service Key", StatusCodes.Status401Unauthorized);

            return new OkObjectResult("Ok");
        }

        #region Utils

        private static string GetIFTTTServiceKey(ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                             .SetBasePath(context.FunctionAppDirectory)
                             .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                             .AddEnvironmentVariables()
                             .Build();

            return config["IFTTT_SERVICE_KEY"];
        }

        /// <summary>
        /// Create an error response with the given http status code and error message
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        private static IActionResult CreateErrorResponse(string errorMessage, int statusCode)
        {
            dynamic result = new JObject();
            result.errors = new JArray();
            (result.errors as JArray).Add(new JObject());
            result.errors[0].message = errorMessage;

            return new ObjectResult(result) { StatusCode = statusCode };
        }

        #endregion
    }
}
