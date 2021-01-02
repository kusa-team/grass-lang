using System;
using System.IO;
namespace grasslang.Build
{
    public class Platform
    {
        public enum PlatformName
        {
            Windows,
            Linux,
            Mac
        }
        public static PlatformName GetPlatform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    if (Directory.Exists("/Applications")
                        & Directory.Exists("/System")
                        & Directory.Exists("/Users")
                        & Directory.Exists("/Volumes"))
                        return PlatformName.Mac;
                    else
                        return PlatformName.Linux;

                case PlatformID.MacOSX:
                    return PlatformName.Mac;

                default:
                    return PlatformName.Windows;
            }
        }
    }
}
