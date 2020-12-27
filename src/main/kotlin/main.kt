import org.graphstream.graph.Edge
import org.graphstream.graph.Element
import org.graphstream.ui.graphicGraph.GraphicGraph
import org.graphstream.ui.swing_viewer.DefaultView
import org.graphstream.ui.swing_viewer.SwingViewer
import org.graphstream.ui.view.View
import org.graphstream.ui.view.Viewer
import org.graphstream.ui.view.ViewerListener
import org.graphstream.ui.view.util.ShortcutManager
import java.awt.Dimension
import java.awt.event.KeyEvent
import java.awt.event.KeyListener
import java.nio.file.StandardOpenOption
import java.util.UUID
import java.util.concurrent.ConcurrentLinkedQueue
import java.util.concurrent.atomic.AtomicLong
import javax.swing.*
import kotlin.concurrent.thread
import kotlin.io.path.Path
import kotlin.io.path.outputStream
import kotlin.random.Random
import kotlin.random.nextInt
import org.graphstream.graph.Node as GNode


fun rand(range: IntRange) = Random.Default.nextInt(range)


class Lock

private inline operator fun <R> Lock.invoke(block: () -> R) = /*synchronized(this, block)*/ block()

@OptIn(ExperimentalUnsignedTypes::class)
fun main() {
	val producers = buildSet(7) { Producer("$it", oneMass = .5, biomass = 2.0 * 4000) }
	val consumer1 =
		buildSet(5) { Consumer(1, "$it", oneMass = 2.0, biomass = 2.0 * 500, eatRatio = 3.5) }
	val consumer2 =
		buildSet(3) { Consumer(2, "$it", oneMass = 6.0, biomass = 6.0 * 18, eatRatio = 5.0) }
	val consumer3 =
		buildSet(1) { Consumer(3, "$it", oneMass = 13.0, biomass = 27.0 * 3, eatRatio = 7.5) }
	val ecoSystem = EcoSystem(500, .0f, producers, mutableListOf(consumer1, consumer2, consumer3))
	
	System.setProperty("org.graphstream.ui", "swing")
	val graph = ecoSystem.getGraph()
	graph.setAttribute(
		"ui.stylesheet", """
		node {
			size: 15px;
			size-mode: dyn-size;
			fill-mode: dyn-plain;
			stroke-width: 3px;
			text-size: 19px;
			text-offset: 0, -21px;
		}
		
		node.danger {
			text-color: #c76;
			text-style: bold;
		}
		
		node.extinct {
			text-color: #e30;
			text-style: bold;
			stroke-mode: plain;
		}
		
		node.fast {
			stroke-mode: dots;
		}
		
		edge {
			size-mode: dyn-size;
			fill-mode: dyn-plain;
			text-size: 15px;
			text-color: #57c;
		}
		
		node.selected {
			fill-mode: plain;
			fill-color: black;
		}
		edge.selected {
			fill-mode: plain;
			fill-color: black;
			size-mode: normal;
			size: 3px;
		}
	""".trimIndent()
	)
	graph.setAttribute("ui.antialias")
	
	val window = JFrame("MyWindow")
	val panel = JPanel()
	val viewer = SwingViewer(graph, Viewer.ThreadingModel.GRAPH_IN_ANOTHER_THREAD)
	val defaultView = viewer.addDefaultView(false) as DefaultView
	defaultView.enableMouseOptions()
	defaultView.preferredSize = Dimension(1600, 800)
	panel.add(defaultView)
	val pipe = viewer.newViewerPipe()
	val taskQueue = ConcurrentLinkedQueue<() -> Unit>()
	
	val text = JTextArea()
	text.isEditable = false // workaround for multiline: JLabel does not support multiline (without html)
	text.font = text.font.deriveFont(15f)
	panel.add(text)
	
	window.add(panel)
	window.setSize(1600, 900)
	window.defaultCloseOperation = JFrame.EXIT_ON_CLOSE
	window.isVisible = true
	
	fun uiThread(block: () -> Unit) {
		SwingUtilities.invokeLater(block)
	}
	
	var selection: Element? = null
	var isShiftPressed = false
	var ecoTicking = false
	val wait = AtomicLong(500L)
	var tickCount = 5
	
	val lock = Lock()
	
	val writer = Path("D:\\LHW\\develop\\school\\science\\EcoSystem\\csvs\\out.csv")
		.outputStream(StandardOpenOption.CREATE).writer()
	writer.appendLine(ecoSystem.allLevels.indices.joinToString(separator = ",") {
		"#" + colorForLevel(it).rgb.toUInt().toString(16).padStart(8, '0')
	})
	
	Runtime.getRuntime().addShutdownHook(thread(start = false) {
		writer.flush()
		writer.close()
	})
	
	fun tick() = lock {
		ecoSystem.tick(tickCount)
		val result = ecoSystem.allLevels.joinToString(separator = ",") { level ->
			level.sumOf { it.biomass }.toString()
		}
		writer.appendLine(result)
		println(result)
		ecoSystem.updateAll()
	}
	
	fun updateEcoTick() {
		if(ecoTicking) taskQueue.offer {
			if(ecoTicking) {
				tick()
				updateEcoTick()
			}
		}
	}
	
	fun unselect() {
		selection?.let { node ->
			node.setHasClass("selected", false)
		}
		selection = null
	}
	
	fun select(node: Element) {
		unselect()
		selection = node
		node.setHasClass("selected", true)
		
		when(node) {
			is GNode -> with(node.ecoNode) {
				uiThread {
					text.text = """
							개체군 $id($kind): ${level}단계, ${roundDisplay(biomass)}kg (개체수: ${count}, 평균 개체 생물량: $oneMass)
							평균 번식 수: $siblings${if(this is ConsumingNode) ", 소비 효율: $eatRatio" else ""}
						""".trimIndent()
				}
			}
			
			is Edge -> with(node.ecoDependency) {
				uiThread {
					text.text = """
						의존관계 $from -> $to, 비중: $weight
					""".trimIndent()
				}
			}
		}
	}
	
	defaultView.setShortcutManager(object : ShortcutManager, KeyListener {
		override fun init(graph: GraphicGraph?, view: View) {
			defaultView.addKeyListener(this)
		}
		
		override fun release() {
			defaultView.removeKeyListener(this)
		}
		
		override fun keyTyped(e: KeyEvent) = lock {
			if(e.modifiersEx == 0) when(e.keyChar.toUpperCase().toInt()) { // no control, no shift, etc.
				KeyEvent.VK_DELETE -> when(val node = selection) {
					is GNode -> ecoSystem.extinct(node.ecoNode)
					is Edge -> node.ecoDependency.remove()
				}
				KeyEvent.VK_SPACE -> tick()
				KeyEvent.VK_ESCAPE -> unselect()
				KeyEvent.VK_N -> { //new node
					// [level] becomes one of selected node
					val selectionLevel = (selection as? GNode)?.ecoNode?.level ?: JOptionPane.showInputDialog(
						"생물단계를 입력해주세요. (0=생산자, -1=분해자)"
					)?.toIntOrNull()?.let {
						if(it == -1) ecoSystem.levelCount - 1 else it
					}?.coerceIn(0, ecoSystem.levelCount - 1) ?: return
					
					fun defaultOneMassForLevel(level: Int) = level * level + 1
					
					val oneMass = defaultOneMassForLevel(selectionLevel) * (0.7 + 0.6 * ecoSystem.random.nextDouble())
					val biomass = oneMass * rand(selectionLevel..selectionLevel * selectionLevel * 3 + 3)
					when(selectionLevel) {
						0 -> {
							val node = Producer(UUID.randomUUID().toString(), oneMass, biomass)
							ecoSystem.producer += node
							node.ecoSystem = ecoSystem
							node.graph(graph)
						}
						else -> {
							val node = Consumer(selectionLevel, UUID.randomUUID().toString(), oneMass, biomass)
							ecoSystem.consumers[selectionLevel - 1].add(node)
							node.ecoSystem = ecoSystem
							node.graph(graph)
						}
					}
				}
				
				KeyEvent.VK_W -> {
					(selection as? Edge)?.let {
						val weight = JOptionPane.showInputDialog("비중을 입력해주세요.")
							?.toDoubleOrNull() ?: return
						it.ecoDependency.weight = weight
					}
				}
				
				KeyEvent.VK_R -> {
					val node = (selection as? GNode)?.ecoNode as? ConsumingNode ?: return
					val eatRatio = JOptionPane.showInputDialog("소비효율을 입력해주세요.")?.toDoubleOrNull() ?: return
					node.eatRatio = eatRatio
				}
				
				KeyEvent.VK_M -> {
					val node = (selection as? GNode)?.ecoNode ?: return
					val mass = JOptionPane.showInputDialog("생물량을 입력해주세요.")?.toDoubleOrNull() ?: return
					node.biomass = mass
				}
				
				KeyEvent.VK_F -> {
					val fairNess = JOptionPane.showInputDialog("공평함 비율을 입력해주세요.")?.toDoubleOrNull() ?: return
					ecoSystem.fairness = fairNess
				}
				
				KeyEvent.VK_D -> {
					wait.set(JOptionPane.showInputDialog("기다리는 시간을 입력해주세요. (밀리초)")?.toLongOrNull() ?: return)
					return
				}
				
				KeyEvent.VK_T -> {
					tickCount = JOptionPane.showInputDialog("틱 수를 입력해주세요.")?.toIntOrNull() ?: return
					return
				}
				
				KeyEvent.VK_S -> {
					ecoSystem.speed = JOptionPane.showInputDialog("속도 배율을 입력해주세요.")?.toDoubleOrNull() ?: return
					return
				}
				
				else -> return
			} else if(e.modifiersEx == KeyEvent.SHIFT_DOWN_MASK) {
				// only shift, no control/alt/etc.
				when(e.keyChar.toUpperCase().toInt()) {
					KeyEvent.VK_SPACE -> {
						// start/end tick
						val ticking = !ecoTicking
						ecoTicking = ticking
						updateEcoTick()
					}
					else -> return
				}
			} else {
				return
			}
			
			ecoSystem.updateAll()
		}
		
		override fun keyPressed(e: KeyEvent) = lock {
			if(e.keyCode == KeyEvent.VK_SHIFT) isShiftPressed = true
		}
		
		override fun keyReleased(e: KeyEvent) = lock {
			if(e.keyCode == KeyEvent.VK_SHIFT) isShiftPressed = false
		}
	})
	
	pipe.addViewerListener(object : ViewerListener {
		override fun viewClosed(viewName: String) {
		}
		
		override fun buttonPushed(id: String) = lock {
			val last = selection
			val new = graph.getNode(id)
			
			if(isShiftPressed) { // select edge
				// if the edge does not exist, create one
				val lastNode = when(last) {
					is GNode -> last
					is Edge -> last.sourceNode
					else -> return
				}
				val lastEco = lastNode.ecoNode
				val newEco = new.ecoNode
				val dependency = (newEco as? ConsumingNode)?.let {
					(lastEco as? ProducingNode)?.edgeTo(it)
				} ?: run {
					val (from, to) = when {
						// if both are Consumer, become last -> new (due to sequential execution)
						lastEco is ProducingNode && newEco is ConsumingNode -> lastEco to newEco
						lastEco is ConsumingNode && newEco is ProducingNode -> newEco to lastEco
						else -> {
							JOptionPane.showMessageDialog(null, "생산자끼리나 분해자끼리 연결할 수 없습니다.")
							return
						}
					}
					if(to is ProducingNode && from is ConsumingNode && to.edgeTo(from) != null) {
						JOptionPane.showMessageDialog(null, "이미 포식자, 피식자 관계가 반대로 형성되어 있습니다.")
						return
					}
					
					from.connectTo(to, weight = ecoSystem.randomWeight())
				}
				
				val edge = dependency.edgeOf(create = true)
				if(edge != null) {
					dependency.updateEco()
					unselect()
					select(edge)
				}
			} else {
				select(new)
			}
			
			ecoSystem.updateAll()
		}
		
		override fun buttonReleased(id: String) {
		}
		
		override fun mouseOver(id: String) {
		}
		
		override fun mouseLeft(id: String) {
		}
	})
	
	defaultView.requestFocus()
	
	thread {
		while(true) {
			Thread.sleep(wait.get())
			
			taskQueue.poll()?.let { lock(it) }
		}
	}
	
	while(true) {
		pipe.blockingPump()
	}
}

