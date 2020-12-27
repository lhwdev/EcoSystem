

val Node.count get() = (biomass / oneMass).toInt()

val Node.kind
	get() = when(this) {
		is Producer -> "생산자"
		is Consumer -> "소비자"
		is Decomposer -> "분해자"
		else -> "??"
	}

val Set<Node>.sumMass get() = sumOf { it.biomass }


fun ProducingNode.connectTo(to: ConsumingNode, weight: Double) = CoDependency(this, to, weight)

fun ProducingNode.edgeTo(to: ConsumingNode) = preyedBys.find { it.to == to }?.co
