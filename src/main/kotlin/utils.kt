import org.graphstream.graph.Element
import java.awt.Color
import kotlin.math.round


fun roundDisplay(number: Double) = round(number * 1e2f) / 1e2f

fun <K, V, E : Map.Entry<K, V>> Set<E>.toMap(): Map<K, V> = mutableMapOf<K, V>().also { map ->
	forEach {
		map[it.key] = it.value
	}
}

inline fun <T> buildSet(
	count: Int,
	block: (i: Int) -> T
): MutableSet<T> {
	val set = mutableSetOf<T>()
	for(i in 0 until count) {
		set.add(block(i))
	}
	return set
}

fun lerp(from: Color, to: Color, fraction: Float) = Color(
	lerp(from.red, to.red, fraction),
	lerp(from.green, to.green, fraction),
	lerp(from.blue, to.blue, fraction),
	lerp(from.alpha, to.alpha, fraction)
)

fun lerp(from: Int, to: Int, fraction: Float): Int = (from + (to - from) * fraction).toInt()
fun lerp(from: Double, to: Double, fraction: Double): Double = (from + (to - from) * fraction)


fun Element.setHasClass(name: String, has: Boolean) {
	val last = getAttribute("ui.class")
	when(last) {
		null -> if(has) setAttribute("ui.class", name)
		is Array<*> -> if(has) {
			if(name !in last) setAttribute("ui.class", *last, name)
		} else {
			if(name in last) setAttribute("ui.class", last.filter { it != name }.toTypedArray())
		}
		else -> if(has) {
			if(last != name) setAttribute("ui.class", last, name)
		} else {
			if(last == name) removeAttribute("ui.class")
		}
	}
}
