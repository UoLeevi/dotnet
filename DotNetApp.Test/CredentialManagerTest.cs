using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace DotNetApp.Test
{
    public class CredentialManagerTest
    {
        [Fact]
        public void CanWriteAndReadGenericCredential()
        {
            string targetName = "CanReadAndWriteCredentials";
            string secret = "secret text";

            Credential w_credential = new Credential
            {
                TargetName = targetName,
                Persist = CredentialPersist.LocalMachine,
                Type = CredentialType.Generic,
                CredentialBlob = Encoding.Unicode.GetBytes(secret)
            };

            bool success;
            int error;

            success = CredentialManager.WriteCredential(ref w_credential);
            error = Marshal.GetLastWin32Error();
            Assert.True(success || error != 0);
            Assert.True(success);

            success = CredentialManager.ReadCredential(targetName, CredentialType.Generic, 0, out Credential r_credential);
            error = Marshal.GetLastWin32Error();
            Assert.True(success || error != 0);
            Assert.True(success);

            Assert.True(secret == Encoding.Unicode.GetString(r_credential.CredentialBlob));
        }
    }
}
