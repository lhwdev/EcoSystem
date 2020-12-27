import org.jetbrains.kotlin.gradle.tasks.KotlinCompile

plugins {
	kotlin("jvm") version "1.4.21"
	application
}

group = "com.lhwdev.school.ecosystem"
version = "1.0-SNAPSHOT"

repositories {
	mavenCentral()
}

kotlin {
	sourceSets {
		all {
			languageSettings.useExperimentalAnnotation("kotlin.io.path.ExperimentalPathApi")
		}
	}
}

dependencies {
	// graph
	implementation("org.graphstream:gs-core:2.0")
	implementation("org.graphstream:gs-ui-swing:2.0")
	
	// concurrency / scheduling
	implementation("org.jetbrains.kotlinx:kotlinx-coroutines-core:1.4.2")
	
	testImplementation(kotlin("test-junit"))
}

tasks.test {
	useJUnit()
}

tasks.withType<KotlinCompile> {
	kotlinOptions.jvmTarget = "13"
}

application {
	mainClass.set("MainKt")
}
