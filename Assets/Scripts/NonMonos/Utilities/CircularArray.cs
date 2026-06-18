using System.Runtime.CompilerServices;

namespace Slenderpi.Utilities.CircularArray {
	public class CircularArray<T> {

		public T[] Array => _arr;
		public int Capacity => _arr.Length;
		/// <summary>
		/// Index of the head, i.e. the oldest element.
		/// </summary>
		public int HeadPointer => _headPointer;
		/// <summary>
		/// Index of the tail, i.e. the newest element.
		/// </summary>
		public int TailPointer => Capacity - _headPointer - 1;

		private readonly T[] _arr;
		private int _headPointer;

		public T this[int index] {
			get => _arr[index];
			set => _arr[index] = value;
		}



		public CircularArray(int length) {
			_arr = new T[length];
			_headPointer = 0;
		}

		/// <summary>
		/// Sets all elements to val.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetAll(T val) {
			for (int i = 0; i < Capacity; i++)
				_arr[i] = val;
		}

		/// <summary>
		/// Returns the oldest element (the element at the HeadPointer).
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Peek() {
			return _arr[_headPointer];
		}

		/// <summary>
		/// Returns the newest element.
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T PeekTail() {
			return _arr[TailPointer];
		}

		/// <summary>
		/// Adds a new element to the CircularArray.
		/// </summary>
		/// <param name="elem">The element to add.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(T elem) {
			_arr[_headPointer] = elem;
			_headPointer = (_headPointer + 1) % (Capacity - 1);
		}

	}
}