using UnityEngine;
using System.Collections;

public class AIDano : MonoBehaviour {
	
	void Start(){
		//Cache a referencia do animator e state machine
		_maquinaEstado = transform.root.GetComponentInChildren<AIStateMachine> ();

		if (_maquinaEstado != null)
			_animator = _maquinaEstado.animator;
		
		_parametroHash = Animator.StringToHash (_parametro); 

		_gameSceneManager = GameSceneManager.instance;
	}

	// Desc	:	Chamado pela unity a cada update que esse Trigger esta em contato com outro
	void NoTriggerFica( Collider col ){
		//Se nao tem nenhum animator, retorna
		if (!_animator)
			return;
		
		//Se esse é o objeto PLAYER e o parametro esta setado para damage
		if (col.gameObject.CompareTag ("Player") && _animator.GetFloat(_parametroHash) >0.9f){
			if (GameSceneManager.instance && GameSceneManager.instance.particulaSangue) {
				ParticleSystem system = GameSceneManager.instance.particulaSangue;

				//Codigo Temporario
				system.transform.position = transform.position;
				system.transform.rotation = Camera.main.transform.rotation;

				var settings = system.main;
				settings.simulationSpace = ParticleSystemSimulationSpace.World;
				system.Emit (_particulaSangue);
			}

			if (_gameSceneManager!=null){
				PlayerInfo info = _gameSceneManager.GetPlayerInfo( col.GetInstanceID() );
				if (info!=null && info.characterManager!=null){
					info.characterManager.TomaDano( _qtdDano );
				}
			}
		}
	}

	// Private
	AIStateMachine  	_maquinaEstado 		= null;
	Animator	   	 	_animator	 		= null;
	int			    	_parametroHash		= -1;
	GameSceneManager	_gameSceneManager	=	null;

	// Inspector 
	[SerializeField] string			_parametro = "";
	[SerializeField] int			_particulaSangue	=	10;
	[SerializeField] float			_qtdDano				=	0.1f;

}