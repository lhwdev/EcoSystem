import java.awt.Color
import java.util.UUID
import kotlin.math.abs
import kotlin.math.sqrt
import kotlin.properties.Delegates
import kotlin.random.Random
import org.graphstream.graph.Node as GNode


// graph logic should be separated from Nodes, but I can't do that

private val sColorMap = mutableMapOf<Int, Color>()
fun colorForLevel(level: Int) = sColorMap.getOrPut(level) {
	Color(rand(100..245), rand(30..180), rand(70..220))
}

abstract class AbstractNode(
	final override val level: Int,
	override val id: String,
	override var oneMass: Double,
	biomass: Double
) : Node {
	override var ecoSystem: EcoSystem? = null
	override val uuid: UUID = UUID.randomUUID()
	override var biomass by Delegates.observable(biomass) { _, old, new ->
		massUpdate(old, new)
	}
	private var oldMass = -1.0
	private var newMass = -1.0
	protected inline val isUpdating get() = ecoSystem?.updating == true
	protected inline fun act(block: () -> Unit) {
		if(!isUpdating) block()
	}
	
	private fun massUpdate(old: Double, new: Double) {
		oldMass = old
		newMass = new
		
		update()
	}
	
	override fun update() = act {
		val deltaCount = if(oldMass == -1.0) 0 else (newMass / oneMass).toInt() - (oldMass / oneMass).toInt()
		val delta = when {
			deltaCount == 0 -> "0"
			deltaCount > 0 -> "+$deltaCount"
			else -> "-${-deltaCount}"
		}
		graphNode?.apply {
			val count = count
			if(count == 0) {
				setAttribute("ui.class", "extinct")
				setAttribute("ui.label", "멸종")
			} else {
				setHasClass("danger", count < 5 || count + deltaCount < 5)
				setAttribute("ui.label", "${roundDisplay(biomass)}kg (${count}개: $delta)")
				setAttribute("ui.size", 15 + sqrt(count.toFloat() * (level + 1)) / 2)
			}
			setHasClass("fast", abs(deltaCount) >= 10)
		}
		updateEdges()
	}
	
	protected fun updateEdges() = act {
		if(this is ProducingNode && graphNode != null) preyedBys.forEach { give ->
			val toNode = give.to
			if(toNode.graphNode != null) give.co.updateEco()
		}
	}
	
	override fun toString() = "$id(count=$count, level=$level)"
	override var graphNode: GNode? = null
		set(value) {
			field = value
			if(value != null) act {
				val color = when(this) {
					is Producer -> Color(140, 210, 100)
					is Consumer -> colorForLevel(level)
					is Decomposer -> Color(40, 40, 60)
					else -> Color.BLACK
				}
				value.setAttribute("ui.color", color)
				value.setAttribute("ecosystem.node", this)
				
				massUpdate(biomass, biomass)
			}
		}
	override var siblings = 80 / ((level + 1) * (level + 1))
}

class Producer(
	id: String,
	oneMass: Double,
	biomass: Double
) : AbstractNode(level = 0, id, oneMass, biomass), ProducingNode {
	override val preyedBys = mutableSetOf<GiveDependency>()
}

class Consumer(
	level: Int,
	id: String,
	oneMass: Double,
	biomass: Double,
	eatRatio: Double = 5.0,
) : AbstractNode(level, id, oneMass, biomass), ProducingNode, ConsumingNode {
	override var eatRatio: Double by Delegates.observable(eatRatio) { _, _, _ ->
		updateEdges()
	}
	
	override val preys = mutableSetOf<GetDependency>()
	override val preyedBys = mutableSetOf<GiveDependency>()
}

class Decomposer(
	level: Int,
	id: String,
	biomass: Double,
	oneMass: Double,
	override var eatRatio: Double = 5.0
) : AbstractNode(level, id, oneMass, biomass), ConsumingNode {
	override val preys = mutableSetOf<GetDependency>()
}
