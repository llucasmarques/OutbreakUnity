using UnityEngine;
using System.Collections;

public class AIEstadoZumbi_Patrulha : AIEstadoZumbi{

	// Private
	private float 		_timer 				= 0.0f;
	private float		_refazCaminhoTimer		= 0.0f;
	private float 		_olhandoPeso = 0.0f;

	// Overrides
	public override AITipoEstado PegaTipoEstado() { return AITipoEstado.Perseguicao; }




	public override void 		UpdateAnimator()	{
		if (_maquinaEstadoZumbi == null)
			return;

		if (Vector3.Angle (_maquinaEstadoZumbi.transform.forward, _maquinaEstadoZumbi.posicaoAlvo - _maquinaEstadoZumbi.transform.position) < _olhaAnguloLimite){
			_maquinaEstadoZumbi.animator.SetLookAtPosition (_maquinaEstadoZumbi.posicaoAlvo + Vector3.up );
			_olhandoPeso = Mathf.Lerp (_olhandoPeso, _olhaoPeso, Time.deltaTime);
			_maquinaEstadoZumbi.animator.SetLookAtWeight (_olhandoPeso);
		} else {
			_olhandoPeso = Mathf.Lerp (_olhandoPeso, 0.0f, Time.deltaTime);
			_maquinaEstadoZumbi.animator.SetLookAtWeight (_olhandoPeso);	
		}
	}

	public override void EntraEstado(){
		Debug.Log ("Entrando estado de perseguicao");

		base.EntraEstado ();
		if (_maquinaEstadoZumbi == null)
			return;

		// Configura a maquina de estado
		_maquinaEstadoZumbi.NavAgentControl (true, false);
		_maquinaEstadoZumbi.procura 	= 0;
		_maquinaEstadoZumbi.alimentando 	= false;
		_maquinaEstadoZumbi.tipoAtaque 	= 0;

		_timer 			= 0.0f;
		_refazCaminhoTimer	= 0.0f;
	
		// Seta o caminho
		_maquinaEstadoZumbi.navAgent.SetDestination(_maquinaEstadoZumbi.posicaoAlvo);
		_maquinaEstadoZumbi.navAgent.Resume();

		_olhandoPeso = 0.0f;
	}
		
	public override AITipoEstado	NoUpdate( ){ 
		_timer += Time.deltaTime;
		_refazCaminhoTimer += Time.deltaTime;

		if (_timer > _duracaoMaxima)
			return AITipoEstado.Patrulha;

		//Se estamos perseguindo o player e entramos na area de melee, entao ataca
		if(_maquinaEstado.tipoAlvo ==AITipodoAlvo.TipoVisual_Player && _maquinaEstadoZumbi.estaDentroDaRange){
			return AITipoEstado.Ataque;
		}

		//Se chegamos no lugar 
		if ( _maquinaEstadoZumbi.alvoProximo ){
			switch (_maquinaEstado.tipoAlvo){

			//Se  encontramos a fonte
			case AITipodoAlvo.Audio:
			case AITipodoAlvo.TipoVisual_Luz:
				_maquinaEstado.LimpaAlvo();	// Limpa a ameaça
				return AITipoEstado.Alerta;		// Entra no estado de alerta e procura por alvos

			case AITipodoAlvo.TipoVisual_Comida:
				return AITipoEstado.Alimentando;
			}
		}

		//Se por alguma razao o navAgent perdeu o caminho, entao volta para o estado de alerta, assim tentaremos re-encontra
		//o alvo ou desistir e apenas ir para o estado de Patrol
		if (_maquinaEstadoZumbi.navAgent.isPathStale || 
			(!_maquinaEstadoZumbi.navAgent.hasPath && !_maquinaEstadoZumbi.navAgent.pathPending) ||
			_maquinaEstadoZumbi.navAgent.pathStatus!=UnityEngine.AI.NavMeshPathStatus.PathComplete) {
			return AITipoEstado.Alerta;
		}

		if (_maquinaEstadoZumbi.navAgent.pathPending)
			_maquinaEstadoZumbi.velocidade = 0;
		else {
			_maquinaEstadoZumbi.velocidade = _velocidade;

			//Se estamos proximo do alvo que e um player e ainda tempos ele no campo de visao, entao continua olhando para ele
			if (!_maquinaEstadoZumbi.usaRotacao && _maquinaEstadoZumbi.tipoAlvo == AITipodoAlvo.TipoVisual_Player && _maquinaEstadoZumbi.AmeacaVisual.tipo == AITipodoAlvo.TipoVisual_Player && _maquinaEstadoZumbi.alvoProximo) {
				Vector3 targetPos = _maquinaEstadoZumbi.posicaoAlvo;
				targetPos.y = _maquinaEstadoZumbi.transform.position.y;
				Quaternion newRot = Quaternion.LookRotation (targetPos - _maquinaEstadoZumbi.transform.position);
				_maquinaEstadoZumbi.transform.rotation = newRot;
			} else
			//Update na rotaçao ate a rotaçao desejata, mas apenas se estamos perseguindo o player e ele esta proximo
			if (!_maquinaEstado.usaRotacao && !_maquinaEstadoZumbi.alvoProximo) {
				//Gera um novo Quaternion representando a rotaçao que temos que ter
				Quaternion newRot = Quaternion.LookRotation (_maquinaEstadoZumbi.navAgent.desiredVelocity);
				
				// Aos poucos rotaciona para a nova rotaçao ao longo do tempo
				_maquinaEstadoZumbi.transform.rotation = Quaternion.Slerp (_maquinaEstadoZumbi.transform.rotation, newRot, Time.deltaTime * _slerp);
			} else if (_maquinaEstadoZumbi.alvoProximo) {
				return AITipoEstado.Alerta;
			}
		}
	
		//Temos uma ameaça visual que e um player
		if (_maquinaEstadoZumbi.AmeacaVisual.tipo==AITipodoAlvo.TipoVisual_Player){
			//A posiçao é diferente - talvez a mesma ameaça porem mudou de posiçao
			if (_maquinaEstadoZumbi.posicaoAlvo!=_maquinaEstadoZumbi.AmeacaVisual.position){
				// Refaz o caminho mais frequentemente assim que chegamos mais proximo do alvo
				if (Mathf.Clamp (_maquinaEstadoZumbi.AmeacaVisual.distance*_refazCaminhoDistanciaMulti, _refazCaminhoVisualMinimaDuracao, _repathVisualMaxDuration)<_refazCaminhoTimer){
					// Repath o agent
					_maquinaEstadoZumbi.navAgent.SetDestination( _maquinaEstadoZumbi.AmeacaVisual.position );
					_refazCaminhoTimer = 0.0f;
				}
			}
			//Ter certeza que esse é o alvo atual
			_maquinaEstadoZumbi.SetaAlvo ( _maquinaEstadoZumbi.AmeacaVisual );

			return AITipoEstado.Perseguicao;
		}

		if (_maquinaEstadoZumbi.tipoAlvo == AITipodoAlvo.TipoVisual_Player)
			return AITipoEstado.Perseguicao;

		//Se é luz
		if (_maquinaEstadoZumbi.AmeacaVisual.tipo == AITipodoAlvo.TipoVisual_Luz) {
			//Entra no estado de alerta para tentar encontrar a origem da luz
			if (_maquinaEstadoZumbi.tipoAlvo == AITipodoAlvo.Audio || _maquinaEstadoZumbi.tipoAlvo == AITipodoAlvo.TipoVisual_Comida) {
				_maquinaEstadoZumbi.SetaAlvo (_maquinaEstadoZumbi.AmeacaVisual);
				return AITipoEstado.Alerta;
			}
			else if (_maquinaEstadoZumbi.tipoAlvo == AITipodoAlvo.TipoVisual_Luz) {
				//Obtem o ID do collider do alvo
				int atualID = _maquinaEstadoZumbi.IDColliderAlvo;

				//Se é a mesma luz
				if (atualID == _maquinaEstadoZumbi.AmeacaVisual.collider.GetInstanceID ()) {
					//A posiçao é diferente, talvez seja o mesmo alvo porem se movimentou
					if (_maquinaEstadoZumbi.posicaoAlvo!=_maquinaEstadoZumbi.AmeacaVisual.position){
						//Refaz o caminho mais frequentemente assim que chega mais proximo do alvo
						if (Mathf.Clamp (_maquinaEstadoZumbi.AmeacaVisual.distance*_refazCaminhoDistanciaMulti, _refazCaminhoVisualMinimaDuracao, _repathVisualMaxDuration)<_refazCaminhoTimer){
							// Repath o agent
							_maquinaEstadoZumbi.navAgent.SetDestination( _maquinaEstadoZumbi.AmeacaVisual.position );
							_refazCaminhoTimer = 0.0f;
						}
					}

					_maquinaEstadoZumbi.SetaAlvo (_maquinaEstadoZumbi.AmeacaVisual);	
					return AITipoEstado.Perseguicao;
				}else {
					_maquinaEstadoZumbi.SetaAlvo (_maquinaEstadoZumbi.AmeacaVisual);
					return AITipoEstado.Alerta; 
				}
			}
			//Se é Audio
		} else if (_maquinaEstadoZumbi.AmeacaAudivel.tipo == AITipodoAlvo.Audio ) {
				
			if (_maquinaEstadoZumbi.tipoAlvo == AITipodoAlvo.TipoVisual_Comida) {
				_maquinaEstadoZumbi.SetaAlvo (_maquinaEstadoZumbi.AmeacaAudivel);
				return AITipoEstado.Alerta;
			}
			else if (_maquinaEstadoZumbi.tipoAlvo == AITipodoAlvo.Audio) {
				//Obtem o ID do collider do alvo
				int atualID = _maquinaEstadoZumbi.IDColliderAlvo;
				
				// Se e o mesmo audio
				if (atualID == _maquinaEstadoZumbi.AmeacaAudivel.collider.GetInstanceID ()) {
					//A posiçao é diferente, talvez seja o mesmo alvo porem se movimentou
					if (_maquinaEstadoZumbi.posicaoAlvo != _maquinaEstadoZumbi.AmeacaAudivel.position) {
						//Refaz o caminho mais frequentemente assim que chega mais proximo do alvo
						if (Mathf.Clamp (_maquinaEstadoZumbi.AmeacaAudivel.distance * _refazCaminhoDistanciaMulti, _refazAudioMinimaDuracao, _refazAudioMaxDuracao) < _refazCaminhoTimer) {
							// Repath o agent
							_maquinaEstadoZumbi.navAgent.SetDestination (_maquinaEstadoZumbi.AmeacaAudivel.position);
							_refazCaminhoTimer = 0.0f;
						}
					}

					_maquinaEstadoZumbi.SetaAlvo (_maquinaEstadoZumbi.AmeacaAudivel);	
					return AITipoEstado.Perseguicao;	
				}else {
					_maquinaEstadoZumbi.SetaAlvo (_maquinaEstadoZumbi.AmeacaAudivel);
					return AITipoEstado.Alerta; 
				}
			}
		}	

		// Default
		return AITipoEstado.Perseguicao;
	}

	//Inspector
	[SerializeField]	[Range(0,10)]		private float _velocidade						=	1.0f;
	[SerializeField]	[Range(0.0f,1.0f)]	 float	_olhaoPeso			= 	0.7f;
	[SerializeField]	[Range(0.0f, 90.0f)] float  _olhaAnguloLimite	=	15.0f;
	[SerializeField]						private float _slerp					=	5.0f;



	[SerializeField]						private float _refazCaminhoDistanciaMulti		=	0.035f;
	[SerializeField]						private float _refazCaminhoVisualMinimaDuracao		=	0.05f;
	[SerializeField]						private float _refazCaminhoVisualMaxDuracao		=	5.0f;
	[SerializeField]						private float _refazAudioMinimaDuracao		=	0.25f;
	[SerializeField]						private float _refazAudioMaxDuracao		=	5.0f;
	[SerializeField]						private float _duracaoMaxima				=	40.0f;
}