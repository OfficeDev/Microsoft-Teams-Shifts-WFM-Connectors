// <copyright file="QueueExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Queue extension methods.
    /// </summary>
    public static class QueueExtensions
    {
        /// <summary>
        /// Dequeue an item from queue.
        /// </summary>
        /// <typeparam name="T">Specifies the type of elements in the queue.</typeparam>
        /// <param name="queue">Incoming sequence.</param>
        /// <param name="chunkSize">Lot size.</param>
        /// <returns>IEnumerable.</returns>
        public static IEnumerable<T> DequeueChunk<T>(this Queue<T> queue, int chunkSize)
        {
            if (queue is null)
            {
                throw new ArgumentNullException(nameof(queue));
            }

            for (int i = 0; i < chunkSize && queue.Count > 0; i++)
            {
                yield return queue.Dequeue();
            }
        }
    }
}