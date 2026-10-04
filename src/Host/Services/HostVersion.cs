namespace Kuestencode.Werkbank.Host.Services;

public static class HostVersion
{
    public static string Get(IConfiguration configuration) =>
        configuration["MODULE_VERSION"]
        ?? configuration["IMAGE_TAG"]
        ?? configuration["DOCKER_IMAGE_TAG"]
        ?? "dev";
}
