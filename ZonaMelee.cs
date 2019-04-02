using UnityEngine;
using System.Collections;

public class ZonaMelee : MonoBehaviour {
	void EntraTrigger( Collider collider ){
		AIStateMachine maquina = GameSceneManager.instance.GetAIMaquinaDeEstado( collider.GetInstanceID() );
		if (maquina){
			maquina.estaDentroDaRange = true;
		}
	}

	void SaiTrigger( Collider col){
		AIStateMachine maquina = GameSceneManager.instance.GetAIMaquinaDeEstado( col.GetInstanceID() );
		if (maquina){
			maquina.estaDentroDaRange = false;
		}
	}
}