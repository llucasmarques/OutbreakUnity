using UnityEngine;
using System.Collections;

public class AIEstadoZumbi_Alimentando : AIEstadoZumbi {
	
	// Overrides
	public override AITipoEstado PegaTipoEstado() { return AITipoEstado.Alimentando; }
	public override void 		EntraEstado(){
		Debug.Log ("Entrando no estado Alimentacao");

		base.EntraEstado ( );
		if (_maquinaEstadoZumbi==null) return;

		// layer index
		if (_comendoLayerIndex==-1 )
			_comendoLayerIndex= _maquinaEstadoZumbi.animator.GetLayerIndex("Cinematic");

		// Reseta o Timer do Blood Particles 
		_timer = 0.0f;

		// Configura o Animator do State Machine
		_maquinaEstadoZumbi.alimentando			= true;
		_maquinaEstadoZumbi.procura 		= 0;
		_maquinaEstadoZumbi.velocidade 			= 0;
		_maquinaEstadoZumbi.tipoAtaque		= 0;

		// Update na posição mas nao na rotação
		_maquinaEstado.NavAgentControl(true,false);
	}
		
	// Descricao	:	O motor desse estado
	public override AITipoEstado	NoUpdate( )	{ 
		_timer += Time.deltaTime;

		if (_maquinaEstadoZumbi.satisfeito > 0.9f) {
			_maquinaEstadoZumbi.PegaPosicaoWaypoint (false);
			return AITipoEstado.Alerta;
		}

		//Se é uma ameaça visual entao entra no estado de alerta
		if (_maquinaEstadoZumbi.AmeacaVisual.tipo!=AITipodoAlvo.Nenhum && _maquinaEstadoZumbi.AmeacaVisual.tipo!=AITipodoAlvo.TipoVisual_Comida){
			_maquinaEstadoZumbi.SetaAlvo ( _maquinaEstadoZumbi.AmeacaVisual );
			return AITipoEstado.Alerta;
		}	

		//Se a ameaça por audio entao entra no estado de alerta
		if (_maquinaEstadoZumbi.AmeacaAudivel.tipo==AITipodoAlvo.Audio ){
			_maquinaEstadoZumbi.SetaAlvo ( _maquinaEstadoZumbi.AmeacaAudivel);
			return AITipoEstado.Alerta;
		}

		//Se a animaçao de feeding esta executando
		int currentHash = _maquinaEstadoZumbi.animator.GetCurrentAnimatorStateInfo(_comendoLayerIndex).shortNameHash;
		if (currentHash == _comendoHash || currentHash == _rastejandoAlimentandoHash){
			_maquinaEstadoZumbi.satisfeito = Mathf.Min (_maquinaEstadoZumbi.satisfeito + ((Time.deltaTime * _maquinaEstadoZumbi.replenishRate)/100.0f),1.0f);
			if (GameSceneManager.instance && GameSceneManager.instance.particulaSangue && _qtdParticulaSangue) {
				if (_timer > _tempoBurstSangue) {
					ParticleSystem system = GameSceneManager.instance.particulaSangue;
					system.transform.position = _qtdParticulaSangue.transform.position;
					system.transform.rotation = _qtdParticulaSangue.transform.rotation;

					var settings = system.main;
					settings.simulationSpace = ParticleSystemSimulationSpace.World;
					system.Emit (_qtdSangue);
					_timer = 0.0f;
				}
			}
		}
			
		if (!_maquinaEstadoZumbi.usaRotacao){
			//Mantem o zumbi olhando o player o tempo todo
			Vector3 targetPos = _maquinaEstadoZumbi.posicaoAlvo;
			targetPos.y = _maquinaEstadoZumbi.transform.position.y;
			Quaternion  newRot = Quaternion.LookRotation (  targetPos - _maquinaEstadoZumbi.transform.position);
			_maquinaEstadoZumbi.transform.rotation = Quaternion.Slerp( _maquinaEstadoZumbi.transform.rotation, newRot, Time.deltaTime* _slerp);
		}

		Vector3 headToTarget = _maquinaEstadoZumbi.posicaoAlvo - _maquinaEstadoZumbi.animator.GetBoneTransform (HumanBodyBones.Head).position;
		_maquinaEstadoZumbi.transform.position = Vector3.Lerp (_maquinaEstadoZumbi.transform.position, 
												_maquinaEstadoZumbi.transform.position + headToTarget, Time.deltaTime);
		//Continua no estado de feeding
		return AITipoEstado.Alimentando;
	}

	public override void SaiEstado(){
		if (_maquinaEstadoZumbi!=null)
			_maquinaEstadoZumbi.alimentando = false;
	}

	// Private
	private int 			_rastejandoAlimentandoHash = Animator.StringToHash("comendo rastejando");
	private int 			_comendoHash 	= Animator.StringToHash("Estado de alimentacao");
	private int				_comendoLayerIndex	= -1;
	private float			_timer				= 0.0f;

	// Inspector
	[SerializeField]						float		_slerp					=	5.0f;
	[SerializeField]						Transform	_qtdParticulaSangue		=	null;
	[SerializeField] [Range(0.01f, 1.0f)] 	float 		_tempoBurstSangue	=	0.1f;
	[SerializeField] [Range(1, 100)]		int 		_qtdSangue 	= 	10;


}