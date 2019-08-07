using System.Reflection;

namespace SupportPlatform
{
   abstract class CameraNVR
    {
        private static readonly string AssemblyName = "SupportPlatform";
        private static readonly string cameraType = "HikvisionInit";
        public static Hikvision CreateCamera(string cameraType)
        {
            switch (cameraType)
            {
                case "Hikvision":cameraType = "HikvisionInit";
                    break;
                case "宇世":cameraType = "";
                    break;
                case "大华":cameraType = "";
                    break;
                default:
                    break;
            }
            string className = AssemblyName + "." + cameraType;
            return (Hikvision)Assembly.Load(AssemblyName).CreateInstance(className);
        }
    }
}
