using System.Runtime.InteropServices;
using Docker.DotNet;

namespace Cs2PracticeModeTests.Helpers.DockerContainerFolder;

public class DockerApi
{
    public static readonly DockerClient DockerClient =
        new DockerClientConfiguration(new Uri(DockerApi.Uri())).CreateClient();
    
    private static string Uri()
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        if (isWindows)
        {
            return "npipe://./pipe/docker_engine";
        }

        var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        if (isLinux)
        {
            return "unix:/var/run/docker.sock";
        }

        throw new Exception(
            "Was unable to determine what OS this is running on, does not appear to be Windows or Linux!?");
    }

}