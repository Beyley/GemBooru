using Bunkum.Core.Authentication;
using GemBooru.Database;

namespace GemBooru.Authentication;

public class BunkumToken : IToken<DbUser>
{
    internal BunkumToken(DbUser user) => this.User = user;

    public DbUser User { get; }
    public string CertificateHash => User.CertificateHash;
}