using UnityEngine;
using System.Collections;

// Descricao		:	Classe base para todos os zumbis. Provem os eventos processando e armazenando as ameaças
public abstract class AIEstadoZumbi : AIEstado {
	// Private
	protected int 						_layerPlayer 	= -1;
	protected int 						_layerCorpo		= -1;
	protected int						_layerVisual	= -1;
	protected AIZombieStateMachine 		_maquinaEstadoZumbi = null;

	// Descricao	:	Calcula as masks e os layers usados para raycasting e teste de layer
	void Awake(){
		//Obtem a mask para a linha de visao testando para o player. +1 é um hack para incluir o layer default 
		_layerPlayer = LayerMask.GetMask ("Player", "AI parte corpo")+1;
		_layerVisual = LayerMask.GetMask ("Player", "AI parte corpo", "Visual Aggravator")+1;
		//Obtem o index do layer do AI Body Part 
		_layerCorpo 	= LayerMask.NameToLayer ("AI parte corpo");
	}

	public override void SetaMaquinaEstado( AIStateMachine stateMachine ){
		if (stateMachine.GetType () == typeof(AIZombieStateMachine)) {
			base.SetaMaquinaEstado (stateMachine);
			_maquinaEstadoZumbi = (AIZombieStateMachine)stateMachine;
		}
	}
		
	// Desc	:	Chamado pelo State Machine pai quando a ameaça entra/fica/sai do sensor trigger do zumbi
	//			Examina a ameaça e armazena ela nas ameaças Visuais ou Audíveis
	public override void EventoTrigger( AIEventoTipo eventType, Collider other ){

		if (_maquinaEstadoZumbi == null)
			return;
	
		//Nao temos interesse em eventos de saida, apenas de entrada e de manter
		if (eventType != AIEventoTipo.Sai) {
			//Qual e o tipo de ameaça visual que esta armazenada
			AITipodoAlvo curType = _maquinaEstadoZumbi.AmeacaVisual.tipo;

			//Se o collider que entrou no sensor é um player
			if (other.CompareTag ("Player")) {
				//Pega a distancia do sensor de origem ate o collider
				float distance = Vector3.Distance (_maquinaEstadoZumbi.posicaoSensor, other.transform.position);

				//Se a ameaça armazenada nao e um player ou esse player esta proximo de um player previamente armazenado
				//como ameaça visual, entao isso pode ser mais importante
				if (curType != AITipodoAlvo.TipoVisual_Player ||
				    (curType == AITipodoAlvo.TipoVisual_Player && distance < _maquinaEstadoZumbi.AmeacaVisual.distance)) {
					//Se o colider esta dentro do cone de visao 
					RaycastHit hitInfo;
					if (ColliderVisivel (other, out hitInfo, _layerPlayer)) {
						//Esta perto e no FOV e temos uma linha de visão, entao armazena como a ameaça mais perigosa
						_maquinaEstadoZumbi.AmeacaVisual.Seta (AITipodoAlvo.TipoVisual_Player, other, other.transform.position, distance);
					}
				}
			} 
			else if (other.CompareTag ("Flash Light") && curType != AITipodoAlvo.TipoVisual_Player) {

				BoxCollider flashLightTrigger = (BoxCollider)other;
				float distanceToThreat = Vector3.Distance (_maquinaEstadoZumbi.posicaoSensor, flashLightTrigger.transform.position);
				float zSize = flashLightTrigger.size.z * flashLightTrigger.transform.lossyScale.z;
				float aggrFactor = distanceToThreat / zSize;
				if (aggrFactor <= _maquinaEstadoZumbi.sentido && aggrFactor <= _maquinaEstadoZumbi.inteligencia) {
					_maquinaEstadoZumbi.AmeacaVisual.Seta ( AITipodoAlvo.TipoVisual_Luz, other, other.transform.position, distanceToThreat);
				}
			}
			else if (other.CompareTag ("AI Sound Emitter")) {
				SphereCollider  soundTrigger = (SphereCollider) other;
				if (soundTrigger==null) return;

				//Pega a posição do Agent sensor
				Vector3 agentSensorPosition 	= _maquinaEstadoZumbi.posicaoSensor;

				Vector3 soundPos;
				float   soundRadius;
				AIState.ConverteCollider( soundTrigger, out soundPos, out soundRadius ); 

				//O quao longe do raio sonoro estamos
				float distanciadaAmeaca = (soundPos - agentSensorPosition).magnitude; 

				//Calcula a distancia, 1.0 quando esta no raio e 0 quando esta no centro
				float fatorDistancia  = (distanciadaAmeaca / soundRadius);

				fatorDistancia+=fatorDistancia*(1.0f-_maquinaEstadoZumbi.ouvindo);

				//Muito longe
				if (fatorDistancia > 1.0f)
					return;
						
				//Se pode ouvir e esta mais perto do que anteriormente armazenado
				if (distanciadaAmeaca<_maquinaEstadoZumbi.AmeacaAudivel.distance){
					//A ameaça por audio mais perigosa
					_maquinaEstadoZumbi.AmeacaAudivel.Seta ( AITipodoAlvo.Audio, other, soundPos, distanciadaAmeaca );
				}
			}else
		    //Registra a ameaça visual mais proxima
			if (other.CompareTag ("AI Food") &&	curType!=AITipodoAlvo.TipoVisual_Player &&	curType!=AITipodoAlvo.TipoVisual_Luz &&
				_maquinaEstadoZumbi.satisfeito<=0.9f && _maquinaEstadoZumbi.AmeacaAudivel.tipo==AITipodoAlvo.Nenhum   ){	
				//O quao longe a ameaça esta de nós
					float distanciaameaca = Vector3.Distance( other.transform.position, _maquinaEstadoZumbi.posicaoSensor);

				//Esta mais perta do que qualquer coisa previamente armazenada
				if (distanciaameaca<_maquinaEstadoZumbi.AmeacaVisual.distance){
	 				//Se sim, checa se esta no nosso FOV e dentro da range
					RaycastHit hitInfo;
					if ( ColliderVisivel( other, out hitInfo, _layerVisual)){
						_maquinaEstadoZumbi.AmeacaVisual.Seta ( AITipodoAlvo.TipoVisual_Comida, other, other.transform.position, distanciaameaca );
					}
				}
			}
		}
	}
		
	// Descricao	:	Testa o collider passado pelo FOV do zumbi e usando o layer mask para o teste de linha de visao
	protected virtual bool	ColliderVisivel( Collider other,  out RaycastHit hitInfo, int layerMask=-1 ){
		hitInfo = new RaycastHit ();
		if (_maquinaEstadoZumbi == null) return false; 

		//Calcula o angulo entre o sensor de origem e a direçao do collider
		Vector3 cabeca 		= _maquinaEstado.posicaoSensor;
		Vector3 direcao	= other.transform.position - cabeca;
		float	angulo 		= Vector3.Angle (direcao, transform.forward);

		//Se o angulo é maior que a metade do FOV, entao esta for do cone de visao, entao retorna falso(Nao visivel)
		if (angulo > _maquinaEstadoZumbi.fov * 0.5f)
			return false;

		//Agora precisamos testar a linha de visao. Faz um raycast do nosso sensor de origem em direçao do collider para a distancia
		//do raio do nosso sensor escalado pelo abilidade de visao do zumbi. Isso irá retornar todos os hits
		RaycastHit[] hits = Physics.RaycastAll( cabeca, direcao.normalized, _maquinaEstadoZumbi.RaioSensor * _maquinaEstadoZumbi.sentido, layerMask);

		//Procura o collider mais proximo que nao é o proprio corpo do AI. Se nao e o alvo, entao o alvo esta obstruido
		float 		distanciaColliderProximo = float.MaxValue;
		Collider	colliderProximo			= null;

		//Examina cada hit
		for (int i = 0; i < hits.Length; i++) {
			RaycastHit hit = hits [i];

			//Se esse hit esta mais proximo do que qualquer outro armazenado anteriormente
			if (hit.distance < distanciaColliderProximo) {
				//Se esse hit esta no layer Body part
				if (hit.transform.gameObject.layer == _layerCorpo) {
					//E assumindo que nao é o proprio corpo
					if (_maquinaEstado != GameSceneManager.instance.GetAIMaquinaDeEstado (hit.rigidbody.GetInstanceID ())) {
						//Armazena o collider, distancia e o hit info
						distanciaColliderProximo = hit.distance;
						colliderProximo = hit.collider;
						hitInfo = hit;
					}
				} else {
					//Se nao e um corpo entao simplesmente armazena isso como o hit mais proximo que encontramos
					distanciaColliderProximo = hit.distance;
					colliderProximo = hit.collider;
					hitInfo = hit;
				}
			}
		}

		//Se o hit mais proximo é o collider que estamos testando contra, isso significa que temos uma linha de visao, entao retorna true
		if (colliderProximo && colliderProximo.gameObject==other.gameObject) return true;

		//Ou, qualquer outra coisa esta proxima de nos do que o collider, entao o campo de visao esta bloqueado
		return false;
	}
}