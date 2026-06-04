using System;
using System.Collections.Generic;
using UnityEngine;

namespace Slenderpi.Utilities.ListExtensions {
	/// <summary>
	/// Contains extension functions for classes that implement IList.
	/// </summary>
	public static class ListExtensions {
		/// <summary>
		/// Removes the element at the indicated index, and then replaces it with the element at the end of the list.
		/// </summary>
		/// <returns>The element that was removed.</returns>
		public static T RemoveAtSwapBack<T>(this IList<T> list, int index) {
			int lastIndex = list.Count - 1;
			T ret = list[index];
			list[index] = list[lastIndex];
			list.RemoveAt(lastIndex);
			return ret;
		}

		/// <summary>
		/// Get a random element.
		/// </summary>
		/// <typeparam name="T">Element type.</typeparam>
		/// <exception cref="ArgumentException">Thrown if the List is empty.</exception>
		public static T GetRandom<T>(this IList<T> list) {
			if (list == null || list.Count == 0) {
				throw new ArgumentException("[ListExtensions]: Cannot select a random item from an empty or null list.");
			}
			return list[UnityEngine.Random.Range(0, list.Count)];
		}
	}
}
