using CPService.Config;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace CPService.Tasks.Exchange
{
    public class ExchPowershell : IDisposable
    {
        private bool _disposed = false;

        internal WSManConnectionInfo _connection;
        internal Runspace _runspace;
        internal PowerShell _powershell;

        private readonly ILog logger = LogManager.GetLogger("Exchange Powershell");

        public ExchPowershell()
        {
            string uri = string.Format("https://{0}/powershell", ServiceSettings.ExchangeServer);
            this._connection = GetConnection(uri, ServiceSettings.Username, ServiceSettings.Password, ServiceSettings.ExchangeConnection == "Kerberos" ? true : false);

            _runspace = RunspaceFactory.CreateRunspace(_connection);
            _runspace.Open();

            _powershell = PowerShell.Create();
            _powershell.Runspace = _runspace;
        }

        private WSManConnectionInfo GetConnection(string uri, string username, string password, bool kerberos)
        {
            SecureString pwd = new SecureString();
            foreach (char x in password)
                pwd.AppendChar(x);

            PSCredential ps = new PSCredential(username, pwd);

            WSManConnectionInfo wsinfo = new WSManConnectionInfo(new Uri(uri), "http://schemas.microsoft.com/powershell/Microsoft.Exchange", ps);
            wsinfo.SkipCACheck = true;
            wsinfo.SkipCNCheck = true;
            wsinfo.SkipRevocationCheck = true;
            wsinfo.OpenTimeout = 9000;
            wsinfo.MaximumConnectionRedirectionCount = 1;

            if (kerberos)
                wsinfo.AuthenticationMechanism = AuthenticationMechanism.Kerberos;
            else
                wsinfo.AuthenticationMechanism = AuthenticationMechanism.Basic;

            return wsinfo;
        }

        internal long GetExchangeBytes(string data)
        {
            // Should be in this format: "768 MB (805,306,386 bytes)"
            //logger.DebugFormat("Parsing Exchange bytes for {0}", data);
            int startIndex = data.IndexOf("(");
            int endIndex = data.LastIndexOf(")");

            //logger.DebugFormat("Start index of {0} is {1} and end index of {2}", data, startIndex, endIndex);
            string subString = data.Substring(startIndex + 1, endIndex - startIndex - 1);

            //logger.DebugFormat("Substring of {0} is {1}", data, subString);
            string[] numbersOnly = subString.Split(new[] { "bytes" }, StringSplitOptions.RemoveEmptyEntries);

            //logger.DebugFormat("Numbers only is {0}", numbersOnly[0].Trim());
            return long.Parse(numbersOnly[0].Trim(), NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
        }

        #region Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (_powershell != null)
                    _powershell.Dispose();

                if (_runspace != null)
                    _runspace.Dispose();

                _connection = null;
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ExchPowershell()
        {
            Dispose(false);
        }
        #endregion
    }
}
