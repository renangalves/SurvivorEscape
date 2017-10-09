using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {

	bool playerInput;

	AudioSource source;

	public Animator anim;

	float timeToReceiveInput = 7.5f;
	float timeToStartGame = 3;


	void Start () 
    {
	
		source = GetComponent<AudioSource> ();
		Invoke ("InputReady", timeToReceiveInput); //invoco o método InputReady para ativar o input do jogador no Menu principal depois de 7.5 segundo

	}
	

	void Update () 
    {
	
		//quando o jogador apertar o botão esquerdo do mouse quando o imput tiver ativado, toco o som e a animacão para transicionar para o jogo
		if (playerInput) {
			if (Input.GetMouseButtonDown (0)) {
				source.Play ();
				anim.SetTrigger ("InputPressed");
				playerInput = false;
				Invoke ("StartGame", timeToStartGame);
			}
		}

	}


	//método para ativar o input do jogador
	void InputReady()
	{
		playerInput = true;
	}

	//método chamado por Invoke para comecar o jogo no momento certo
	void StartGame()
	{
		SceneManager.LoadScene (1);
	}
}
