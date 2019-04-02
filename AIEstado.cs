using UnityEngine;
using System.Collections;

// Descricao		:	A classe base para todos os AI States usados pelo sistema AI
public abstract class AIEstado : MonoBehaviour {
	// Metodo publico
	//Chamado pelo State Machine pai para atribuir sua referencia
	public virtual void SetaMaquinaEstado( AIStateMachine maquinaEstado ) { _maquinaEstado = maquinaEstado; }

	public virtual void			EntraEstado()			{}
	public virtual void 		SaiEstado()			{}
	public virtual void 		UpdateAnimator()	{}
	public virtual void 		EventoTrigger( AIEventoTipo tipoEvento, Collider other ){}
	public virtual void 		DestinoEcontontrado ( bool foiEncontrado ) {}

	//Metodos abstratos
	public abstract AITipoEstado PegaTipoEstado();
	public abstract AITipoEstado NoUpdate();

	// Protected Fields
	protected AIStateMachine	_maquinaEstado;

	// Desc	:	Converte a posiçao e o raio da Sphere Collider passada para dentro do espaço do mundo          	
	public static void ConverteSphereCollider( SphereCollider col, out Vector3 pos, out float raio ){
		// Default 
		pos 	= Vector3.zero;
		raio 	= 0.0f;

		//Se nao tem um sphere collider valido, retorna
		if (col == null)
			return;
		
		//Calcula o a posição do centro da esfera no mundo
		pos    = col.transform.position;
		pos.x += col.center.x * col.transform.lossyScale.x;
		pos.y += col.center.y * col.transform.lossyScale.y;
		pos.z += col.center.z * col.transform.lossyScale.z;

		//Calcula o raio da esfera no espaço do mundo
		raio = Mathf.Max(	col.radius * col.transform.lossyScale.x,
							col.radius * col.transform.lossyScale.y); 

		raio = Mathf.Max( raio, col.radius * col.transform.lossyScale.z);
	}
		
	// Descricao	:	Chamado pelo State Machine pai para permitir processar o root motion
	public virtual void 		UpdateAnimator(){
		//Obtem o numero de metros do root motion atualizou para esse update e divide por deltaTime para obter
		//metros por segundo. Depois atribui isso para a velocidade do NavAgent
		if (_maquinaEstado.usaPosicao)
			_maquinaEstado.navAgent.velocity = _maquinaEstado.animator.deltaPosition / Time.deltaTime;

		//Pega o root rotation do animator e atribui ao transform rotation
		if (_maquinaEstado.usaRotacao)
			_maquinaEstado.transform.rotation = _maquinaEstado.animator.rootRotation;

	}
	// Descricao	:	Retorna o anglo entre os vetores em graus
	public static float EncontraAngulo( Vector3 fromVector, Vector3 toVector ){
		if (fromVector == toVector)
			return 0.0f;

		float angulo = Vector3.Angle (fromVector, toVector);
		Vector3 cross = Vector3.Cross (fromVector, toVector);
		angulo *= Mathf.Sign (cross.y);
		return angulo;
	}
}