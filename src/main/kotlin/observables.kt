@file:Suppress("NOTHING_TO_INLINE")


private fun <T> Set<T>.mutate(removed: Set<T>, added: Set<T>): Set<T> = LinkedHashSet<T>(
	size - removed.size + added.size
).also {
	for(element in this)
		if(element !in removed) it.add(element)
	it.addAll(added)
}

private fun <T> Collection<T>.asSet() = if(this is Set<T>) this else toSet()


class DiffObservableSet<T>(
	var originalSet: Set<T> = emptySet(),
	private val onChanged: (before: Set<T>, removed: Set<T>, added: Set<T>) -> Unit
) :
	MutableSet<T> {
	private inline fun mutate(removed: Set<T> = emptySet(), added: Set<T> = emptySet()): Boolean {
		if(removed.isEmpty() && added.isEmpty()) return false
		val original = originalSet
		originalSet = original.mutate(removed, added)
		onChanged(original, removed, added)
		return true // don't check if before == after(?? why not?)
	}
	
	override fun add(element: T) = mutate(added = setOf(element))
	override fun addAll(elements: Collection<T>) = mutate(added = elements.asSet())
	override fun clear() {
		mutate(removed = originalSet)
	}
	
	override fun iterator(): MutableIterator<T> = object : MutableIterator<T> {
		private var lastElement: T? = null
		private val originalIterator = originalSet.iterator()
		override fun hasNext() = originalIterator.hasNext()
		override fun next(): T {
			val e = originalIterator.next()
			lastElement = e
			return e
		}
		
		override fun remove() {
			@Suppress("UNCHECKED_CAST")
			mutate(removed = setOf(lastElement as T))
		}
	}
	
	override fun remove(element: T) = mutate(removed = setOf(element))
	override fun removeAll(elements: Collection<T>) = mutate(removed = elements.asSet())
	override fun retainAll(elements: Collection<T>) =
		mutate(removed = originalSet - elements)
	
	override val size = originalSet.size
	override fun contains(element: T) = originalSet.contains(element)
	override fun containsAll(elements: Collection<T>) = originalSet.containsAll(elements)
	override fun isEmpty() = originalSet.isEmpty()
}
