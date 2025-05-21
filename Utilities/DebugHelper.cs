using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AppCleanRoom.Utilities
{
    /// <summary>
    /// DebugHelper helper log ra các property có giá trị null trong một object 
    /// </summary>
    public static class DebugHelper
    {
        public static void LogNullProperties(object obj, string objName = "Object")
        {
            if (obj == null) {
                Console.WriteLine($"{objName} is NULL.");
                return;
            }

            var nullProps = obj.GetType()
                               .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                               .Where(p => p.CanRead && p.GetValue(obj) == null)
                               .Select(p => p.Name)
                               .ToList();

            if (nullProps.Count == 0) {
                Console.WriteLine($"{objName} has no null properties.");
            }
            else {
                Console.WriteLine($"[DEBUG] {objName} has {nullProps.Count} null properties:");
                foreach (var prop in nullProps) {
                    Console.WriteLine($" - {prop}");
                }
            }
        }
    }
}
