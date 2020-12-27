import kotlin.math.min

val ConsumingNode.allEatExpected get() = eatRatio * biomass

// assumption: the more biomass, the more eat
val GiveDependency.consumerEatMass get() = to.biomass * weight

val ProducingNode.consumersEatMass get() = preyedBys.sumOf { it.consumerEatMass }


fun EcoSystem.tick(count: Int = 5) {
	val time = System.currentTimeMillis()
	repeat(count) { tickOnce() }
	println("tick took ${System.currentTimeMillis() - time}ms")
}

fun EcoSystem.tickOnce() {
	fun Node.newSiblings() = siblings * random.nextFloat()
	fun ProducingNode.removeMass(mass: Double, factor: Double) {
		val real = min(biomass, mass) * factor
		biomass -= real
	}
	
	updating = true
	
	nodesToRemove?.forEach(::extinct)
	
	val sumMassCache = mutableMapOf<ProducingNode, Double>()
	val nodesToRemove = mutableSetOf<Node>()
	
	producer.forEach { it.biomass += lerp(0.0, it.newSiblings() * it.oneMass, speed) }
	consumingNodes.forEachNode { consumer ->
		val survivalRatio = consumer.preys.sumOf { get ->
			val from = get.from
			val preyEatersMass = sumMassCache.getOrPut(from) { from.consumersEatMass }
			val eats = from.biomass * lerp(get.co.consumerEatMass / preyEatersMass, 1.0 / from.preyedBys.size, fairness)
			from.removeMass(eats, speed * decreasing)
			eats
		} / consumer.allEatExpected
		consumer.biomass *= lerp(1.0, survivalRatio, speed)
		if(consumer.count == 0) nodesToRemove += consumer // set shouldn't modified while it is iterated
	}
	
	this.nodesToRemove = nodesToRemove
	
	verify()
	
	updating = false
}

fun EcoSystem.extinct(node: Node) {
	verify()
	
	node.biomass = 0.0
	if(node is ConsumingNode) node.preys.forEach { get -> get.from.preyedBys.removeAll { it.to == node } }
	if(node is ProducingNode) node.preyedBys.forEach { give -> give.to.preys.removeAll { it.from == node } }
	
	when(node) {
		is Producer -> producer -= node
		is Consumer -> consumers.forEach { it -= node }
	}
	
	// should be last -- or upper things will break
	node.graphNode?.let { boundGraph?.removeNode(it) }
}
