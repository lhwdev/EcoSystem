import org.graphstream.graph.Edge


// correspond to edge of graph
interface Dependency {
	var weight: Double
	val co: CoDependency
	var graphNode: Edge?
}

interface GetDependency : Dependency {
	val from: ProducingNode
}

interface GiveDependency : Dependency {
	val to: ConsumingNode
}

class CoDependency(override val from: ProducingNode, override val to: ConsumingNode, weight: Double) :
	GetDependency, GiveDependency {
	
	override var weight = weight
		set(value) {
			field = value
			updateEco()
		}
	
	override var graphNode: Edge? = null
		set(value) {
			field = value
			value?.setAttribute("ecosystem.dependency", this)
		}
	
	override val co get() = this
	
	init {
		from.preyedBys += this
		to.preys += this
		graphNode = edgeOf(create = true)
	}
}


fun CoDependency.remove() {
	from.preyedBys.remove(this)
	to.preys.remove(this)
	graphNode?.let { edge ->
		edge.sourceNode.graph.removeEdge(edge)
	}
}
