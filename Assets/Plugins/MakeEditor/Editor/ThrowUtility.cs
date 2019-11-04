using System;

namespace Bloodstone.MakeEditor
{
    public static class ThrowUtility
    {
        public static T ThrowIfNull<T>(this T target, string message) where T : class
        {
            if(target == null)
            {
                throw new ArgumentNullException(message);
            }

            return target;
        }

        public static T ThrowIfNull<T>(this T target) where T : class
        {
            if (target == null)
            {
                throw new ArgumentNullException();
            }

            return target;
        }
    }
}