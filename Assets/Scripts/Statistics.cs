using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


public struct StatisticItem {
	public string name;
	public Func<Environment, string> header;
	public Func<Environment, string> update;
}


public class Statistics : MonoBehaviour {
	public static StatisticItem[] items = {
		new StatisticItem {
			name = "개체 수",
			header = (env) => env.DumpCurrentPopulationKinds(),
			update = (env) => env.DumpCurrentPopulationCount()
		},
		new StatisticItem {
			name = "속도",
			header = (env) => "Bunny",
			update = (env) => {
				var all = env.speciesMaps[Species.Bunny].allEntities;
				return all.Count == 0 ? "?" : all.Average(e => (e as Animal).moveSpeed).ToString();
			},
		},
		new StatisticItem {
			name = "번식 욕구",
			header = (env) => "Bunny",
			update = (env) => {
				var all = env.speciesMaps[Species.Bunny].allEntities;
				return all.Average(e => (e as Animal).mateDesire).ToString();
			},
		}
	};

	public string filePath;
	public float interval;

	Environment environment;
	float lastTimeScale;

	StreamWriter[] writers;


	void Start() {
		environment = GameObject.Find("Environment").GetComponent<Environment>();
		lastTimeScale = environment.timeScale;
		filePath = Application.persistentDataPath;

		writers = new StreamWriter[items.Length];
		for(var i = 0; i < items.Length; i++) {
			var writer = File.CreateText(filePath + "/" + items[i].name + ".csv");
			writer.WriteLine(items[i].header(environment));
			writers[i] = writer;
		}

		InvokeRepeating("UpdateStatistic", 0f, interval / environment.timeScale);

	}

	void UpdateStatistic() {
		if(enabled) for(var i = 0; i < items.Length; i++) {
			writers[i].WriteLine(items[i].update(environment));
		}
	}

	void OnApplicationQuit() {
		for(var i = 0; i < items.Length; i++) {
			writers[i].Close();
		}
	}

	void Update() {
		if(environment.timeScale != lastTimeScale) {
			CancelInvoke("UpdateStatistic");
			lastTimeScale = environment.timeScale;
			InvokeRepeating("UpdateStatistic", 0.3f, interval / environment.timeScale);
		}
	}
}
