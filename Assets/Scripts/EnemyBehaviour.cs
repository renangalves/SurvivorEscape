using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EnemyBehaviour : MonoBehaviour {

	UnityEngine.AI.NavMeshAgent nav;

	Vector3 movePos;
	Vector3 rayDirection;

	RaycastHit hit;

	[HideInInspector]
	public int enemyHealth = 2;

	float zombieViewDistance = 30;
	float findNewPosTimer;
	float findNewPosTime = 8;
	float dist;
	float attackTime = 2f;
	float attackTimer;
	float idleMoveSpeed = 5;
	float idleAcceleration = 8;
	float chasingMoveSpeed = 13;
	float chasingAcceleration = 50;
    float zombieSightAngle = 45;

	[HideInInspector]
	public bool chasingPlayer;
	[HideInInspector]
	public bool attackPerformed;
	bool showIndicatorOnce;
	bool touchingPlayer;
	bool playAttackOnce;
	bool playDeathAnimationOnce;
	bool stopOnce;

	public GameObject playerSpottedIndicator;
	public GameObject damageTakenIndicator;
	public GameObject missedAttackIndicator;
	[HideInInspector]
	public GameObject player;

	Animator anim;


	void Start () {
		nav = GetComponent<UnityEngine.AI.NavMeshAgent> ();
		anim = GetComponent<Animator> ();
		player = GameManager.instance.currentPlayer; //o zumbi terá como foco o sobrevivente sendo controlado atual
		GameManager.instance.enemies.Add (GetComponent<EnemyBehaviour> ()); //mantenho todos inimigos criados no GameManager para caso necessitar de fazer alguma mudanca, como qual sobrevivente focar
		FindNewSpot (); //procuro de início um local para onde o zumbi se movimentará
	}
	

	void Update () {

		//se o inimigo não estiver morto
		if (enemyHealth > 0) {

			//se o inimigo não estiver perseguindo o jogador, ele ficará se movimentando aleatoriamente pelo cenário
			if (!chasingPlayer || player.layer == 10) {
				nav.SetDestination (movePos);
				nav.speed = idleMoveSpeed;
				nav.acceleration = idleAcceleration;
				chasingPlayer = false;
				touchingPlayer = false;
				showIndicatorOnce = false;
				findNewPosTimer += Time.deltaTime;

				//depois de um cooldown, uma nova posicão será adquirida para o zumbi se movimentar
				if (findNewPosTimer >= findNewPosTime) {
					FindNewSpot ();
					findNewPosTimer = 0;
					findNewPosTime = Random.Range (10, 15);
				}

			//se o zumbi estiver perseguindo o jogador
			} else {

				//se o zumbi não estiver encostando no jogador
				if (!touchingPlayer) {

					//aqui eu confiro se o sobrevivente escolhido está sendo seguido por um outro sobrevivente, se não estiver então o zumbi persegue o jogador mesmo
					if (player.GetComponent<PlayerMovement> ().beingFollowed == false)
						nav.SetDestination (player.transform.position);
					
					//senão o zumbi vai perseguir o sobrevivente que está seguindo o jogador
					else
						nav.SetDestination (player.GetComponent<PlayerMovement> ().survivorFollowing.transform.position);

				//se o zumbi estiver encostando no jogador
				} else {
					nav.velocity = Vector3.zero; //paro a movimentacão dele

					//dou o trigger na animacão de ataque
					if (!playAttackOnce) {
						anim.SetTrigger ("Attack");
						playAttackOnce = true;
					}

					//reseto algumas variáveis para  um próximo ataque depois de um cooldown
					attackTimer += Time.deltaTime;
					if (attackTimer >= attackTime) {
						attackTimer = 0;
						touchingPlayer = false;
						playAttackOnce = false;
						attackPerformed = false;
					}
				}

				//a velocidade e aceleracão do zumbi aumenta
				nav.speed = chasingMoveSpeed;
				nav.acceleration = chasingAcceleration;

				//mostro o indicador que demonstra um ! para dar o feedback de que o zumbi está perseguindo o jogador
				if (!showIndicatorOnce) {
					GameObject indicator = Instantiate (playerSpottedIndicator, this.gameObject.transform.position, Quaternion.identity) as GameObject;
					Destroy (indicator, 3);
					showIndicatorOnce = true;
				}
			}

            //gerencia a animacão de andar ou idle
            anim.SetFloat("IsWalking", nav.velocity.magnitude);
			
			//guardo a direcão que sairá o ray do zumbi
			rayDirection = player.transform.position - transform.position;

			//se o ângulo entre o jogador e a frente do zumbi for menor que 45 graus, então o zumbi estará vendo o jogador 
            if ((Vector3.Angle (rayDirection, transform.forward)) < zombieSightAngle) {

				//dou um raycast até o jogador na distância máxima zombieViewDistance
				if (Physics.Raycast (transform.position, rayDirection, out hit, zombieViewDistance)) {

					//se o raycast pegar o jogador
					if (hit.transform.tag == "Player") {

						//se o jogador não estiver escondido e ele já não estiver perseguindo o jogador
						if (player.layer != 10 && !chasingPlayer) {
							chasingPlayer = true;
							GameManager.instance.GettingChased (true);
							GameManager.instance.spottedCount++; //esta contagem aumenta para saber quantas vezes o jogador está sendo descoberto pelos zumbis
						}
					}
				}
			}

		//se o zumbi tiver morrido
		} else {
			nav.velocity = Vector3.zero; //paro a movimentacão dele

			if (!playDeathAnimationOnce) {
				anim.SetInteger("Death", Random.Range(0, 3)); //dou play aleatoriamente uma das 3 animacões de morte do zumbi
				playDeathAnimationOnce = true;
				GameManager.instance.GettingChased (false);
				GameManager.instance.enemies.Remove (this.gameObject.GetComponent<EnemyBehaviour> ());
				GameManager.instance.totalOfKilledZombies++;
			}
			Destroy (this.gameObject, 5f);
		}
			
	}


	//este método procura um novo local dentro do cenário para o zumbi se movintar
	void FindNewSpot()
	{
		Collider[] neighbours;
		float circleRadius = 25;
		float sphereRadius = 2;

		do {
			Vector3 aux = Random.insideUnitCircle * circleRadius;
			movePos = new Vector3 (aux.x, 0, aux.y);
			neighbours = Physics.OverlapSphere (movePos, sphereRadius, 9);
		} while(neighbours.Length > 0);
	}


	//este método mostra o indicador de dano no inimigo
	public void ShowDamageIndicator(int damage)
	{
		GameObject indicator = Instantiate (damageTakenIndicator, this.gameObject.transform.position, Quaternion.identity) as GameObject;
		indicator.transform.GetComponentInChildren<Text> ().text = "- " + damage;
		Destroy (indicator, 3);
	}



	void OnTriggerStay(Collider col)
	{
		//se estiver colidindo com o jogador, touchingPlayer será true e o zumbi comecará a perseguir
		if (col.tag == "Player") {
			if(chasingPlayer)
				touchingPlayer = true;
		}

		//se o zumbi colidir com um sobrevivente
		if (col.tag == "Survivor") {

			//primeiro checo se o sobrevivente está sendo escoltado pelo jogador, se sim então touchingPlayer é true o zumbi atacará
			if (col.GetComponent<PlayerMovement> ().survivorBeingSaved == true)
				touchingPlayer = true;
		}
	}
		
}
