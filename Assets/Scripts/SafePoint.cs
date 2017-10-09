using UnityEngine;
using System.Collections;

public class SafePoint : MonoBehaviour {

	public bool isSafeZone;
	public bool isSceneObject;


	//quando o jogadore encostarem no collider de um objeto ou muro para se esconderem
	void OnTriggerStay(Collider col)
	{
		if (col.tag == "Player") {
			//se não for um objeto do cenário, ou seja, não for um dos muros
			if (!isSceneObject) {
				col.gameObject.layer = 10; //mudo a layer para Hidden, que os inimigos perdem de visão
				GameManager.instance.ResetChase (); //reseto as variáveis que cuidam dos inimigos pra perseguir o jogador
			}
			else {
				//se for objeto do cenário, primeiro checo se o jogador está atualmente sendo perseguido e se o jogador não esta se movendo
				if (!GameManager.instance.playerIsBeingChased && !col.GetComponent<PlayerMovement>().isMoving)
					col.gameObject.layer = 10; //o jogador está escondido
				else
					col.gameObject.layer = 0; //o jogador não está escondido
			}
		}
	}


	//quando o jogador sair do collider de um objeto ou muro, ele não está mais escondido
	void OnTriggerExit(Collider col)
	{
		if (col.tag == "Player") {
			col.gameObject.layer = 0;
		}
	}
}
