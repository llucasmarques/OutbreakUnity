using UnityEngine;
using System.Collections;

public class AIEstadoZumbi_Alerta : AIEstadoZumbi {
	
	// Descricao	:	Retorna o tipo desse estado
	public override AITipoEstado PegaTipoEstado(){
		return AITipoEstado.Alerta;
	}

	// Descricao	:	Chamado pela maquina de estado quando transitada para esse estado. Isso inicializa o timer e configura
	//					a maquina de estado
	public override void EntraEstado()	{
		Debug.Log ("Entrando no estado Alerta");
		base.EntraEstado ();
		if (_maquinaEstadoZumbi == null)
			return;
		
		// Configura a State Machine
		_maquinaEstadoZumbi.NavAgentControl (true, false);
		_maquinaEstadoZumbi.velocidade 	= 0;
		_maquinaEstadoZumbi.procura = 0;
		_maquinaEstadoZumbi.alimentando = false;
		_maquinaEstadoZumbi.tipoAtaque = 0;

		_timer = _maximaDuracao;
		_mudaDirecaoTimer = 0.0f;

		_chanceGrito = _maquinaEstadoZumbi.ScreamChance - Random.value;
	}
		
	public override AITipoEstado NoUpdate(){
		// Reduz o timer
		_timer-=Time.deltaTime;
		_mudaDirecaoTimer += Time.deltaTime;

		// Transita para o estado de Patrol se possivel
		if (_timer <= 0.0f) {
			_maquinaEstadoZumbi.navAgent.SetDestination(_maquinaEstadoZumbi.PegaPosicaoWaypoint (false));
			_maquinaEstadoZumbi.navAgent.Resume ();
			_timer = _maximaDuracao;
		}

		//Ameaça visual
		if (_maquinaEstadoZumbi.AmeacaVisual.tipo==AITipodoAlvo.TipoVisual_Player){
			_maquinaEstadoZumbi.SetaAlvo ( _maquinaEstadoZumbi.AmeacaVisual );
			if (_chanceGrito > 0.0f && Time.time > _proximoGrito) {
				if (_maquinaEstadoZumbi.Scream ()) {
					_chanceGrito = float.MinValue;
					_proximoGrito = Time.time + _frequenciaGrito;
					return AITipoEstado.Alerta;
				}
			}
				
			return AITipoEstado.Perseguicao;
		}	

		//Ameaça por audio
		if (_maquinaEstadoZumbi.AmeacaAudivel.tipo==AITipodoAlvo.Audio ){
			_maquinaEstadoZumbi.SetaAlvo ( _maquinaEstadoZumbi.AmeacaAudivel );
			_timer = _maximaDuracao;
		}

		//Ameaça por luz
		if (_maquinaEstadoZumbi.AmeacaVisual.tipo==AITipodoAlvo.TipoVisual_Luz){
			_maquinaEstadoZumbi.SetaAlvo ( _maquinaEstadoZumbi.AmeacaVisual);
			_timer = _maximaDuracao;
		}	

		//Alimento
		if (_maquinaEstadoZumbi.AmeacaAudivel.tipo==AITipodoAlvo.Nenhum && 
			_maquinaEstadoZumbi.AmeacaVisual.tipo==AITipodoAlvo.TipoVisual_Comida &&
			_maquinaEstadoZumbi.tipoAlvo==AITipodoAlvo.Nenhum){
			_maquinaEstadoZumbi.SetaAlvo ( _maquinaEstado.AmeacaVisual );
			return AITipoEstado.Perseguicao;
		}	

		float angulo;

		if ((_maquinaEstadoZumbi.tipoAlvo == AITipodoAlvo.Audio || _maquinaEstadoZumbi.tipoAlvo == AITipodoAlvo.TipoVisual_Luz) && !_maquinaEstadoZumbi.alvoProximo){
			angulo = AIState.FindSignedAngle (_maquinaEstadoZumbi.transform.forward, 
				_maquinaEstadoZumbi.posicaoAlvo - _maquinaEstadoZumbi.transform.position);
			
			if (_maquinaEstadoZumbi.tipoAlvo == AITipodoAlvo.Audio && Mathf.Abs (angulo) < _ameacaLimiteAngulo) {
				return AITipoEstado.Perseguicao;
			}

			if (_mudaDirecaoTimer > _mudaDirecao) {
				if (Random.value < _maquinaEstadoZumbi.inteligencia){
					_maquinaEstadoZumbi.procura = (int)Mathf.Sign (angulo);
				} else {
					_maquinaEstadoZumbi.procura = (int)Mathf.Sign (Random.Range (-1.0f, 1.0f));
				}

				_mudaDirecaoTimer = 0.0f;
			}
		} 
		else if (_maquinaEstadoZumbi.tipoAlvo == AITipodoAlvo.Waypoint && !_maquinaEstadoZumbi.navAgent.pathPending){
			angulo = AIState.FindSignedAngle (_maquinaEstadoZumbi.transform.forward, 
				_maquinaEstadoZumbi.navAgent.steeringTarget - _maquinaEstadoZumbi.transform.position);

			if (Mathf.Abs (angulo) < _waypointLimiteAngulo)
				return AITipoEstado.Patrulha;
			if (_mudaDirecaoTimer > _mudaDirecao) {
				_maquinaEstadoZumbi.procura = (int)Mathf.Sign (angulo);
				_mudaDirecaoTimer = 0.0f;
			}
		}else {
			if (_mudaDirecaoTimer > _mudaDirecao) {
				_maquinaEstadoZumbi.procura = (int)Mathf.Sign (Random.Range (-1.0f, 1.0f));
				_mudaDirecaoTimer = 0.0f;
			}
		}

		if (!_maquinaEstadoZumbi.usaRotacao)
			_maquinaEstadoZumbi.transform.Rotate (new Vector3 (0.0f, _slerp * _maquinaEstadoZumbi.procura * Time.deltaTime, 0.0f));

		//Continua no estado de alerta
		return AITipoEstado.Alerta;
	}
	// Private	
	float	_timer	=	0.0f;
	float   _proximoGrito = 0.0f;
	float   _frequenciaGrito = 120.0f;
	float   _mudaDirecaoTimer = 0.0f;
	float   _chanceGrito = 0.0f;

	// Inspector
	[SerializeField] [Range(1, 60)] float	_maximaDuracao = 10.0f; 
	[SerializeField] float	_mudaDirecao	=	1.5f;
	[SerializeField] float  _slerp				=   45.0f;
	[SerializeField] float	_waypointLimiteAngulo	=	90.0f;
	[SerializeField] float	_ameacaLimiteAngulo	=	10.0f;

}