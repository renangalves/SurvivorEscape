using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour {

	public static UIManager instance
	{
		get
		{
			if (_instance == null) {
				_instance = FindObjectOfType<UIManager> ();
			}
			return _instance;
		}
	}

	private static UIManager _instance;

	public List<Text> stageTransitionText = new List<Text>();
	public List<Text> gameOverStatsText = new List<Text>();
	public List<Text> winScreenStatsText = new List<Text>();

	public List<GameObject> heartsUI = new List<GameObject> ();
	public List<GameObject> inventoryButtons = new List<GameObject> ();

	public Text foodFoundText;
	public Text pistolAmmoText;

	public GameObject inventoryWindow;
	public GameObject woodenPlankEquipped;
	public GameObject pistolEquipped;
	public GameObject stageTransition;
	public GameObject gameOver;
	public GameObject winScreen;



	//este método será chamado para atualizar a UI quando tiver alguma mudanca como troca de arma, dano ou troca de personagem
	public void UpdateCharacterUI(PlayerStatus pS)
	{
		PlayerMovement pM = pS.GetComponent<PlayerMovement> ();
		//mostro o número de coracões na tela dependendo do valor no script do personagem
		switch (pS.playerHealth) {
		case 0:
			heartsUI [0].SetActive (false);
			heartsUI [1].SetActive (false);
			heartsUI [2].SetActive (false);
			//se for 0, então o jogador morreu, assim mando a informacões para fazer a troca pra outro personagem
			if (pM.rescuedSurvivor) {
				pM.PlayerDied ();
				pS.weaponEquipped = 0;
				GameManager.instance.PlayableSurvivorDied (pS.gameObject);
			}
			else
				GameManager.instance.RescuableSurvivorDied (pS.gameObject);
			break;
		case 1:
			heartsUI [0].SetActive (true);
			heartsUI [1].SetActive (false);
			heartsUI [2].SetActive (false);
			break;
		case 2:
			heartsUI [0].SetActive (true);
			heartsUI [1].SetActive (true);
			heartsUI [2].SetActive (false);
			break;
		case 3:
			heartsUI [0].SetActive (true);
			heartsUI [1].SetActive (true);
			heartsUI [2].SetActive (true);
			break;
		}

		//aqui chamo o respectivo método para mostrar na tela qual arma o jogador tem escolhido
		switch (pS.weaponEquipped) {
		case 1:
			UIManager.instance.OnWoodenPlankButtonClick ();
			break;
		case 2:
			UIManager.instance.OnPistolButtonClick ();
			pistolAmmoText.text = "Bullets: " + pS.bulletsLeft;
			break;
		case 0:
			UIManager.instance.OnNoWeaponEquipped ();
			break;
		}


	}


	//esta corotina vai mostrar a tela de passagem de level, com as informacões necessárias, usando os intervalos "yield return new WaitForSeconds" para mostrar um de cada vez
	public IEnumerator ActivateStageTransition(int survivorsCount, int stageSurvivorsSaved, int foodCount, int deadSurvivors)
	{
		stageTransition.SetActive (true);
        stageTransition.GetComponent<Animator>().Play("Appear", 0, 0f); //Comeca a animacão do inicio

		stageTransitionText [0].text = "Survivors Saved: " + survivorsCount;
		stageTransitionText [1].text = "Food Found: " + foodCount;
		if(deadSurvivors > 0)
			stageTransitionText [2].text = deadSurvivors + " Survivors fell ill and died. " + (stageSurvivorsSaved - deadSurvivors) + " Survivors joined your party!";
		else
			stageTransitionText [2].text = stageSurvivorsSaved + " Survivors joined your party!";

		yield return new WaitForSeconds (3f);

		StartCoroutine (GameManager.instance.CreateNewStage ());

		yield return new WaitForSeconds (10f);

		stageTransition.SetActive (false);
	}


	//se o jogador perder, este método ativa e mostra a tela de game over com os dados
	public void GameOver(int survivorsSavedTotal, int zombiesKilledTotal, int level)
	{
		gameOver.SetActive (true);
		gameOverStatsText [0].text = "Survivors Saved: " + survivorsSavedTotal;
		gameOverStatsText [1].text = "Zombies Killed: " + zombiesKilledTotal;
		gameOverStatsText [2].text = "Level: " + level;
	}


	//se o jogador apertar o botão Continue na tela de game over, o jogo recomeca
	public void OnContinueButton()
	{
		SceneManager.LoadScene (1);
	}


	//se o jogador apertar o botão Quit na tela de game over, o jogo finaliza
	public void OnQuitButton()
	{
		SceneManager.LoadScene (0);
	}


	//este método mostra a tela de vitória, com os dados de como o jogador perfomou
	public void WinScreen(int survivorsSavedTotal, int zombiesKilledTotal, int spottedAmount)
	{
		winScreen.SetActive (true);
		if(survivorsSavedTotal == 7)
			winScreenStatsText[0].text = "You rescued all survivors!";
		else
			winScreenStatsText[0].text = "You rescued " + survivorsSavedTotal + " survivors!";

		winScreenStatsText[1].text = "Total zombies killed: " + zombiesKilledTotal;

		//aqui mostra a nota do quão stealthy ele foi, dependendo do quanto ele foi visto por zumbis
		if(spottedAmount == 0)
			winScreenStatsText [2].text = "Stealth Rating \n SS";
		else if(spottedAmount > 0 && spottedAmount <= 2)
			winScreenStatsText [2].text = "Stealth Rating \n S";
		else if(spottedAmount > 2 && spottedAmount <= 5)
			winScreenStatsText [2].text = "Stealth Rating \n A";
		else if(spottedAmount > 5 && spottedAmount <= 8)
			winScreenStatsText [2].text = "Stealth Rating \n B";
		else if(spottedAmount > 8 && spottedAmount <= 11)
			winScreenStatsText [2].text = "Stealth Rating \n C";
		else
			winScreenStatsText [2].text = "Stealth Rating \n D";

		//aqui mostra a nota do quanto ele entrou em combate
		if(zombiesKilledTotal > 12)
			winScreenStatsText [3].text = "Combat Rating \n SS";
		else if(zombiesKilledTotal == 12)
			winScreenStatsText [3].text = "Combat Rating \n S";
		else if(zombiesKilledTotal < 12 && zombiesKilledTotal >= 10)
			winScreenStatsText [3].text = "Combat Rating \n A";
		else if(zombiesKilledTotal < 10 && zombiesKilledTotal >= 8)
			winScreenStatsText [3].text = "Combat Rating \n B";
		else if(zombiesKilledTotal < 8 && zombiesKilledTotal >= 6)
			winScreenStatsText [3].text = "Combat Rating \n C";
		else
			winScreenStatsText [3].text = "Combat Rating \n D";
	}


	//na tela de vitória, se o jogador apertar o botão Continue, volta para o Main Menu
	public void OnWinScreenContinueButton()
	{
		SceneManager.LoadScene (0);
	}


	//se o jogador apertar o botão de inventário, o jogo pausa e a janela do inventário aparece
	public void OnInventoryButtonClick()
	{
        inventoryWindow.GetComponent<Animator>().Play("Appear", 0, 0f); //Comeca a animacão do inicio
		inventoryWindow.SetActive (true);
		Time.timeScale = 0f;
		foodFoundText.text = "Food found: " + GameManager.instance.foodCollected; 
		PlayerStatus pS = GameManager.instance.currentPlayer.GetComponent<PlayerStatus> ();

		//ativo no inventário os itens que o jogador tem com o personagem atual
		ActivateInventoryButtons (pS.hasWoodenPlank, 0);
		ActivateInventoryButtons (pS.hasPistol, 1);
		ActivateInventoryButtons (pS.hasMedkit, 2);
	}


	//este método mostra no inventário se o jogador tem um certo item
	void ActivateInventoryButtons(bool playerHas, int pos)
	{
		if (playerHas)
			inventoryButtons [pos].SetActive (true);
		else
			inventoryButtons [pos].SetActive (false);
	}


	//quando o botão de fechar for clicado, o inventário é fechado e o jogo é despausado
	public void OnCloseInventoryButtonClick()
	{
		inventoryWindow.SetActive (false);
		Time.timeScale = 1f;
	}


	//se o botão do pedaco de madeira for clicado no inventário, o personagem é equipado com ela e é mostrado na parte "Equipped" da tela
	public void OnWoodenPlankButtonClick()
	{
		GameManager.instance.currentPlayer.GetComponent<PlayerStatus> ().weaponEquipped = 1;
		woodenPlankEquipped.SetActive (true);
		pistolEquipped.SetActive (false);
	}


	//se o botão da arma for clicado no inventário, o personagem é equipado com ela e é mostrado na parte "Equipped" da tela, junto com a quantidade de balas
	public void OnPistolButtonClick()
	{
		GameManager.instance.currentPlayer.GetComponent<PlayerStatus> ().weaponEquipped = 2;
		woodenPlankEquipped.SetActive (false);
		pistolEquipped.SetActive (true);
		pistolAmmoText.text = "Bullets: " + GameManager.instance.currentPlayer.GetComponent<PlayerStatus> ().bulletsLeft;
	}


	//quando o pedaco de madeira quebrar ou o jogador trocar de personagem, nada estará equipado, então este método desativa as imagens
	public void OnNoWeaponEquipped()
	{
		woodenPlankEquipped.SetActive (false);
		pistolEquipped.SetActive (false);
	}


	//se o kit de cura for clicado, então aumenta a vida do personagem, atualiza a UI dele na tela e desaparece com o kit de cura
	public void OnMedkitButtonClick()
	{
		PlayerStatus pS = GameManager.instance.currentPlayer.GetComponent<PlayerStatus> ();
		pS.playerHealth = 3;
		UpdateCharacterUI (pS);
		pS.hasMedkit = false;
		ActivateInventoryButtons (pS.hasMedkit, 2);
	}
}
