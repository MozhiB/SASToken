using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SasTokenGeneration.Models
{
    public class GithubApiSettings
    {
        public string InstallationId { get; set; }
        public string PortalRepositoryId{ get; set; }
        public string ApiRepositoryId { get; set; }
        public string PublicKey { get; set; }
        public string PublicKeyId { get; set; }
        public string EnvironmentName { get; set; }
        public string SecretName { get; set; }
        public string BaseUrl { get; set; }
        public string Scheduled_Dev_Token { get; set; }
        public string Scheduled_Test_Token { get; set; }
        public string Sheduled_Prod_Token { get; set; }
        public string EnvironmentName_Scheduled { get; set; }
    }
}
