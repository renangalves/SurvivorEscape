using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WorldGenerator : MonoBehaviour {

	List<GameObject> spawnedObjects = new List<GameObject> ();
	List<GameObject> spawnedResources = new List<GameObject> ();
	List<GameObject> spawnedSurvivors = new List<GameObject> ();
	List<GameObject> spawnedZombies = new List<GameObject> ();


	public GameObject[] obstacles;
	public GameObject[] resources;
	public GameObject[] survivors;
	public GameObject zombie;
	public GameObject centerPoint;
	public GameObject hidingPos;

	int numberOfZombies = 2;
	int survivorCounter = 1;
	int survivorCounterLimit = 3;
	int wallPlacementRadius = 30;
	int objectPlacementRadius = 25;
	int ignoreLayer = 9;
	int foodCount;
	[HideInInspector]
	public int level = 0;

	Vector2 wallPlacement;
	Vector3 newPos;

	bool distanceCheck;

	float distance;


	void Start () {

		GenerateLevel ();

	}

	//este método gera o estágio, criando e movendo objetos, sobreviventes e inimigos
	public void GenerateLevel()
	{
		level = GameManager.instance.currentLevel; //pego qual estágio que o jogo está no momento
		wallPlacement = Random.insideUnitCircle.normalized * wallPlacementRadius; //adiciono no aux uma posicão aleatória no cenário. O normalized serve para pegar o ponto extremo do círculo, onde o muro vai estar

		//gero os dois muros, um sendo oposto ao outro
		spawnedObjects.Add(Instantiate (obstacles[0], new Vector3(wallPlacement.x, 0, wallPlacement.y), Quaternion.identity) as GameObject);
		spawnedObjects.Add(Instantiate (obstacles[1], new Vector3(-wallPlacement.x, 0, -wallPlacement.y), Quaternion.identity) as GameObject);

		//rotaciono os dois para ficarem de frente um pro outro, virando para o ponto central do mapa
		spawnedObjects[0].transform.LookAt (centerPoint.transform);
		spawnedObjects[1].transform.LookAt (centerPoint.transform);

		//envio as informacões para o GameManager das paredes que foram criadas para pegar os pontos onde os sobrevivente se escondem
		GameManager.instance.GetWallSpots (spawnedObjects[0].transform.Find("HidingPositions")); 
		GameManager.instance.outerWall = spawnedObjects [1].transform.Find ("HidingPositions");

		//se o estágio for o primeiro, então tenho que instanciar o primeiro sobrevivente jogável
		if (level == 0) {
			//dou spawn no survivor na posicão segura atras do muro
			spawnedSurvivors.Add (Instantiate (survivors [0], spawnedObjects [0].transform.Find ("HidingSpot").position, Quaternion.identity) as GameObject);
			spawnedSurvivors [0].GetComponent<PlayerMovement> ().rescuedSurvivor = true; //indico aqui que este é um sobrevivente jogável
			GameManager.instance.savedSurvivors.Add (spawnedSurvivors [0]);
			GameManager.instance.numberOfPlayableSurvivors++;
			foodCount++; //este contador aumenta para saber quantos mantimentos serão criados mais abaixo

		//se não for o primeiro estágio, então apenas movo os sobreviventes já existentes para o novo local do muro inicial
		} else {
			List<GameObject> spawnRescuedSurvivors = new List<GameObject> ();
			spawnRescuedSurvivors.AddRange (GameManager.instance.savedSurvivors); //pego a informacão dos sobreviventes salvos

			//para cada sobrevivente eu mudo o local dele para a posicão no muro apropriada e faco dele um sobrevivente jogável
			foreach (GameObject spawn in spawnRescuedSurvivors) {
				spawn.transform.position = GameManager.instance.wallStartSpots [spawn.GetComponent<PlayerStatus>().index].position;
				PlayerMovement pM = spawn.GetComponent<PlayerMovement> ();
				pM.rescuedSurvivor = true;
				pM.ResetSurvivor (); //este método reseta qualquer variável que fazia dele um sobrevivente não jogável
				foodCount++; //este contador aumenta para saber quantos mantimentos serão criados mais abaixo
				GameManager.instance.numberOfPlayableSurvivors++;
			}
		}

		//crio no cenário os obstáculos (barris e caixa)
		for (int i = 2; i < obstacles.Length; i++) {
			FindSpot (8); //pego aqui uma posicão no mapa onde fica 8 metros de distância de outros objetos
			GameObject newObject = Instantiate (obstacles[i], newPos, Quaternion.identity) as GameObject;
			spawnedObjects.Add (newObject);

			//crio aqui os 2 sobreviventes não jogáveis, que ficam perto dos objetos 
			if (survivorCounter < survivorCounterLimit) {
				newPos.x += 4;
				spawnedSurvivors.Add(Instantiate (survivors [survivorCounter], newPos, Quaternion.identity) as GameObject);
				survivorCounter++;
				GameManager.instance.numberOfRescuableSurvivors++;
				foodCount++;
			}
		}

		survivorCounterLimit += 2; //adiciono 2 a este limite para seguir na ordem da lista de sobreviventes de 2 em 2, a cada estágio

		//faco o cálculo de quantos mantimentos serão criados. Com 3 sobreviventes no cenário aparecerão 2 mantimentos, pois cada mantimento mantém 2 sobreviventes
		if (foodCount % 2 != 0) {
			foodCount = (foodCount / 2) + 1;
		} else
			foodCount = foodCount / 2;

		//crio os mantimentos
		for (int i = 0; i < foodCount; i++) {
			FindSpot (2); //pego aqui uma posicão no mapa onde fica 2 metros de distância de outros objetos
			GameObject newObject = Instantiate (resources[0], newPos, Quaternion.identity) as GameObject;
			spawnedResources.Add (newObject);
			spawnedObjects.Add (newObject);
		}

		foodCount = 0; //reseto o foodCount para a próxima vez que um cenário for gerado

		//crio os outros recursos, como a arma, pedaco de madeira e kit médico
		for (int i = 1; i < resources.Length; i++) {
			FindSpot (2); //pego aqui uma posicão no mapa onde fica 2 metros de distância de outros objetos
			GameObject newObject = Instantiate (resources[i], newPos, Quaternion.identity) as GameObject;
			spawnedResources.Add (newObject);
			spawnedObjects.Add (newObject);
		}

		//mudo a layer dos recursos para IgnoreRaycast, para que assim os zumbis tenham mais espaco para serem criados e o jogador não conseguir clicar neles
		foreach (GameObject resource in spawnedResources) {
			resource.layer = 2;
		}

		//crio aqui os zumbis
		for (int i = 0; i < numberOfZombies; i++) {
			FindSpot (4); //pego aqui uma posicão no mapa onde fica 4 metros de distância de outros objetos
			GameObject newObject = Instantiate (zombie, newPos, Quaternion.identity) as GameObject;
			spawnedZombies.Add (newObject);
		}

		numberOfZombies += 2; //a cada novo estágio, mais 2 zumbis aparecerão

		//mudo todos os objetos para a layer IgnoreRaycast para evitar deteccão de cliques do mouse no exterior deles
		for (int i = 2; i < spawnedObjects.Count; i++) {
			spawnedObjects [i].layer = 2;
		}

		//se for o primeiro level eu coloco o survivor 1 para ser controlado pelo jogador
		if (level == 0)
			GameManager.instance.ChangePlayers (spawnedSurvivors [0]);
		else
			GameManager.instance.GetAllEnemies ();
	}


	//este método irá salvar em newPos a posicão onde não há nenhum objeto em um radius maxDist por perto, para evitar de objetos aparecerem um em cima do outro
	void FindSpot(int maxDist)
	{
		Collider[] neighbours;

		do {
			wallPlacement = Random.insideUnitCircle * objectPlacementRadius;
			newPos = new Vector3 (wallPlacement.x, 0, wallPlacement.y);
			neighbours = Physics.OverlapSphere (newPos, maxDist, ignoreLayer);
		} while(neighbours.Length > 0);
	}


	//este método cuida de destruir o cenário atual e criar um novo
	public void ResetWorld()
	{
		ResetList (spawnedObjects);
		ResetList (spawnedResources);
		GameManager.instance.ResetSurvivorCount ();
		GenerateLevel ();
		GameManager.instance.ResetHidingSpots ();
	}


	//este método destrói os objetos enviados pelo ResetWorld
	void ResetList(List<GameObject> clearList)
	{
		foreach (GameObject clear in clearList) 
			Destroy (clear);
		clearList.Clear ();
	}
}
