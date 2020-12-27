import org.graphstream.graph.implementations.SingleGraph
import kotlin.math.max
import kotlin.random.Random


inline fun <T> List<Set<T>>.forEachNode(block: (T) -> Unit) {
	forEach {
		it.forEach(block)
	}
}


fun EcoSystem.randomWeight() = 1.0 /*random.nextDouble() * 0.4 + 0.8*/


class EcoSystem(
	intertwining: Int,
	intertwiningExtent: Float,
	val producer: MutableSet<Producer>,
	val consumers: MutableList<MutableSet<Consumer>>,
	val random: Random = Random.Default
) {
	var updating = false
	var fairness = 0.3 // communism engine(???)
	var decreasing = 0.01
	var speed = 0.1
	var nodesToRemove: Set<Node>? = null
	var boundGraph: SingleGraph? = null
	val allLevels: List<Set<Node>> get() = listOf(producer, *consumers.toTypedArray())
	val consumingNodes: List<Set<ConsumingNode>> = consumers
	val producingNodes: List<Set<ProducingNode>> = listOf(producer, *consumers.toTypedArray())
	val levelCount get() = 1 + consumers.size
	
	init {
		val n = intertwining
		val ratio = intertwiningExtent
		
		val realRatio = n * (1 - ratio)
		fun countFor(sub: Int) = if(sub == 1) n else max(n - realRatio * max(0, sub - 1), 0f).toInt()
		
		// connect producer
		for(to in consumers.indices) repeat(countFor(to + 1)) {
			producer.random(random).connectTo(consumers[to].random(random), weight = randomWeight())
		}
		
		// connect consumer
		for(from in consumers.indices) {
			val fromSet = consumers[from]
			
			for(to in from + 1..consumers.lastIndex) {
				val toSet = consumers[to]
				val count = countFor(to - from)
				repeat(count) {
					fromSet.random(random).connectTo(toSet.random(random), weight = randomWeight())
				}
			}
		}
		
		allLevels.forEachNode { node ->
			node.ecoSystem = this
		}
	}
	
	fun verify() {
		allLevels.forEachNode { node ->
			if(node is ConsumingNode) node.preys.forEach { check(it.co.to == node) }
			if(node is ProducingNode) node.preyedBys.forEach { check(it.co.from == node) }
		}
	}
}
