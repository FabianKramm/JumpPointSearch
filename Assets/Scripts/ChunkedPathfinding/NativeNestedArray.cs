using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Internal;

namespace ChunkedPathfinding
{
    /// <summary>
    /// A native container that holds an array of blittable data.
    /// Unlike the nativeArray provided by unity, it is itself blittable, allowing for nesting if nativeNestedArrays
    /// </summary>
    /// <typeparam name="T">The type of the field</typeparam>
    [NativeContainer]
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(NativeNestedArrayDebugView<>))]
    public struct NativeNestedArray<T> : IDisposable, IEnumerable<T> where T : struct
    {
        /// <summary>
        /// Pointer to the native memory holding the array of values of the field
        /// </summary>
        [NativeDisableUnsafePtrRestriction]
        private IntPtr _buffer;
        private int _length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private int _minIndex;
        private int _maxIndex;

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once MemberCanBePrivate.Global
        internal AtomicSafetyHandle m_Safety;
#endif

        private Allocator _allocatorLabel;

        /// <summary>
        /// Creates a new instance with the desired length / capacity of the array. All items of the array will have their default value
        /// </summary>
        /// <param name="length">The length/capacity of the array</param>
        /// <param name="allocator">The type of allocator to use for the native memory allocation</param>
        /// <param name="safeguard">The safeguard which counts this native allocation and detects memory leaks</param>
        /// <exception cref="ArgumentException">Throws if the allocator is not supported or the type is not blittable</exception>
        /// <exception cref="ArgumentOutOfRangeException">Throws if the length is invalid</exception>
        public unsafe NativeNestedArray(int length, Allocator allocator)
        {
            long totalSize = UnsafeUtility.SizeOf<T>() * length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Native allocation is only valid for Temp, Job and Persistent
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", "allocator");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", "Length must be >= 0");
            // if (!UnsafeUtility.IsBlittable<T>())
            //    throw new ArgumentException(string.Format("{0} used in NativeCustomArray<{0}> must be blittable", typeof(T)));
#endif

            _buffer = (IntPtr)UnsafeUtility.Malloc(totalSize, UnsafeUtility.AlignOf<T>(), allocator);
            UnsafeUtility.MemClear((void*)_buffer, totalSize);

            _length = length;
            _allocatorLabel = allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            _minIndex = 0;
            _maxIndex = length - 1;
            m_Safety = AtomicSafetyHandle.Create();
#endif
        }

        public NativeNestedArray(NativeNestedArray<T> array, int length, Allocator allocator) : this(length, allocator)
        {
            CopyFrom(array);
        }

        private unsafe void CopyFrom(NativeNestedArray<T> array)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            if (Length < array.Length)
            {
                Dispose();
                throw new ArgumentException("array.Length is greater than the length of this instance");
            }
            for (int index = 0; index < array.Length; ++index)
                UnsafeUtility.WriteArrayElement((void*)_buffer, index, array[index]);
        }

        /// <summary>
        /// The length of the array
        /// </summary>
        public int Length { get { return _length; } }

        /// <summary>
        /// Returns the item at the specified index
        /// </summary>
        public unsafe T this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // If the container is currently not allowed to read from the buffer
                // then this will throw an exception.
                // This handles all cases, from already disposed containers
                // to safe multithreaded access.
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

                // Perform out of range checks based on
                // the NativeContainerSupportsMinMaxWriteRestriction policy
                if (index < _minIndex || index > _maxIndex)
                    FailOutOfRangeError(index);
#endif
                // Read the element from the allocated native memory
                return UnsafeUtility.ReadArrayElement<T>((void*)_buffer, index);
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // If the container is currently not allowed to write to the buffer
                // then this will throw an exception.
                // This handles all cases, from already disposed containers
                // to safe multithreaded access.
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

                // Perform out of range checks based on
                // the NativeContainerSupportsMinMaxWriteRestriction policy
                if (index < _minIndex || index > _maxIndex)
                    FailOutOfRangeError(index);
#endif
                // Writes value to the allocated native memory
                UnsafeUtility.WriteArrayElement((void*)_buffer, index, value);
            }
        }

        /// <summary>
        /// Converts the nativeNestedArray to an array
        /// </summary>
        /// <returns></returns>
        public unsafe T[] ToArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

            var array = new T[Length];
            for (var i = 0; i < Length; i++)
                array[i] = UnsafeUtility.ReadArrayElement<T>((void*)_buffer, i);
            return array;
        }

        /// <summary>
        /// The native nested array had been created if the pointer has been allocated
        /// </summary>
        public bool IsCreated
        {
            get { return _buffer != IntPtr.Zero; }
        }

        /// <summary>
        /// Frees any memory allocated by the native nested array
        /// </summary>
        public unsafe void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(m_Safety);
#endif
            
            UnsafeUtility.Free((void*)_buffer, _allocatorLabel);
            _buffer = IntPtr.Zero;
            _length = 0;
        }


#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private void FailOutOfRangeError(int index)
        {
            if (index < Length && (_minIndex != 0 || _maxIndex != Length - 1))
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of restricted IJobParallelFor range [{_minIndex}...{_maxIndex}] in ReadWriteBuffer.\n" +
                    "ReadWriteBuffers are restricted to only read & write the element at the job index. " +
                    "You can use double buffering strategies to avoid race conditions due to " +
                    "reading & writing in parallel to the same elements from a job.");

            throw new IndexOutOfRangeException($"Index {index} is out of range of '{Length}' Length.");
        }

#endif
        /// <summary>
        /// Retuns an enumerator to enumerate over the items of the native array
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// The enumerator that can enumerate over items of this nativeNestedArray
        /// </summary>
        [ExcludeFromDocs]
        public struct Enumerator : IEnumerator<T>
        {
            private NativeNestedArray<T> _mArray;
            private int _mIndex;

            public Enumerator(ref NativeNestedArray<T> array)
            {
                _mArray = array;
                _mIndex = -1;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                ++_mIndex;
                return _mIndex < _mArray.Length;
            }

            public void Reset()
            {
                _mIndex = -1;
            }

            public T Current => _mArray[_mIndex];

            object IEnumerator.Current => Current;
        }

    }

    /// <summary>
    /// Used to represent the nativeNestedArray in a debuggers' view
    /// </summary>
    internal sealed class NativeNestedArrayDebugView<T> where T : struct
    {
        private NativeNestedArray<T> _array;

        public NativeNestedArrayDebugView(NativeNestedArray<T> array)
        {
            _array = array;
        }

        public T[] Items
        {
            get { return _array.ToArray(); }
        }
    }
}