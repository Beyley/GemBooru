using System.Security.Cryptography;
using Bunkum.Core.Authentication;
using Bunkum.Core.Database;
using Bunkum.Listener.Request;
using GemBooru.Database;

namespace GemBooru.Authentication;

public class CertificateAuthenticationProvider : IAuthenticationProvider<BunkumToken>
{
    public BunkumToken? AuthenticateToken(ListenerContext request, Lazy<IDatabaseContext> lazyDatabase)
    {
        if (request.RemoteCertificate != null)
        {
            var certHash = request.RemoteCertificate.GetCertHashString(HashAlgorithmName.SHA256);
            
            var database = (GemBooruDatabaseContext)lazyDatabase.Value;

            var name = request.RemoteCertificate.Subject;
            if (name.StartsWith("CN="))
                name = name[3..];
            
            // Create the user with a default name
            var user = database.CreateOrGetUser(certHash, name);

            database.SaveChanges();
            
            return new BunkumToken(user);
        }

        return null;
    }
}