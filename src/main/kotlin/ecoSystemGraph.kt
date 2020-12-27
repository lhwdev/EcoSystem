import org.graphstream.graph.Edge
import org.graphstream.graph.Graph
import org.graphstream.graph.implementations.SingleGraph
import org.graphstream.ui.graphicGraph.GraphicGraph
import java.awt.Color
import org.graphstream.graph.Node as GNode


val Node.graphNodeName get() = uuid.toString()
fun String.graphEdgeName(to: String) = "$this -> $to"
val CoDependency.graphEdgeName get() = from.graphNodeName.graphEdgeName(to.graphNodeName)

val GNode.ecoNode get() = getAttribute("ecosystem.node") as Node
val Edge.ecoDependency get() = getAttribute("ecosystem.dependency") as CoDependency

fun CoDependency.edgeOf(create: Boolean = false): Edge? {
	val fromNode = from.graphNode
	val toNode = to.graphNode
	if(fromNode == null || toNode == null) return null
	
	val edgeName = graphEdgeName
	val graph = fromNode.graph
	
	val existing = graph.getEdge(edgeName)
	val edge = existing ?: if(create) graph.addEdge(edgeName, fromNode, toNode, true).also {
		graphNode = it
	} else null
	edge?.ecoDependency?.updateEco()
	return edge
}

fun CoDependency.updateEco() {
	val edge = graphNode ?: return
	val eatMass = consumerEatMass
	// edge.setAttribute("ui.label", "소비: ${roundDisplay(eatMass)}, 비중: ${roundDisplay(weight)}")
	edge.setAttribute("ui.color", Color(0, 0, 0, (eatMass * 3 + 55).toInt().coerceIn(60, 100)))
	edge.setAttribute("ui.size", 1)
}

fun EcoSystem.getGraph() = boundGraph ?: run {
	val graph = SingleGraph("EcoSystem")
	this.boundGraph = graph
	
	producer.forEach { it.graph(graph) }
	for(level in consumers) level.forEach { it.graph(graph) }
	
	layoutAll()
	
	fun ProducingNode.addAll() {
		preyedBys.forEach {
			it.co.edgeOf(create = true)
		}
	}
	producer.forEach { it.addAll() }
	for(consumer in consumers) consumer.forEach { it.addAll() }
	
	graph
}

fun Node.graph(graph: Graph): GNode {
	graphNode?.let { return it }
	
	val node = graph.addNode(graphNodeName)
	graphNode = node
	return node
}

fun EcoSystem.layoutAll() {
	for(level in allLevels.indices) layoutLevel(level)
}

fun EcoSystem.layoutLevel(level: Int) {
	val items = allLevels[level]
	val xStart = items.size * -0.5f
	items.forEachIndexed { index, node ->
		node.graphNode?.let { graphNode ->
			graphNode.setAttribute(
				"xyz",
				xStart + index + (random.nextFloat() - 0.5f) * 0.15,
				node.level * 1 + (random.nextFloat() - 0.5f) * 0.15,
				0
			)
		}
	}
}

fun EcoSystem.updateAll() {
	allLevels.forEachNode { node ->
		node.update()
		if(node is ProducingNode) node.preyedBys.forEach { it.co.updateEco() }
	}
}
