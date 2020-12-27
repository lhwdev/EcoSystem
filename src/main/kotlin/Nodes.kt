import java.util.UUID
import org.graphstream.graph.Node as GNode


// correspond to node of graph
interface Node {
	var ecoSystem: EcoSystem?
	val uuid: UUID
	val level: Int
	val id: String
	var oneMass: Double
	var biomass: Double
	var siblings: Int
	var graphNode: GNode?
	
	fun update() {}
}

interface ProducingNode : Node {
	val preyedBys: MutableSet<GiveDependency>
}

interface ConsumingNode : Node {
	/**
	 * Here, we will call all species this species eats as 'prey', including producers.
	 */
	val preys: MutableSet<GetDependency>
	
	/**
	 * = (biomass of prey) / (biomass of itself)
	 */
	var eatRatio: Double
}


fun Node.dumpSelf() = "$id(level=$level, oneMass=$oneMass, count=$count, siblings=$siblings)"
