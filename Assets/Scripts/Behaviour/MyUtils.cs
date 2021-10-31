using System;


public static class MyUtils {
	public static T[] ConcatArray<T>(this T[] a, T[] b) {
		T[] result = new T[a.Length + b.Length];
		a.CopyTo(result, 0);
		b.CopyTo(result, a.Length);
		return result;
	}
}
