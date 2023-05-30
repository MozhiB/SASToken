using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using SasTokenGeneration.Services;

namespace SasTokenGeneration
{
    public class SasTokenTrigger
    {
        
        private readonly IGithubService _service;
        private readonly ILogger<SasTokenTrigger> _log;
        public SasTokenTrigger(IGithubService service,ILogger<SasTokenTrigger> log)
        {            
            _service = service;
            _log = log;
        }
        [FunctionName("SasToken")]
        public void Run([TimerTrigger("0 0 0 26 * *")] TimerInfo myTimer, ExecutionContext context)
        {
            //"0 30 7 */28 * *" //"0 12 28 * *"
            //0 0 0 */28 * *
            _log.LogInformation("Run", "Method");
            try
            {
                var tokenResponse= _service.generateTokenAsync(context);
                if (tokenResponse != null) { _log.LogInformation($"TokenResponse: {tokenResponse}"); }
                else _log.LogInformation("Function App Failed");

            }
            catch (Exception ex) {
                 _log.LogError($"Timer trigger function Failed at: {DateTime.Now}");
                 _log.LogError($"Exception: {ex.Message}");
            }
            _log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            //  return new string("Response from function with injected dependencies.");
        }

    }
}
