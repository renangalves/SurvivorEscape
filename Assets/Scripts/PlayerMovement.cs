using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour {

	UnityEngine.AI.NavMeshAgent nav;

	Animator anim;

	PlayerStatus pS;

	[HideInInspector]
	public float dist;
	public float moveSpeed = 9;
    float distanceToHideCursor = 0.2f;
    float distanceToFollowPlayer = 3.5f;

	[HideInInspector]
	public bool rescuedSurvivor;
	[HideInInspector]
	public bool currentSelectedCharacter;
	[HideInInspector]
	public bool isMoving;
	[HideInInspector]
	public bool hasBeenRescued;
	[HideInInspector]
	public bool followCharacter;
	[HideInInspector]
	public bool characterIsDead;
	[HideInInspector]
	public bool beingFollowed;
	[HideInInspector]
	public bool survivorBeingSaved;

//	[HideInInspector]
	public Vector3 movePos;

//	[HideInInspector]
	public Transform characterToFollow;

	[HideInInspector]
	public GameObject survivorFollowing;



	void Start () 
    {
		pS = GetComponent<PlayerStatus> ();
		nav = GetComponent<UnityEngine.AI.NavMeshAgent> ();
		anim = GetComponent<Animator> ();
		movePos = transform.position;
	}
	

	//este Update irá tratar dos cliques do botão direito do mouse, identificando onde foi clicado, o movimento e controla as animacões 
	void Update () 
    {
	
		//se for um sobrevivente resgatado, for o personagem atualmente selecionado e o personagem não estiver morto
		if (rescuedSurvivor && currentSelectedCharacter && !characterIsDead) {
            MouseInputManager();							
		}
			
		//guardo a distância entre o personagem e a posicão onde ele irá se mover
		dist = Vector3.Distance (transform.position, movePos);

        anim.SetFloat("IsRunning", nav.velocity.magnitude);

		//caso o personagem estiver bem perto do destino dele e for o personagem selecionado
        if (dist < distanceToHideCursor && currentSelectedCharacter) {
			GameManager.instance.HideMoveSpot (); //desapareco com o cursor
		} 
			
		//se o personagem ainda não tiver sido resgatado (ainda não esta na party), então algumas consideracões para controlar as animacões dele são feitas
		if (!rescuedSurvivor) {
			//checo se o personagem não está escondido para não fazer transicão por movimento
			if (gameObject.layer != 10) {
				anim.SetFloat ("Movement", dist);
			} else {
				if(nav.velocity != Vector3.zero)
					anim.SetFloat ("Movement", dist);
				else
					anim.SetFloat ("Movement", 0);
			}
		}
		//senão apenas atualizo o estado de movimento com a distância
		else
			anim.SetFloat ("Movement", dist);

		//aqui é controlado a movimentacão do personagem com o NavMeshAgent
		//se for o personagem escolhido ou um sobrevivente sendo salvo (seguindo o jogador)
		if (rescuedSurvivor || survivorBeingSaved) {
			//se o personagem não estiver seguindo alguém e o NavMesh estiver ativado, move para a posicão
            if (!followCharacter && nav.enabled)
            {
                nav.SetDestination(movePos);
            }
			else 
			{
				//se o sobrevivente tiver algum jogador pra seguir
				if (characterToFollow != null) {
					movePos = characterToFollow.position;
					//se o sobrevivente estiver seguindo o jogador, seu layer irá ser igual a do jogador (para se esconder quando o jogador também estiver escondendo)
					if(!hasBeenRescued)
						this.gameObject.layer = characterToFollow.gameObject.layer;
				} 

				//se o sobrevivente estiver a uma distância maior que 3.5f ele persegue o jogador
                if (dist > distanceToFollowPlayer && nav.enabled) {
					nav.SetDestination (movePos);
				//senão ele para, que assim o sobrevivente para logo atrás do jogador, em vez de andar até a exata mesma posicão 
				} else {
					if (!rescuedSurvivor) {
						nav.velocity = Vector3.zero;
					}
				}
			}
		}	



		//esta consideracão é feita para sempre saber quando o personagem está se movendo, para assim os zumbis poderem enxergar o jogador movendo mesmo se ele estiver por perto de um objeto do cenário
		if (nav.velocity != Vector3.zero) {
			isMoving = true;
		} else {
			isMoving = false;
			//se a layer do sobrevivente for 10 e ainda não tiver sido resgatado, então ativa a animacão de se esconder
			if (this.gameObject.layer == 10 && !hasBeenRescued) {
				anim.SetTrigger ("Hiding");
			//se ele tiver sido resgatado, ativa a animacão de vitória
			}else if (hasBeenRescued)
				anim.SetTrigger ("Win");
		}



	}
	


    void MouseInputManager()
    {
        if (Input.GetMouseButtonDown (1)) {
            followCharacter = false; //reseto essa variável para caso o jogador tivesse seguindo um personagem e depois clicou em outro local, parando de seguir
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);

            //uso raycast para pegar a informacão de onde o mouse está sendo clicado
            if (Physics.Raycast (ray, out hit)) {

                //se for a parede, a posicão para o personagem mover é o local de se esconder no index do personagem (cada personagem tem um ponto atrás dos muros pra se esconder)
                if (hit.collider.tag == "Wall") {
                    movePos = GameManager.instance.wallStartSpots [pS.index].position;
                    characterToFollow = null;

                    //se for a parede final, a posicão para o personagem mover é o local de se esconder no index do personagem (cada personagem tem um ponto atrás dos muros pra se esconder)
                } else if (hit.collider.tag == "OuterWall") {
                    movePos = GameManager.instance.hidingSpots [pS.index].position;
                    characterToFollow = null;

                    //se for clicado em um collider dos objetos do cenário, o personagem se move para a área lateral do objeto
                } else if (hit.collider.tag == "InteriorCollider") {
                    Vector3 aux = hit.collider.transform.position;
                    float offset = 3.5f;
                    aux.x -= offset;
                    movePos = aux;
                    characterToFollow = null;

                    //se o jogador clicar em um zumbi
                } else if (hit.collider.tag == "Enemy") {
                    //se o jogador estiver com a arma equipada
                    if (pS.weaponEquipped == 2) {
                        transform.LookAt (hit.collider.transform); //giro o personagem para a direcão do zumbi
                        pS.ShootPistol (Vector3.Distance (transform.position, hit.collider.transform.position), hit.collider.gameObject);
                        movePos = transform.position; //faco o personagem parar de mover
                        GameManager.instance.HideMoveSpot (); //escondo o cursor 
                        //senão o personagem irá perseguir o zumbi
                    } else {
                        characterToFollow = hit.collider.gameObject.transform;
                        followCharacter = true;
                    }

                    //se o jogador clicar em um sobrevivente escondido no cenário, o personagem irá se mover até ele
                } else if (hit.collider.tag == "Survivor") {
                    Debug.Log("here");
                    characterToFollow = hit.collider.gameObject.transform;
                    followCharacter = true;
                }

                //senão o jogador clicou no chão e o personagem irá se mover até aquele ponto
                else {
                    movePos = hit.point;
                    characterToFollow = null;
                }

                //se o jogador não estiver perseguindo nenhum personagem, o cursor do local onde ele clicou vai aparecer
                if(!followCharacter)
                    GameManager.instance.ShowMoveSpot (movePos);
                //senão desapareco com o cursor
                else
                    GameManager.instance.HideMoveSpot ();
            }
        }
    }



	//se o jogador morrer, o jogador perde o controle do personagem
	public void PlayerDied()
	{
		characterIsDead = true; //evita qualquer movimento
		movePos = transform.position; //faz o personagem parar de movimentar
		anim.SetTrigger ("Dead");
		GetComponent<BoxCollider> ().enabled = false; //o personagem não será mais clicável
		//se for um sobrevivente jogável que morreu, então para a perseguicão dos zumbis, pois vai mudar para um outro sobrevivente escondido
		if (rescuedSurvivor)
			GameManager.instance.ResetChase ();
	}


	//se este sobrevivente for clicado com o botão esquerdo do mouse
	void OnMouseDown()
	{
		//se for um sobrevivente jogável (que está na party), tiver escondido e não estiver morto, então realiza a troca de personagem
		if(rescuedSurvivor && GameManager.instance.currentPlayer.layer == 10 && !characterIsDead)
			GameManager.instance.ChangePlayers (this.gameObject);
	}


	//se o sobrevivente estiver sendo salvo pelo jogador
	public void SurvivorBeingSaved(Transform characterToFollow)
	{
		EnableNavMesh (); //ativo a NavMeshAgent dele
		survivorBeingSaved = true;
		followCharacter = true; //vai seguir o personagem
		isMoving = true;
		this.characterToFollow = characterToFollow; //salvo qual Transform que ele vai seguir
	}


	//este método irá ativar o NavMeshAgent quando este sobrevivente estiver seguindo o jogador ou for selecionado
	public void EnableNavMesh()
	{
		movePos = transform.position;
		if (nav == null)
			nav = GetComponent<UnityEngine.AI.NavMeshAgent> ();
		//this.gameObject.GetComponent<UnityEngine.AI.NavMeshObstacle> ().enabled = false;
		nav.enabled = true;
		nav.speed = moveSpeed;
	}


	//este método desativa a NavMeshAgent quando o jogador selecionar para controlar outro sobrevivente
	public void DisableNavMesh()
	{
		nav.enabled = false;
		//this.gameObject.GetComponent<UnityEngine.AI.NavMeshObstacle> ().enabled = true; //ativo o NavMeshObstacle para ele ser um obstáculo no caminho

	}


	//este método reseta o sobrevivente, usado quando um novo estágio é criado
	public void ResetSurvivor()
	{
		hasBeenRescued = false;
		characterToFollow = null;
		movePos = transform.position;
		anim.SetTrigger ("Hiding");
	}


	void OnTriggerEnter(Collider col)
	{
		//quando este sobrevivente colidir com um sobrevivente no cenário para ser resgatado
		if (col.tag == "Survivor") {
			//se followCharacter é true, então o jogador clicou no sobrevivente (que assim evita de o sobrevivente seguir o jogador por acidente)
			//se beingFollowed é false então o jogador não esta sendo seguido por outro sobrevivente no momento (evitando de poder levar mais de 1 sobrevivente ao mesmo tempo)
			if (followCharacter && rescuedSurvivor && !beingFollowed) {
				col.gameObject.GetComponent<PlayerMovement> ().SurvivorBeingSaved (this.transform);
				beingFollowed = true;
				survivorFollowing = col.gameObject;
			}
		}

		//se colidir com o ponto de resgate, então o jogador está seguro ou sobrevivente foi resgatado
		if (col.tag == "SafePoint") {
			//se for um sobrevivente que ainda nao faz parte da party
			if (!rescuedSurvivor) {
				PlayerMovement pM = characterToFollow.GetComponent<PlayerMovement> ();
				followCharacter = false; //o sobrevivente para de seguir o jogador
				hasBeenRescued = true;
				pM.beingFollowed = false; //o jogador não estará mais sendo seguido
				pM.survivorFollowing = null;
				pM.characterToFollow = null;
				this.gameObject.tag = "Untagged";
				movePos = GameManager.instance.hidingSpots [pS.index].position; //pego a posicão atras do muro pro sobrevivente ir
				GameManager.instance.savedSurvivors.Add (this.gameObject); //este sobrevivente é adicionado pra lista savedSurvivors pois poderá fazer parte da party no próximo estágio
				GameManager.instance.numberOfRescuedSurvivors++; //este número aumenta para decidir se todos os sobrevivente do estágio foram resgatados
				GameManager.instance.totalOfSavedSurvivors++; //este número aumenta para mostrar o resultado na tela quando o estágio for finalizado
			//se não for um sobrevivente e sim o personagem que o jogador está controlando
			} else
				GameManager.instance.numberOfSafePlayers++; //este número aumenta para saber se todos os sobreviventes controláveis (que estão na party) estão seguros no ponto de resgate
		}
	}


	void OnTriggerExit(Collider col)
	{
		//se o jogador sair fora do ponto de resgate
		if (col.tag == "SafePoint") {
			if (rescuedSurvivor) {
				GameManager.instance.numberOfSafePlayers--; //este número diminui para saber que ainda tem personagem que não esta no ponto de resgate
			}
		}
	}
}
