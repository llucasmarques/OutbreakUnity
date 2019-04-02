using UnityEngine;
using System.Collections;

public class AIEstadoZumbi_Ocioso : AIEstadoZumbi {
	
	// Descricao	:	Retorna o tipo desse estado
	public override AITipoEstado PegaTipoEstado(){
		return AITipoEstado.Ocioso;
	}
		
	// Descricao	:	Chamado pela maquina de estado quando ocorre a transiçao para esse estado. Inicializa o timer e
	//				    configura a maquina de estado
	public override void EntraEstado(){
		Debug.Log ("Entrando no estado OCIOSO");
		base.EntraEstado ();
		if (_maquinaEstadoZumbi == null)
			return;

		// Seta o tempo do IDLE
		_tempoOcioso = Random.Range (_tempoOciosoExtensao.x, _tempoOciosoExtensao.y);
		_timer 	  = 0.0f;

		// Configura maquina de estado
		_maquinaEstadoZumbi.NavAgentControl (true, false);
		_maquinaEstadoZumbi.velocidade 	= 0;
		_maquinaEstadoZumbi.procura = 0;
		_maquinaEstadoZumbi.alimentando = false;
		_maquinaEstadoZumbi.tipoAtaque = 0;
		_maquinaEstadoZumbi.LimpaAlvo ();
	}
		
	// Descricao	:	Chamado pela maquina de estado a cada frame
	public override AITipoEstado NoUpdate(){
		if (_maquinaEstadoZumbi == null)
			return AITipoEstado.Ocioso;

		// Se o player esta visivel
		if (_maquinaEstadoZumbi.AmeacaVisual.tipo == AITipodoAlvo.TipoVisual_Player) {
			_maquinaEstadoZumbi.SetaAlvo (_maquinaEstadoZumbi.AmeacaVisual);
			return AITipoEstado.Perseguicao;
		}

		// Se ameaça é uma luz
		if (_maquinaEstadoZumbi.AmeacaVisual.tipo == AITipodoAlvo.TipoVisual_Luz) {
			_maquinaEstadoZumbi.SetaAlvo (_maquinaEstadoZumbi.AmeacaVisual);
			return AITipoEstado.Alerta;
		}

		// Se a ameaça é um emissor de som
		if (_maquinaEstadoZumbi.AmeacaAudivel.tipo == AITipodoAlvo.Audio) {
			_maquinaEstadoZumbi.SetaAlvo (_maquinaEstadoZumbi.AmeacaAudivel);
			return AITipoEstado.Alerta;
		}

		// Se a ameaça é comida
		if (_maquinaEstadoZumbi.AmeacaVisual.tipo == AITipodoAlvo.TipoVisual_Comida) {
			_maquinaEstadoZumbi.SetaAlvo (_maquinaEstadoZumbi.AmeacaVisual);
			return AITipoEstado.Perseguicao;
		}

		// Update no tempo do Idle
		_timer += Time.deltaTime;

		// Entra no estado Patrol se o tempo do Idle excedeu
		if (_timer > _tempoOcioso) {
			_maquinaEstadoZumbi.navAgent.SetDestination(_maquinaEstadoZumbi.PegaPosicaoWaypoint (false));
			_maquinaEstadoZumbi.navAgent.Resume ();
			return AITipoEstado.Alerta;
		}

		//Mantem o estado
		return AITipoEstado.Ocioso;
	}
	// Inspector
	[SerializeField] Vector2 _tempoOciosoExtensao = new Vector2(10.0f, 60.0f);

	// Private
	float _tempoOcioso	=	0.0f;
	float _timer	=	0.0f;

}