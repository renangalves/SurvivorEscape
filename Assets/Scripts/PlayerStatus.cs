using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class PlayerStatus : MonoBehaviour {

	public GameObject missedAttackIndicator;

	[HideInInspector]
	public int playerHealth = 3;
	[HideInInspector]
	public int bulletsLeft;
	[HideInInspector]
	public int weaponEquipped;
	public int index;
	int woodenPlankAttacksLeft;

	float invulnerableTime = 2;
	float invulnerableTimer;
	float frontMeleeAttackTime = 3f;
	float frontMeleeAttackTimer;
	float closeFiringDistance= 30;
	float mediumFiringDistance= 60;

	[HideInInspector]
	public bool hasWoodenPlank;
	[HideInInspector]
	public bool hasPistol;
	[HideInInspector]
	public bool hasMedkit;
	bool isInvulnerable;
	bool woodenPlankFrontAttack;

	PlayerMovement pM;


	void Start () {
		pM = GetComponent<PlayerMovement> ();
		weaponEquipped = 0;
	}
	

	void Update () {

		//se o jogador tomar dano, ficará invulnerável por dois segundos
		if (isInvulnerable) {
			invulnerableTimer += Time.deltaTime;
			if (invulnerableTimer >= invulnerableTime) {
				isInvulnerable = false;
				invulnerableTimer = 0;
			}
		}

		//se o jogador atacar o zumbi pela frente, então terá um cooldown de 3 segundos até o próximo ataque
		if (woodenPlankFrontAttack) {
			frontMeleeAttackTimer += Time.deltaTime;
			if (frontMeleeAttackTimer >= frontMeleeAttackTime) {
				woodenPlankFrontAttack = false;
				frontMeleeAttackTimer = 0;
			}
		}
			

	}


	//este método irá realizar o disparo da arma e decidir se irá acertar ou não o alvo
	public void ShootPistol(float distance, GameObject target)
	{
		EnemyBehaviour eB = target.GetComponent<EnemyBehaviour> ();

		//se tiver ainda balas disponíveis na arma
		if (bulletsLeft > 0) {
			bulletsLeft--;
			int chanceToKill;
			int chanceToWarn = 10;

			//chanceToKill recebe a chance de matar o zumbi baseado no distance
			if (distance < closeFiringDistance) {
				chanceToKill = 8;
			} else if (distance > closeFiringDistance && distance < mediumFiringDistance) {
				chanceToKill = 5;
			} else
				chanceToKill = 2;

			//se o número de 0 a 10 selecionado for menor que a chance de matar então matou o zumbi
			if (Random.Range (0, 11) <= chanceToKill) {
				eB.enemyHealth -= 2;
				eB.ShowDamageIndicator (2);
				StartCoroutine (GameManager.instance.SpawnZombie ()); //este método irá criar um novo zumbi depois de um delay

			//senão o personagem errou o disparo, e um indicador dizendo MISS aparece para avisar
			} else {
				GameObject indicator = Instantiate (missedAttackIndicator, this.gameObject.transform.position, Quaternion.identity) as GameObject;
				Destroy (indicator, 3);
			}

			//se o número de 0 a 10 selecionado for menor que a chance de alertar todos os zumbis
			if (Random.Range (0, 11) <= chanceToWarn) {
				GameManager.instance.WarnAllZombies (); //alerta todos os zumbis
			}

			//atualizo a quantidade de balas na tela
			UIManager.instance.pistolAmmoText.text = "Bullets: " + bulletsLeft;
		}
	}


	//este método trata da mudanca de variáveis caso o sobrevivente que estava seguindo o jogador morrer
	void FollowingSurvivorDied()
	{
		pM.PlayerDied ();
		GameManager.instance.RescuableSurvivorDied (this.gameObject);

		//altero no script do sobrevivente, que escoltava o sobrevivente que morreu, algumas variáveis para que ele possa escoltar outro sobrevivente novamente
		pM.characterToFollow.GetComponent<PlayerMovement> ().beingFollowed = false;
		pM.characterToFollow.GetComponent<PlayerMovement> ().survivorFollowing = null;
		pM.survivorBeingSaved = false;

		this.gameObject.GetComponent<BoxCollider> ().enabled = false; //o personagem não será mais clicável
	}


	void OnTriggerEnter(Collider col)
	{
		//se o jogador colidir com mantimento, adiciono no GameManager
		if (col.tag == "Food") {
			GameManager.instance.foodCollected++;
			Destroy (col.gameObject);
		}

		//se o jogador colidir com um kit médico, ele terá acesso para usá-lo no inventário
		if (col.tag == "Medkit") {
			hasMedkit = true;
			Destroy (col.gameObject);
		}

		//se o jogador colidir com o pedaco de madeira, ele terá acesso para equipá-lo no inventário
		if (col.tag == "WoodenPlank") {
			hasWoodenPlank = true;
			woodenPlankAttacksLeft = 2; //número de ataques restantes para o pedaco de madeira
			Destroy (col.gameObject);
		}

		//se o jogador colidir com a arma, ele terá acesso para equipá-la no inventário. Se ele já tinha uma arma, o número de balas vai pra 5 novamente
		if (col.tag == "Pistol") {
			hasPistol = true;
			bulletsLeft = 5;

			//se a arma equipada no momento é a arma, então já atualiza a quantidade de balas na tela
			if(weaponEquipped == 2)
				UIManager.instance.pistolAmmoText.text = "Bullets: " + bulletsLeft;
			
			Destroy (col.gameObject);
		}
			
	}


	void OnTriggerStay(Collider col)
	{
		//se o sobrevivente colidir com um inimigo
		if (col.tag == "Enemy") {
			EnemyBehaviour eB = col.GetComponent<EnemyBehaviour> ();

            ManageEnemyCollision(eB);
		}
	}	


    void ManageEnemyCollision(EnemyBehaviour eB)
    {
        //se for o sobrevivente sendo controlado pelo jogador
        if (pM.rescuedSurvivor) {

            //se o jogador estiver perseguindo o zumbi (ou seja, clicou nele para movimentar o personagem até ele)
            if (pM.characterToFollow != null && pM.characterToFollow.tag == "Enemy") {

                //se o jogador tiver equipado o pedaco de madeira e o inimigo não estiver morto
                if (weaponEquipped == 1 && eB.enemyHealth > 0) {

                    //esta condicão é para ver se o zumbi, que esta sendo colidido, está atualmente perseguindo o jogador, que quer dizer que não será um ataque stealth, e sim frontal
                    if (eB.chasingPlayer) {

                        //esta condicão é o cooldown para cada ataque do jogador, levando 3 segundos para poder atacar frontal de novo
                        if (!woodenPlankFrontAttack) {

                            //chance de 50% de acerto, causando 1 de dano no zumbi
                            if (Random.Range (0, 2) == 0) {
                                eB.enemyHealth--;
                                eB.ShowDamageIndicator (1); //um indicador mostrando 1 aparece dando o feedback que o zumbi levou 1 de dano
                                woodenPlankAttacksLeft--; //diminui quantos ataques o pedaco de madeira pode dar (2 no máximo)

                                //se o jogador não acertar o ataqye, um indicador escrito MISS aparece dando o feedback que o jogador errou o ataque
                            } else {
                                GameObject indicator = Instantiate (missedAttackIndicator, this.gameObject.transform.position, Quaternion.identity) as GameObject;
                                Destroy (indicator, 3);
                            }

                            transform.LookAt (eB.gameObject.transform); //faco o personagem olhar diretamente pro zumbi que esta atacando
                            woodenPlankFrontAttack = true; //comeca o cooldown do ataque no Update

                            //paro o movimento do personagem para ele não andar até "entrar" dentro do zumbi
                            pM.followCharacter = false;
                            pM.movePos = transform.position;
                        }

                        //se o zumbi não estiver perseguindo o jogador, então o ataque é stealth e vai matar o zumbi em um acerto
                    } else {
                        eB.enemyHealth -= 2;
                        eB.ShowDamageIndicator (2); //um indicador mostrando 2 aparece dando o feedback que o zumbi levou 2 de dano
                        woodenPlankAttacksLeft--; //diminui quantos ataques o pedaco de madeira pode dar (2 no máximo)

                        //paro o movimento do personagem pois o zumbi morreu
                        pM.characterToFollow = null;
                        pM.followCharacter = false;
                        pM.movePos = transform.position;
                    }

                    //se a quantidade de ataques para o pedaco de madeira for 0, então o jogador não tem mais acesso ao pedaco de madeira até coletar outro
                    if (woodenPlankAttacksLeft == 0) {
                        hasWoodenPlank = false;
                        weaponEquipped = 0;
                        UIManager.instance.OnNoWeaponEquipped (); //o ícone do pedaco de madeira não aparece mais na parte Equipped da UI
                    }   
                }
            }

            //faco as conferências para ter certeza que o jogador pode receber o dano
            if (GameManager.instance.playerIsBeingChased && !isInvulnerable && eB.chasingPlayer && eB.enemyHealth > 0 && eB.attackPerformed == false) {
                eB.attackPerformed = true; //comeca um cooldown no script do zumbi ativando um cooldown de 2 segundos até poder atacar novamente

                //50% de chance para o zumbi acertar o ataque
                if (Random.Range (0, 2) == 0) {
                    playerHealth -= 1;
                    isInvulnerable = true;
                    UIManager.instance.UpdateCharacterUI (this.gameObject.GetComponent<PlayerStatus>()); //atualizo a UI para que mostre um coracão a menos na UI
                    StartCoroutine (GameManager.instance.ShowDamageTakenFeedback ()); //mostro na tela um feedback vermelho de que o jogador levou dano

                    //se o zumbi errar o ataque, mostro um indicador no zumbi dizendo MISS para dar o feedback pro jogador
                } else {
                    GameObject indicator = Instantiate (missedAttackIndicator, eB.gameObject.transform.position, Quaternion.identity) as GameObject;
                    Destroy (indicator, 3);
                }
            }

            //se for um sobrevivente sendo escoltado pelo jogador que colidiu com o zumbi
        } else {
            //faco as conferências para ter certeza que o sobrevivente pode receber o dano
            if (pM.characterToFollow != null && !isInvulnerable && eB.chasingPlayer && eB.enemyHealth > 0 && eB.attackPerformed == false) {
                eB.attackPerformed = true;
                playerHealth -= 1;
                isInvulnerable = true;

                //se o sobrevivente morrer, chamo o método FollowingSurvivorDied para fazê-lo parar de seguir o jogador
                if (playerHealth == 0) {
                    FollowingSurvivorDied ();
                }

            }
        }
    }
}
