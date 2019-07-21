using System;
using System.Linq;
using System.Reflection;

namespace GAssist
{
    class PowerLock
    {
        internal static void RequestScreenLock(int timeout)
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Tizen.System");

            Type type = assembly.GetTypes().First(t => t.Name == "Device" && t.IsClass);

            MethodInfo methodInfo = type.GetRuntimeMethods().First(m => m.Name == "DevicePowerRequestLock");

            methodInfo.Invoke(null, new object[] { 1, timeout });
        }

        //static void ReleaseLock()
        //{
        //TODO
        //}

    }
}
