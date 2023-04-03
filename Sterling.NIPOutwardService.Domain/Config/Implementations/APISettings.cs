using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sterling.NIPOutwardService.Domain.Config.Implementations
{
    public class APISettings
    {
        //public string NIPEncryptionSocketIP { get; set; }
        //public int NIPEncryptionSocketPort { get; set; }
        //public string NIPEncryptionSocketPassword { get; set; }
        public string NIPNIBSSService { get; set; }
        public int NIBSSNIPServiceCloseTimeoutInMinutes { get; set; }
        public int NIBSSNIPServiceOpenTimeoutInMinutes { get; set; }
        public int NIBSSNIPServiceReceiveTimeoutInMinutes { get; set; }
        public int NIBSSNIPServiceSendTimeoutInMinutes { get; set; }
        public int NIBSSNIPServiceMaxBufferPoolSize { get; set; }
        public int NIBSSNIPServiceMaxReceivedMessageSize { get; set; }
        public string NIBSSPublicKeyPath { get; set; }
        public string NIBSSPrivateKeyPath { get; set; }
        public string NIBSSPrivateKeyPassword { get; set; }
    }
}
