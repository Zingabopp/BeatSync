using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace BeatSync
{
    public static class Utilities
    {
        /// <summary>
        /// Attempts to find a resource of type TResource with the given name. An action can be provided to execute when the object is found.
        /// pollRateMillis is the interval in milliseconds to check for the existance of the object.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="name"></param>
        /// <param name="action"></param>
        /// <param name="pollRateMillis"></param>
        /// <returns></returns>
        public static IEnumerator<WaitForSeconds> WaitForResource<TResource>(string name, Action<TResource> action = null, int pollRateMillis = 100)
            where TResource : UnityEngine.Object
        {
            Func<bool> waitFunc = () => Resources.FindObjectsOfTypeAll<TResource>().Any(o =>
            {
                if (o.name != name)
                    return false;
                try
                {
                    action?.Invoke(o);
                }
                catch (Exception ex)
                {
                    Logger.log.Error($"Error invoking action for WaitForResource<{typeof(TResource)}> with name {name}.\n{ex?.Message}\n{ex?.StackTrace}");
                }
                return true;
            });
            var wait = new WaitForSeconds(Math.Max(pollRateMillis / 1000f, .02f));
            while (!waitFunc.Invoke())
            {
                yield return wait;
            }
            //yield return waitFunc;

        }
    }
}
