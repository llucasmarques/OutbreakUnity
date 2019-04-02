using UnityEngine;
using System.Collections;

public class AIEstadoZumbi_Ataque :  AIEstadoZumbi {
	
	// Descricao	:	Override IK 
	public override void 		UpdateAnimator()	{
		if (_maquinaEstadoZumbi == null)
			return;

		if (Vector3.Angle (_maquinaEstadoZumbi.transform.forward, _maquinaEstadoZumbi.posicaoAlvo - _maquinaEstadoZumbi.transform.position) < _olhaLimiteAngulo){
			_maquinaEstadoZumbi.animator.SetLookAtPosition (_maquinaEstadoZumbi.posicaoAlvo + Vector3.up );
			_olhandoPeso = Mathf.Lerp (_olhandoPeso, _olhaPeso, Time.deltaTime);
			_maquinaEstadoZumbi.animator.SetLookAtWeight (_olhandoPeso);
		} else {
			_olhandoPeso = Mathf.Lerp (_olhandoPeso, 0.0f, Time.deltaTime);
			_maquinaEstadoZumbi.animator.SetLookAtWeight (_olhandoPeso);	
		}
	}
	//Overrides
	public override AITipoEstado PegaTipoEstado() { return AITipoEstado.Ataque; }

	public override void 		EntraEstado(){
		Debug.Log ("Entrando no estado Ataque");

		base.EntraEstado ();
		if (_maquinaEstadoZumbi == null)
			return;

		// Configura a Maquina de estado
		_maquinaEstadoZumbi.NavAgentControl (true, false);
		_maquinaEstadoZumbi.procura 	= 0;
		_maquinaEstadoZumbi.alimentando 	= false;
		_maquinaEstadoZumbi.tipoAtaque 	= Random.Range (1, 100);;
		_maquinaEstadoZumbi.velocidade 		= _speed;
		_olhandoPeso = 0.0f;
	}

	public override void	SaiEstado(){
		_maquinaEstadoZumbi.tipoAtaque = 0;
	}
		
	public override AITipoEstado	NoUpdate( )	{ 
		Vector3 posicaoAlvo;
		Quaternion newRot;

		if (Vector3.Distance (_maquinaEstadoZumbi.transform.position, _maquinaEstadoZumbi.posicaoAlvo) < _distanciaParada)
			_maquinaEstadoZumbi.velocidade = 0;
		else
			_maquinaEstadoZumbi.velocidade = _speed;
			
		//Temos uma ameaça visual que é um player
		if (_maquinaEstadoZumbi.AmeacaVisual.tipo==AITipodoAlvo.TipoVisual_Player){
			//Seta o novo alvo
			_maquinaEstadoZumbi.SetaAlvo ( _maquinaEstado.AmeacaVisual );

			//Se nao estamos na melee range, entao entra no pursuit 
			if (!_maquinaEstadoZumbi.estaDentroDaRange)	return AITipoEstado.Perseguicao;

			if (!_maquinaEstadoZumbi.usaRotacao){
				//Mantem o zumbi olhando pro player o tempo todo
				posicaoAlvo = _maquinaEstadoZumbi.posicaoAlvo;
				posicaoAlvo.y = _maquinaEstadoZumbi.transform.position.y;
				newRot = Quaternion.LookRotation (  posicaoAlvo - _maquinaEstadoZumbi.transform.position);
				_maquinaEstadoZumbi.transform.rotation = Quaternion.Slerp( _maquinaEstadoZumbi.transform.rotation, newRot, Time.deltaTime* _slerP);
			}
				
			_maquinaEstadoZumbi.tipoAtaque = Random.Range (1,100);

			return AITipoEstado.Ataque;
		}

		//O player saiu do FOV ou escondeu, entao olhe para a direção dele e volta para o estado de alerta, para tentar
		//achar novamente o alvo
		if (!_maquinaEstadoZumbi.usaRotacao){
			posicaoAlvo = _maquinaEstadoZumbi.posicaoAlvo;
			posicaoAlvo.y = _maquinaEstadoZumbi.transform.position.y;
			newRot = Quaternion.LookRotation (  posicaoAlvo - _maquinaEstadoZumbi.transform.position);
			_maquinaEstadoZumbi.transform.rotation = newRot;
		}

		return AITipoEstado.Alerta;
	}
	// Inspector
	[SerializeField]	[Range(0,10)]		 float	_velocidade				=	0.0f;
	[SerializeField]						 float	_distanciaParada		=	1.0f;
	[SerializeField]	[Range(0.0f,1.0f)]	 float	_olhaPeso			= 	0.7f;
	[SerializeField]	[Range(0.0f, 90.0f)] float  _olhaLimiteAngulo	=	15.0f;
	[SerializeField]						 float	_slerP				=	5.0f;

	// Private
	private float _olhandoPeso = 0.0f;

}