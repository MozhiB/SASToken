using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SasTokenGeneration.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SasTokenGeneration.Services
{
    public interface IGithubService
    {
        Task<string> generateTokenAsync(ExecutionContext context);
        Task<string> SendRequest(string requestUrl, string tokenDetails, HttpContentBody content);
        string GenerateSasToken();
    }
}