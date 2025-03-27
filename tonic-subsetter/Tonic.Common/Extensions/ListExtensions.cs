using System;
using System.Collections.Generic;

namespace Tonic.Common.Extensions
{
    public static class ListExtensions
    {
        public static void ForEach<T>(this IReadOnlyList<T> list, Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(list);
            ArgumentNullException.ThrowIfNull(action);

            int count = list.Count;

            for (int i = 0; i < count; i++)
            {
                action(list[i]);
            }
        }
        public static void ForEach<T>(this IList<T> list, Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(list);
            ArgumentNullException.ThrowIfNull(action);

            int count = list.Count;

            for (int i = 0; i < count; i++)
            {
                action(list[i]);
            }
        }
        public static T? Find<T>(this IList<T> list, Predicate<T> match)
        {
            ArgumentNullException.ThrowIfNull(list);
            ArgumentNullException.ThrowIfNull(match);

            int size = list.Count;

            for (int i = 0; i < size; i++)
            {
                if (match(list[i]))
                {
                    return list[i];
                }
            }
            return default;
        }

        public static void AddRange<T>(this IList<T> list, IEnumerable<T>? items)
        {
            ArgumentNullException.ThrowIfNull(list);

            if (items != null)
            {
                foreach (var item in items)
                {
                    list.Add(item);
                }
            }
        }
    }
}