﻿using System;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class EnumerableEx
    {
        /// <summary>
        /// Completely consumes the given sequence. This method uses immediate execution,
        /// and doesn't store any data during execution.
        /// </summary>
        /// <typeparam name="T">Element type of the sequence</typeparam>
        /// <param name="source">Source to consume</param>

        public static void Consume<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            foreach (var element in source){}
        }
    }
}