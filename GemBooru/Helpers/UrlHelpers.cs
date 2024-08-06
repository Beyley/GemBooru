namespace GemBooru.Helpers;

public static class UrlHelpers
{
    public static string WithSchemeAndPath(string uri, string scheme, string path) =>
        new UriBuilder(uri)
        {
            Scheme = scheme,
            Path = path
        }.ToString();
}