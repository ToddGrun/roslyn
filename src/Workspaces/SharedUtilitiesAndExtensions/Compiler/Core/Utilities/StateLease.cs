// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Roslyn.Utilities
{
    [NonCopyable]
    internal struct StateLease(int initialCount)
    {
        private int _state = initialCount;

        public static void Initialize(out StateLease lease)
        {
            // State always has an initial lease by the owning object
            lease._state = 1;
        }

        public static void TryLease(ref StateLease lease, out bool isOwned)
        {
            while (true)
            {
                var existingValue = Volatile.Read(ref lease._state);
                if (existingValue == 0)
                {
                    // The final lease was already released, so ownership is not possible.
                    isOwned = false;
                    return;
                }

                var valueDuringWrite = Interlocked.CompareExchange(ref lease._state, existingValue + 1, existingValue);
                if (valueDuringWrite == existingValue)
                {
                    // A new lease was obtained.
                    isOwned = true;
                    return;
                }

                // Another lease occurred during the operation. Try again.
            }
        }

        public static void TryRelease<TArg>(ref StateLease lease, bool isOwned, Action<TArg> releaser, TArg arg)
        {
            if (!isOwned)
                return;

            var valueAfterWrite = Interlocked.Decrement(ref lease._state);
            if (valueAfterWrite == 0)
            {
                // The last lease was released.
                releaser(arg);
            }
        }
    }
}
