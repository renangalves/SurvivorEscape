using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {

	public static GameManager instance
	{
		get
		{
			if (_instance == null) {
				_instance = FindObjectOfType<GameManager> ();
			}
			return _instance;
		}
	}

	private static GameManager _instance;

	WorldGenerator world;

	[HideInInspector]
	public List<GameObject> spawnedEnemies = new List<GameObject> ();
	public List<GameObject> savedSurvivors = new List<GameObject> ();
	public List<Sprite> characterIcons = new List<Sprite> ();
	public List<Transform> hidingSpots = new List<Transform> ();
	public List<Transform> wallStartSpots = new List<Transform> ();
	public List<GameObject> zombieSpawnPoints = new List<GameObject> ();
	public List<AudioClip> stagesMusic = new List<AudioClip> ();
	List<GameObject> deadSurvivors = new List<GameObject>();

	public List<EnemyBehaviour> enemies = new List<EnemyBehaviour>();

	AudioSource source;

	[HideInInspector]
	public Transform outerWall;

	public GameObject moveSpotIndicator;
	public GameObject damageTakenFeedback;
	public GameObject zombie;
	[HideInInspector]
	public GameObject currentPlayer;
	public GameObject finishLevel;

	public Image currentCharacterIcon;

	[HideInInspector]
	public bool playerIsBeingChased;

	[HideInInspector]
	public int currentLevel;
	[HideInInspector]
	public int numberOfPlayableSurvivors;
	[HideInInspector]
	public int foodCollected;
	[HideInInspector]
	public int numberOfRescuableSurvivors;
	[HideInInspector]
	public int numberOfRescuedSurvivors;
	[HideInInspector]
	public int numberOfSafePlayers;
	[HideInInspector]
	public int totalOfSavedSurvivors;
	[HideInInspector]
	public int totalOfKilledZombies;
	[HideInInspector]
	public int spottedCount;
	int chaseCount;
	int deathByIllness;



	void Start () {
		currentLevel = 0;
		source = GetComponent<AudioSource> ();
		source.clip = stagesMusic [currentLevel];
		source.Play ();
		ResetHidingSpots ();

		world = FindObjectOfType<WorldGenerator> ();
	}
	



	void Update () {

		//se o jogador tiver resgatado todos os sobreviventes e os "playable survivors" (ou sobreviventes que foram resgatados em um nível anterior), então está pronto para mudar de estágio
		if (numberOfRescuableSurvivors == numberOfRescuedSurvivors && numberOfPlayableSurvivors == numberOfSafePlayers) { 
			finishLevel.SetActive (true); //ativo o texto que mostra para o jogador que está pronto para mudar de estágio
			//espero pelo jogador apertar a barra de espaco, para que se o jogador ainda quiser coletar algo no estágio antes de terminar
			if (Input.GetKeyDown (KeyCode.Space)) {
				numberOfSafePlayers = 0;
				source.Stop ();
				currentLevel++;
				//se o nível finalizado for o terceiro, então finaliza o jogo mostrando a tela de vitória
				if (currentLevel == 3)
					UIManager.instance.WinScreen (savedSurvivors.Count, totalOfKilledZombies, spottedCount);
				//senão eu reseto algumas variáveis e ativo a transicão para o próximo estágio
				else {
					int foodTotal = foodCollected;
					FoodCheck ();
					StartCoroutine (UIManager.instance.ActivateStageTransition (savedSurvivors.Count, numberOfRescuedSurvivors, foodTotal, deathByIllness));
					deathByIllness = 0;
				}
			}
		}
		//se o jogador ainda não tiver finalizado o necessário para finalizar o estágio, então o texto (que avisa que está pronto para mudar de estágio) some
		else
			finishLevel.SetActive (false);

	}


	//esta corotina irá criar o novo estágio
	public IEnumerator CreateNewStage()
	{
		foodCollected = 0;
		//destruo todos os sobreviventes que morreram
		foreach (GameObject deadSurvivor in deadSurvivors)
			Destroy (deadSurvivor);
		//destruo todos os inimigos para criar novos
		foreach (EnemyBehaviour enemy in enemies)
			Destroy (enemy.gameObject);
		enemies.Clear (); //limpo a lista enemies para adicionar novos inimigos depois
		world.ResetWorld (); //chamo o método ResetWorld do script WorldGenerator para criar o novo estágio

		yield return new WaitForSeconds (7f); //espero 7 segundos pela animacão sendo mostrada na tela de transicão para então dar Play na música 

		source.clip = stagesMusic [currentLevel];
		source.Play ();
	}


	//este método ativa e posiciona o indicador do local onde foi clicado no mapa para o personagem se mover
	public void ShowMoveSpot(Vector3 pos)
	{
		moveSpotIndicator.SetActive (true);
		pos.y = 0;
		moveSpotIndicator.transform.position = pos;
	}


	//este método esconde o indicador
	public void HideMoveSpot()
	{
		moveSpotIndicator.SetActive (false);
	}


	//este método irá fazer a mudanca de personagem quando o jogador clicar com o mouse esquerdo em outro sobrevivente na party
	public void ChangePlayers(GameObject chosenPlayer)
	{
		//se já tiver um sobrevivente selecionado, primeiro muda as variáveis dele para torná-lo selecionável e tirar movimentacão
		if (currentPlayer != null) {
			PlayerMovement currentPM = currentPlayer.GetComponent<PlayerMovement> ();
			currentPM.currentSelectedCharacter = false;
			currentPM.DisableNavMesh ();
			currentPM.movePos = currentPlayer.transform.position;
			currentPlayer.tag = "Untagged";
		}

		//torno o novo sobrevivente o personagem selecionado atual e ativo o sistema de NavMesh dele
		PlayerMovement pM = chosenPlayer.GetComponent<PlayerMovement> ();
		pM.currentSelectedCharacter = true;
		pM.EnableNavMesh ();

		currentPlayer = null;
		currentPlayer = chosenPlayer;
		currentPlayer.tag = "Player";

		//então altero tudo relacionado a UI para o novo personagem
		PlayerStatus pS = currentPlayer.GetComponent<PlayerStatus>();
		UIManager.instance.UpdateCharacterUI (pS);
		currentCharacterIcon.sprite = characterIcons [pS.index];

		GetAllEnemies ();
	}


	//este método adiciona e altera o comportamento de todos os zumbis para focar no novo sobrevivente selecionado
	public void GetAllEnemies()
	{
		if (enemies == null)
			enemies.AddRange(FindObjectsOfType<EnemyBehaviour> ());
		foreach (EnemyBehaviour enemy in enemies)
			enemy.player = currentPlayer;
	}


	//este método é chamado quando um zumbi detecta e persegue o jogador, assim alterando a variável playerIsBeingChased para dizer se o jogador pode ou não se esconder em objetos do cenário
	public void GettingChased(bool chase)
	{
		if (!chase)
			chaseCount--;
		else
			chaseCount++;
		
		if (chaseCount > 0)
			playerIsBeingChased = true;
		else
			playerIsBeingChased = false;
	}


	//este método é chamado quando o jogador se esconde atrás de um dos muros, que faz os zumbis pararem de o seguir 
	public void ResetChase()
	{
		chaseCount = 0;
		playerIsBeingChased = false;
		foreach (EnemyBehaviour enemy in enemies)
			enemy.chasingPlayer = false;
	}


	//este método faz a conta de toda comida coletada e quantidade de sobreviventes no fim do estágio. Se não for suficiente, sobreviventes irão morrer
	void FoodCheck()
	{
		foodCollected = (foodCollected * 2) - savedSurvivors.Count;
		if (foodCollected < 0) {

			while (foodCollected < 0) {
				PlayableSurvivorDied (savedSurvivors[savedSurvivors.Count-1]);
				foodCollected += 2;
				deathByIllness++;
			}
		}
	}


	//esta corotina mostra a tela vermelha quando o jogador leva dano, ativando-a e depois de 2 segundos desativando-a
	public IEnumerator ShowDamageTakenFeedback()
	{
		damageTakenFeedback.SetActive (true);

		yield return new WaitForSeconds (2);

		damageTakenFeedback.SetActive (false);
	}


	//quando o jogador morrer e perder um dos sobreviventes
	public void PlayableSurvivorDied(GameObject deadSurvivor)
	{
		//adiciono ele a lista de sobreviventes mortos para serem destruídos no final do estágio
		deadSurvivors.Add (deadSurvivor);
		//removo ele da lista de sobreviventes salvos
		savedSurvivors.Remove (deadSurvivor);
		numberOfPlayableSurvivors--; //diminuo essa contagem para saber quantos sobreviventes jogáveis (que estão na party) estão restando

		//se o número de sobreviventes jogáveis é 0, então ativa o Game Over
		if (savedSurvivors.Count == 0) {
			UIManager.instance.GameOver (totalOfSavedSurvivors, totalOfKilledZombies, currentLevel+1);
			source.Stop ();
		} else {
			//se o sobrevivente no início da lista savedSurvivors for jogável, então muda o foco para ele
			if (savedSurvivors [0].GetComponent<PlayerMovement> ().rescuedSurvivor)
				ChangePlayers (savedSurvivors [0]);
			//senão não tem mais sobrevivente jogáveis
			else {
				UIManager.instance.GameOver (totalOfSavedSurvivors, totalOfKilledZombies, currentLevel+1);
				source.Stop ();
			}
		}
	}


	//quando um sobrevivente não jogável (que ainda não está na party) morrer
	public void RescuableSurvivorDied(GameObject deadSurvivor)
	{
		deadSurvivors.Add (deadSurvivor);
		numberOfRescuableSurvivors--; //diminuo essa contagem para saber quantos sobreviventes não jogáveis restam no cenário
	}


	//este método é chamado quando o jogador dispara a arma e todos os zumbis perseguem o jogador
	public void WarnAllZombies()
	{
		spottedCount++; //esta contagem aumenta para saber quantas vezes o jogador está sendo descoberto pelos zumbis

		foreach (EnemyBehaviour enemy in enemies) {
			if (enemy != null && enemy.chasingPlayer == false) {
				enemy.chasingPlayer = true;
				GettingChased (true);
			}
		}
	}


	//este método reseta algumas variáveis que servem para checar a condicão de se o jogador pode avancar pro próximo estágio
	public void ResetSurvivorCount()
	{
		numberOfPlayableSurvivors = 0;
		numberOfRescuableSurvivors = 0;
		numberOfRescuedSurvivors = 0;
		numberOfSafePlayers = 0;

	}


	//esta corotina é chamada quando um zumbi é morto por disparo de arma, que vai criar um novo zumbi depois de um valor aleatório entre 4 a 7 segundos
	public IEnumerator SpawnZombie()
	{
		yield return new WaitForSeconds (Random.Range(4, 8));

		Instantiate (zombie, zombieSpawnPoints [Random.Range (0, 2)].transform.position, Quaternion.identity);
	}


	//este método guarda todos os pontos atrás do muro inicial onde os sobreviventes podem aparecer, é chamado quando o estágio é criado
	public void GetWallSpots(Transform wall)
	{
		wallStartSpots.Clear ();
		foreach (Transform child in wall) {
			wallStartSpots.Add (child);
		}
	}


	//este método reseta e guarda as posicões do muro final onde os sobreviventes podem aparecer
	public void ResetHidingSpots()
	{
		hidingSpots.Clear ();
		foreach (Transform child in outerWall) {
			hidingSpots.Add (child);
		}
	}
		
}
