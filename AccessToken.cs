using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SasTokenGeneration.Models
{
    public class AccessToken
    {
        public string token { get; set; }
        public string repository_selection { get; set; }
        public Permissions permissions { get; set; }
        public string expires_at { get; set; }
    }
    public class Permissions
    {
        public string organization_events { get; set; }
        public string organization_hooks { get; set; }
        public string organization_secrets { get; set; }
        public string actions_variables { get; set; }
        public string checks { get; set; }
        public string environments { get; set; }
        public string metadata { get; set; }
        public string repository_hooks { get; set; }

        public string secrets { get; set; }
    }
    public class HttpContentBody
    {
        public string encrypted_value { get; set; }
        public string key_id { get; set; }
    }
}

