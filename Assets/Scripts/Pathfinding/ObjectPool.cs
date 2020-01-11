// Copyright (c) 2017 StagPoint Software

using System;
using System.Collections.Generic;

namespace Pathfinding
{
    public class ObjectPool<T> where T : class
    {
        #region Static variables 

        // NOTE: Switched to Stack<object> in an attempt to work around
        // a bug in the version of Mono used by Unity on iOS - http://stackoverflow.com/q/16542915
        private static Stack<object> s_objectPool = new Stack<object>();
        private static volatile object s_syncLock = new object();

        #endregion

        #region Stats backing variables

        private static int s_poolObjectHitCount;
        private static int s_poolObjectMissCount;
        private static int s_instancesClaimedCount;
        private static int s_instancesAllocatedCount;
        private static int s_returnedToPoolCount;
        private static int s_lastClaimTime;
        private static int s_lastReturnTime;

        #endregion

        #region Stats properties

        /// <summary>
        /// The number of times the pool had a spare object to provide to the user without creating it on demand.
        /// </summary>
        public static int PoolObjectHitCount
        {
            get { lock (s_syncLock) return s_poolObjectHitCount; }
        }

        /// <summary>
        /// The total count of times the pool was not able to return a pooled object instance when requested.
        /// This value is incremented in both ClaimFromPool() and TryClaimFromPool() whenever the object pool
        /// is empty. For the total number of objects actually allocated, check TotalInstancesAllocated instead.
        /// </summary>
        public static int PoolObjectMissCount
        {
            get { lock (s_syncLock) return s_poolObjectMissCount; }
        }

        /// <summary>
        /// The total number of object instances claimed over the lifetime of the object pool
        /// </summary>
        public static int TotalInstancesClaimed
        {
            get { lock (s_syncLock) return s_instancesClaimedCount; }
        }

        /// <summary>
        /// The total number of object instances allocated over the lifetime of the object pool
        /// </summary>
        public static int TotalInstancesAllocated
        {
            get { lock (s_syncLock) return s_instancesAllocatedCount; }
        }

        /// <summary>
        /// The number of objects that have been returned to the pool over the lifetime of the object pool
        /// </summary>
        public static int TotalInstancesReturnedToPool
        {
            get { lock (s_syncLock) return s_returnedToPoolCount; }
        }

        /// <summary>
        /// Returns the number of objects currently in the object pool
        /// </summary>
        public static int CurrentPoolSize
        {
            get { lock (s_syncLock) return s_objectPool.Count; }
        }

        /// <summary>
        /// Returns the number of milliseconds that have elapsed since the last time 
        /// an object was claimed from the object pool
        /// </summary>
        public static int LastClaimTime
        {
            get { lock (s_syncLock) return System.Environment.TickCount - s_lastClaimTime; }
        }

        /// <summary>
        /// Returns the number of milliseconds that have elapsed since the last time
        /// an object was added or returned to the object pool
        /// </summary>
        public static int LastReturnTime
        {
            get { lock (s_syncLock) return System.Environment.TickCount - s_lastReturnTime; }
        }

        #endregion

        #region Class constructor 

        static ObjectPool()
        {
            ObjectPools.RegisterClearFunction(() => ClearPool(true));

            s_lastClaimTime = System.Environment.TickCount;
            s_lastReturnTime = System.Environment.TickCount;
        }

        #endregion

        #region Public functions

        /// <summary>
        /// If an object instance is available in the object pool, the item argument will be set
        /// to the instance and the function will return TRUE. Otherwise, item will be set to 
        /// null and the function will return FALSE.
        /// </summary>
        public static bool TryClaimFromPool(out T item)
        {
            lock (s_syncLock)
            {
                s_instancesClaimedCount += 1;
                s_lastClaimTime = Environment.TickCount;

                if (s_objectPool.Count > 0)
                {
                    s_poolObjectHitCount += 1;
                    item = (T)s_objectPool.Pop();
                    return true;
                }

                s_poolObjectMissCount += 1;
                item = null;

                return false;
            }
        }

        /// <summary>
        /// If an object instance is available in the object pool, it will be returned. Otherwise
        /// a new instance of the specified type is instantiated and returned.
        /// </summary>
        public static T ClaimFromPool()
        {
            lock (s_syncLock)
            {
                s_instancesClaimedCount += 1;
                s_lastClaimTime = Environment.TickCount;

                if (s_objectPool.Count > 0)
                {
                    s_poolObjectHitCount += 1;
                    return (T)s_objectPool.Pop();
                }

                s_poolObjectMissCount += 1;
                s_instancesAllocatedCount += 1;

                return Activator.CreateInstance<T>();
            }
        }

        /// <summary>
        /// Returns an object instance back to the object pool. <b>IMPORTANT: </b> It is up to the caller
        /// to ensure that the object's state is ready for the object to be recycled (typically by 
        /// setting all of the instance's internal values to the defaults) before calling this function.
        /// </summary>
        public static void ReturnToPool(T instance)
        {
            lock (s_syncLock)
            {
                s_returnedToPoolCount += 1;
                s_lastReturnTime = System.Environment.TickCount;

                s_objectPool.Push(instance);
            }
        }

        /// <summary>
        /// Clears the object pool, and optionally releases all memory used to maintain the object pool lists
        /// </summary>
        public static void ClearPool(bool trimMemory)
        {
            lock (s_syncLock)
            {
                s_objectPool.Clear();

                s_lastClaimTime = System.Environment.TickCount;
                s_lastReturnTime = System.Environment.TickCount;

                if (trimMemory)
                {
                    s_objectPool.TrimExcess();
                }
            }
        }

        /// <summary>
        /// Adds an object to the pool without incrementing stats. Used to prefill the object pool
        /// </summary>
        public static void AddToPool(T instance)
        {
            lock (s_syncLock)
            {
                s_lastReturnTime = System.Environment.TickCount;
                s_objectPool.Push(instance);
            }
        }

        #endregion
    }

    /// <summary>
    /// Provides a convenient way to clear all object pools at the same time
    /// </summary>
    public class ObjectPools
    {
        #region Static variables

        private static List<Action> s_clearFunctions = new List<Action>();
        private static volatile object s_syncLock = new object();

        #endregion

        /// <summary>
        /// Clears all object pools and releases any memory used to maintain internal lists
        /// </summary>
        public static void ClearAll()
        {
            lock (s_syncLock)
            {
                for (int i = 0; i < s_clearFunctions.Count; i++)
                {
                    s_clearFunctions[i]();
                }
            }
        }

        /// <summary>
        /// Used internally by individual object pools to register a function that can be called
        /// by this class to clear all object pools at once. 
        /// </summary>
        internal static void RegisterClearFunction(Action action)
        {
            s_clearFunctions.Add(action);
        }
    }

    public static class ObjectPoolExtensions
    {
        /// <summary>
        /// Returns an object instance to the object pool.
        /// <B>IMPORTANT:</B> It is up to the caller to perform all work necessary to prepare 
        /// the object instance for recycling prior to calling this function.
        /// </summary>
        public static void ReturnToPool<T>(this T instance) where T : class
        {
            ObjectPool<T>.ReturnToPool(instance);
        }
    }
}