using System;

namespace meteor.Utils;

public class Utils
{
    public static void CheckAndLogIfNull(object obj, string name)
    {
        if (obj == null) Console.WriteLine($"{name} is null");
    }
}