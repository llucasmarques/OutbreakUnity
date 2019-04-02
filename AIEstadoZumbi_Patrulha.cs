using UnityEngine;
using System.Collections;

public class AIEstadoZumbi_Patrulha : AIEstadoZumbi{

	public override AITipoEstado PegaTipoEstado(){
		return AITipoEstado.Patrulha;
	}
		
	// Descricao	:	Chamado pelo State Machine pai, quando o zumbi chega no seu destino
	public override void 	DestinoEcontontrado ( bool foiEncontrado ) {
		if (_maquinaEstadoZumbi == null || !foiEncontrado)
			return;

		//Seleciona o proximo waypoint no waypoint network
		if (_maquinaEstadoZumbi.tipoAlvo == AITipodoAlvo.Waypoint)
			_maquinaEstadoZumbi.navAgent.SetDestination (_maquinaEstadoZumbi.PegaPosicaoWaypoint ( true ));
	}

		
	public override AITipoEstado NoUpdate (){
		
		if (_maquinaEstadoZumbi.AmeacaVisual.tipo==AITipodoAlvo.TipoVisual_Player){
			_maquinaEstadoZumbi.SetaAlvo ( _maquinaEstadoZumbi.AmeacaVisual );
			return AITipoEstado.Perseguicao;
		}

		if (_maquinaEstadoZumbi.AmeacaVisual.tipo==AITipodoAlvo.TipoVisual_Luz){
			_maquinaEstadoZumbi.SetaAlvo ( _maquinaEstadoZumbi.AmeacaVisual );
			return AITipoEstado.Alerta;
		}
			
		if (_maquinaEstadoZumbi.AmeacaAudivel.tipo==AITipodoAlvo.Audio){
			_maquinaEstadoZumbi.SetaAlvo (_maquinaEstadoZumbi.AmeacaAudivel );
			return AITipoEstado.Alerta;
		}

		//Se vimos um corpo morto entra no Pursuit se estamos com fome
		if (_maquinaEstadoZumbi.AmeacaVisual.tipo==AITipodoAlvo.TipoVisual_Comida){
			
			if ( (1.0f- _maquinaEstadoZumbi.satisfeito) > (_maquinaEstadoZumbi.AmeacaVisual.distance/_maquinaEstadoZumbi.RaioSensor)  ){
				_maquinaEstado.SetaAlvo ( _maquinaEstado.AmeacaVisual );
				return AITipoEstado.Perseguicao;
			}
		}
			
		if (_maquinaEstadoZumbi.navAgent.pathPending) {
			_maquinaEstadoZumbi.velocidade = 0;
			return AITipoEstado.Patrulha;
		}
		else
			_maquinaEstadoZumbi.velocidade = _velocidade;

		float angulo = Vector3.Angle (_maquinaEstadoZumbi.transform.forward, (_maquinaEstadoZumbi.navAgent.steeringTarget - _maquinaEstadoZumbi.transform.position));

		if (angulo > _virarNaPosicao) {
			return AITipoEstado.Alerta;
		}

		//Se o root rotation nao esta sendo usado entao estamos responsaveis por manter o zumbi rotacionado e olhando
		//na direçao correta
		if (!_maquinaEstadoZumbi.usaRotacao) {
			Quaternion newRot = Quaternion.LookRotation (_maquinaEstadoZumbi.navAgent.desiredVelocity);

			_maquinaEstadoZumbi.transform.rotation = Quaternion.Slerp( _maquinaEstadoZumbi.transform.rotation, newRot, Time.deltaTime * _slerp);
		}

		//Se por alguma razão o navAgent perdeu o caminho, entao chama a funçao Nextwaypoint 
		if (_maquinaEstadoZumbi.navAgent.isPathStale || 
			!_maquinaEstadoZumbi.navAgent.hasPath   ||
			_maquinaEstadoZumbi.navAgent.pathStatus!=UnityEngine.AI.NavMeshPathStatus.PathComplete) {
			_maquinaEstadoZumbi.navAgent.SetaDestinatino(_maquinaEstadoZumbi.PegaPosicaoWaypoint ( true ));
		}

		// Continua no estado
		return AITipoEstado.Patrulha;
	}

	public override void EntraEstado()	{
		Debug.Log ("Entrando estado patrulha");
		base.EntraEstado ();
		if (_maquinaEstadoZumbi == null)
			return;

		_maquinaEstadoZumbi.NavAgentControl (true, false);
		_maquinaEstadoZumbi.procura = 0;
		_maquinaEstadoZumbi.alimentando = false;
		_maquinaEstadoZumbi.tipoAtaque = 0;

		// Seta destino
		_maquinaEstadoZumbi.navAgent.SetDestination( _maquinaEstadoZumbi.PegaPosicaoWaypoint( false ) );

		_maquinaEstadoZumbi.navAgent.isStopped = false;
	}
	// Inpsector 
	[SerializeField] float			   _virarNaPosicao	= 80.0f;
	[SerializeField] float			   _slerp			= 5.0f;
	[SerializeField] [Range(0.0f, 3.0f)] float	 _velocidade			= 1.0f;

}