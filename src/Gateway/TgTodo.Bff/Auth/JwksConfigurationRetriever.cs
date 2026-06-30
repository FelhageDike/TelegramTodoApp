using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;

namespace TgTodo.Bff.Auth;

internal static class AdminJwksLoader
{
    internal static ICollection<SecurityKey> LoadSigningKeys(string jwksUri, bool requireHttps)
    {
        var retriever = new HttpDocumentRetriever { RequireHttps = requireHttps };
        var jwksJson = retriever.GetDocumentAsync(jwksUri, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        return new JsonWebKeySet(jwksJson).GetSigningKeys();
    }
}
