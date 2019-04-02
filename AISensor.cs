using UnityEngine;
using System.Collections;

// Descricao		:	Notifica oo AIStateMachine pai de qualquer ameaça que entra 
//                      o trigger via o metodo OnTriggerEvente do ATStateMachine
public class AISensor : MonoBehaviour {
	// Private
	private AIStateMachine	_maquinaEstadoPai	=	null;
	public AIStateMachine maquinaEstadoPai{ set{ _maquinaEstadoPai = value; }}

	void TriggerEntra( Collider col ){
		if (_maquinaEstadoPai!=null)
			_maquinaEstadoPai.OnTriggerEvent ( AIEventoTipo.Entra,col );
	}

	void TriggerFica( Collider col ){
		if (_maquinaEstadoPai!=null)
			_maquinaEstadoPai.OnTriggerEvent ( AIEventoTipo.Fica, col );
	}

	void TriggerSai( Collider col ){
		if (_maquinaEstadoPai!=null)
			_maquinaEstadoPai.OnTriggerEvent ( AIEventoTipo.Sai,  col );
	}

}