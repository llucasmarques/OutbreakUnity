using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// Descricao		:	Descreve um alvo em potencial para o AI
public struct AIAlvo{
	private		AITipodoAlvo 	_tipo;			//Tipo do alvo
	private		Collider		_collider;		
	private		Vector3			_posicao;		//Posicao atual no mundo
	private		float			_distancia;		//Distancia do player
	private		float			_tempo;			

	public AITipodoAlvo	tipo 		{ get{ return _tipo;}}
	public Collider		collider 	{ get{ return _collider;}}
	public Vector3		position	{ get{ return _posicao;}}
	public float		distance	{ get{ return _distancia;} set {_distancia = value;}}
	public float		time		{ get{ return _tempo;}}

	public void Seta( AITipodoAlvo t, Collider c, Vector3 p, float d ){
		_tipo		=	t;
		_collider	=	c;
		_posicao	=	p;
		_distancia	=	d;
		_tempo		=	Time.time;
	}

	public void Limpar(){
		_tipo		=	AITipodoAlvo.Nenhum;
		_collider	=	null;
		_posicao	=	Vector3.zero;
		_tempo		=	0.0f;
		_distancia	=	Mathf.Infinity;
	}
}

// Descricao		:	Classe base para todos os AI State Machine
public abstract class AIStateMachine : MonoBehaviour{
	// Public
	public AIAlvo		AmeacaVisual	=	new AIAlvo();
	public AIAlvo		AmeacaAudivel		=	new AIAlvo();

	public float RaioSensor{
		get{
			if (_sensor==null)	return 0.0f;
			float radius = Mathf.Max(	_sensor.radius * _sensor.transform.lossyScale.x,
										_sensor.radius * _sensor.transform.lossyScale.y); 

			return Mathf.Max( radius, _sensor.radius * _sensor.transform.lossyScale.z);
		}
	}
	public bool cinematicEnabled{ get{ return _cinematicEnabled;} set { _cinematicEnabled = value; }}

	public bool				usaPosicao	{ get{ return _posicaoRefCount>0; }}
	public bool 			usaRotacao	{ get{ return _rotacaoRefCount>0; }}
	public AITipodoAlvo		tipoAlvo 	   	{ get { return _alvo.tipo; }}
	public Vector3			posicaoAlvo 	{ get { return _alvo.posicao; } }
	public int				IDColliderAlvo{
		get{
			if (_alvo.collider)
				return _alvo.collider.GetInstanceID ();
			else
				return -1;
		}
	}

	// Descricao	:	Cache de Componentes
	protected virtual void Awake(){
		//Cache todos componentes acessados frequentemente
		_transform	=	transform;
		_animator	=	GetComponent<Animator>();
		_navAgent	=	GetComponent<NavMeshAgent>();
		_collider	=	GetComponent<Collider>();

		//Body part layer
		_aiPartedoCorpo = LayerMask.NameToLayer("AI parte corpo");

		//Se tem um GameSceneManager valido
		if (GameSceneManager.instance!=null){
			//Registra o State Machine com o database da cena
			if (_collider) 			GameSceneManager.instance.RegistraMaquinaEstadoIA( _collider.GetInstanceID(), this );
			if (_sensor)		GameSceneManager.instance.RegistraMaquinaEstadoIA( _sensor.GetInstanceID(), this );
		}

		if (_osso != null) {
			Rigidbody[] corpo = _osso.GetComponentsInChildren<Rigidbody> ();
		
			foreach (Rigidbody bodyPart in corpo) {
				if (bodyPart != null && bodyPart.gameObject.layer == _aiPartedoCorpo) {
					_bodyParts.Add (bodyPart);
					GameSceneManager.instance.RegistraMaquinaEstadoIA (bodyPart.GetInstanceID (), this);
				}
			}
		}
	}

	public void OverrideEstado(AITipoEstado estado){
		//Seta o estado atual
		if (estado !=_tipoEstadoAtual && _estados.ContainsKey (estado)) {
			if (_tipoEstadoAtual != null)
				_tipoEstadoAtual.EntraEstado ();

			_tipoEstadoAtual = _estados [estado];
			_tipoEstadoAtual = estado;
			_tipoEstadoAtual.EntraEstado ();
		}
	}

	public virtual void TomaDano(Vector3 position, Vector3 force, int damage, Rigidbody bodyPart, CharacterManager characterManager, int hitDirection =  0){
		Debug.Log ("Dano!");
	}

	protected virtual void Start(){
		//Seta o Sensor Trigger pai para essa maquina de estado
		if (_sensor!=null){
			AISensor script = _sensor.GetComponent<AISensor>();
			if (script!=null){
				script.maquinaEstadoPai = this;
			}
		}

		//Busca todos os estados nesse game objetc
		AIEstado[] estados = GetComponents<AIEstado>();

		//Loop atraves de todos os estados e adiciona eles ao dicionario de estados
		foreach( AIEstado estado in estados ){
			if (estado!=null && !_estados.ContainsKey(estado.PegaTipoEstado())){
				//Adiciona esse estado ao dicionario de estados
				_estados[estado.PegaTipoEstado()] = estado;
				//E seta o State Machine pai desse estado
				estado.SetaMaquinaEstado(this);
			}
		}

		//Seta o estado atual
		if (_estados.ContainsKey( _tipoEstadoAtual )){
			_tipoEstadoAtual = _estados[_tipoEstadoAtual];
			_tipoEstadoAtual.EntraEstado();
		}else{
			_tipoEstadoAtual=	null;
		}

		//Busca todos AIStateMachineLink e seu comportamento derivado do animator e seta a referencia do State Machine
		//Para esse State Machine
		if (_animator){
			AIStateMachineLink[]  scripts	=	_animator.GetBehaviours<AIStateMachineLink>();
			foreach( AIStateMachineLink script in scripts ){
				script.stateMachine = this;
			}
		}
	}

	// Desc	:	Chamado para selecionar um novo waypoint seja ele em sequencia ou em ordem aleatoria 
	//			Seta o novo waypoint como o novo alvo e gera um NavAgent Path para isso
	private void ProximoWaypoint(){		
		//Incrementa o Waypoint atual com o Wrap-around para zero ou escolhe um waypoint randomico
		if (_patrulhaRandomica && _waypoint.Waypoints.Count>1){
			//Mantem gerando um waypoint randomito ate acharmos um que nao e o atual
			int oldWaypoint = _waypointAtual;
			while (_waypointAtual==oldWaypoint){
				_waypointAtual = Random.Range (0,_waypoint.Waypoints.Count);
			}
		}else
			_waypointAtual = _waypointAtual==_waypoint.Waypoints.Count-1?0:_waypointAtual+1;

	}

	// Descricao	:	Busca a posição no mundo em que o State Machine setou o waypoint como um incremento opcional
	public Vector3 PegaPosicaoWaypoint ( bool increment ){
		if (_waypointAtual == -1) {
			if (_patrulhaRandomica)
				_waypointAtual = Random.Range (0, _waypoint.Waypoints.Count);
			else
				_waypointAtual = 0;
		} else if (increment)
			ProximoWaypoint ();

		//Busca um novo waypoint na lista de waypoints
		if( _waypoint.Waypoints[_waypointAtual]!=null){
			Transform newWaypoint = _waypoint.Waypoints [_waypointAtual];

			//Nova posiçao do alvo
			SetaAlvo (	AITipodoAlvo.Waypoint, 
				null, 
				newWaypoint.position, 
				Vector3.Distance(newWaypoint.position , transform.position));

			return newWaypoint.position;
		}

		return Vector3.zero;
	}

	// Descricao	:	Limpa o alvo atual
	public void LimpaAlvo(){
		_alvo.Clear();
		if (_alvo!=null){
			_alvo.enabled = false;
		}
	}

	// Descricao	:	Seta o alvo atual e configura o trigger do alvo. Esse metodo permite especificar uma distancia de parada
	//          customizada
	public void SetaAlvo( AITipodoAlvo t, Collider c, Vector3 p, float d, float s ){
		//Seta os dados do novo alvo
		_alvo.Set( t, c, p, d );

		//Configura e habilita o trigger do alvo a possicao correta e o raio correto
		if (_alvo!=null){
			_alvo.radius = s;
			_alvo.transform.position = _alvo.position;
			_alvo.enabled = true;
		}
	}

	// Descricao	:	Seta o alvo atual e configura o trigger do alvo
	public void SetaAlvo( AIAlvo t ){
		//Atribui o novo alvo
		_alvo = t;

		//Configura e habilita o trigger do alvo a posiçao correta e ao raio correto
		if (_alvo!=null){
			_alvo.radius = _pararDistancia;
			_alvo.transform.position = t.position;
			_alvo.enabled = true;
		}
	}

	// Descricao	:	Seta o alvo atual e configura o trigger do alvo
	public void SetaAlvo( AITipodoAlvo t, Collider c, Vector3 p, float d ){
		//Seta a informaçao do alvo
		_alvo.Set( t, c, p, d );

		//Configura e habilita o trigger do alvo a posicao correta e o raio correto
		if (_alvo!=null)
		{
			_alvo.radius = _pararDistancia;
			_alvo.transform.position = _alvo.position;
			_alvo.enabled = true;
		}
	}
		
	// Descricao	:	Chamado pela Unity a cada tick do sistema físico.
	//					Isso limpa as ameaças por audio e visual a cada update e recalcula a distancia do alvo atual
	protected virtual void FixedUpdate(){
		AmeacaVisual.Limpar();
		AmeacaAudivel.Limpar();

		if(_alvo.tipo!=AITipodoAlvo.Nenhum){
			_alvo.distance = Vector3.Distance( _transform.position, _alvo.position);
		}

		_alvoEncontrado = false;
	}

	// Descricao	:	Chamado pela Unity a cada frame. Da ao estado atual a chance de atualizar ele mesmo e fazer transições
	protected virtual void Update(){
		if (_tipoEstadoAtual==null) return;

		AITipoEstado novoTipoEstado = _tipoEstadoAtual.NoUpdate();
		if ( novoTipoEstado != _tipoEstadoAtual){
			AIEstado novoEstado = null;
			if (_estados.TryGetValue( novoTipoEstado, out novoEstado)){
				_tipoEstadoAtual.SaiEstado();
				novoEstado.EntraEstado();
				_tipoEstadoAtual = novoEstado;
			}
			else if (_estados.TryGetValue( AITipoEstado.Ocioso , out novoEstado)){
				_tipoEstadoAtual.EntraEstado();
				novoEstado.EntraEstado();
				_tipoEstadoAtual = novoEstado;
			}

			_tipoEstadoAtual = novoTipoEstado;
		}
	}

	//	Descricao	:	Chamado pelo sistema físico quando o Main collider do AI 
	//					entra no trigger. Isso permite o estado filho saber quando isso entrou na esfera de influencia
	//					do waypoint ou da posição do ultimo player
	protected virtual void OnTriggerEnter( Collider other ){
		if (_alvo==null || other!=_alvo) return;

		_alvoEncontrado = true;

		//Notifica o state filho
		if (_tipoEstadoAtual)
			_tipoEstadoAtual.DestinoEncontrado( true );
	}

	protected virtual void OnTriggerStay( Collider other ){
		if (_alvo==null || other!=_alvo) return;

		_alvoEncontrado = true;
	}

	//	Descricao	:	Informa ao state filho que a entidade AI nao está mais no destino 
	//					(Tipicamente True quando um novo algo foi setado pelo filho
	protected void OnTriggerExit( Collider other ){
		if (_alvo==null || _alvo!=other) return;

		_alvoEncontrado = false;

		if (_tipoEstadoAtual!=null)
			_tipoEstadoAtual.ChegouDestino( false );
	}

	// Desc	:	Called by our AISensor component when an AI Aggravator
	//			has entered/exited the sensor trigger.
	public virtual void OnTriggerEvent( AIEventoTipo type, Collider other ){
		if(_tipoEstadoAtual!=null)
			_tipoEstadoAtual.EventoTrigger( type, other );
	}

	// Descricao	:	Chamado pela Unity depois do root motion ser avaliado mas nao aplicado ao objeto.
	//			Isso nos permite determinar via codigo o que fazer com a informaçao do root motion.
	protected virtual void OnAnimatorMove(){
		if (_tipoEstadoAtual!=null)
			_tipoEstadoAtual.UpdateAnimator();
	}

	// Descricao	:	Chamado pelo StateMachineBehaviour para habilitar/desabilitar o root motion
	public void AddRootMotionRequest( int rootPosition, int rootRotation ){
		_posicaoRefCount+= rootPosition;
		_rotacaoRefCount+= rootRotation;
	}

	// Descricao	: Chamado pela Unity antes do sistema IK ser atualizado, dando uma chance de configurar o alvo IK e os pesos
	protected virtual void OnAnimatorIK ( int layerIndex ){
		if (_tipoEstadoAtual!=null)
			_tipoEstadoAtual.UpdateAnimator();
	}

	// Descricao	:	Configura o NavMeshAgent para habilitar/desabilitar auto update da posicao/rotacao para
	//   				o nosso transform
	public void NavAgentControl( bool positionUpdate, bool rotationUpdate ){
		if (_navAgent){
			_navAgent.updatePosition = positionUpdate;
			_navAgent.updateRotation = rotationUpdate;
		}
	}
	//Inspector
	[SerializeField]	protected AIBoneAlignmentType _alinhamentoDosOssos=   AIBoneAlignmentType.ZAxis;
	[SerializeField]    protected Transform 		_osso 			= 	null;
	[SerializeField]	protected AITipoEstado		_tipoEstadoAtual	=	AITipoEstado.Ocioso;
	[SerializeField]	protected SphereCollider	_alvo		=	null;
	[SerializeField]	protected SphereCollider	_sensor		=	null;
	[SerializeField] 	protected AIWaypoint _waypoint 	= 	null;
	[SerializeField] 	protected bool			    _patrulhaRandomica		= 	false;
	[SerializeField] 	protected int			    _waypointAtual 	= 	-1;
	[SerializeField]	[Range(0,15)]	protected float		_pararDistancia	=	1.0f;


	// Protected
	protected AIEstado	_tipoEstadoAtual						=	null;
	protected Dictionary< AITipoEstado, AIEstado > _estados	=	new Dictionary< AITipoEstado, AIEstado>();
	protected AIAlvo	_alvo								=	new AIAlvo();
	protected int		_posicaoRefCount				=	0;
	protected int		_rotacaoRefCount				=	0;
	protected bool		_alvoEncontrado					=   false;
	protected List<Rigidbody> _bodyParts					= 	new List<Rigidbody>();
	protected int 		_aiPartedoCorpo					= 	-1;
	protected bool      _cinematicEnabled 					= 	false;

	//Cache de componentes
	protected Animator		_animator		=	null;
	protected NavMeshAgent	_navAgent		=	null;
	protected Collider		_collider		=	null;
	protected Transform		_transform		=	null;


	//Propriedades publicas
	public bool				alvoProximo	{ get{ return _alvoEncontrado;}}
	public bool				estaDentroDaRange	{ get; set; }
	public Animator 		animator 		{ get{ return _animator; }}
	public NavMeshAgent 	navAgent 		{ get{ return _navAgent; }}
	public Vector3			posicaoSensor{
		get{
			if (_sensor==null) return Vector3.zero;
			Vector3 point = _sensor.transform.position;
			point.x += _sensor.center.x * _sensor.transform.lossyScale.x;
			point.y += _sensor.center.y * _sensor.transform.lossyScale.y;
			point.z += _sensor.center.z * _sensor.transform.lossyScale.z;
			return point;
		}
	}

}
//Enum publico do sistema AI
public enum AITipoEstado 		{ Nenhum, Ocioso, Alerta, Patrulha, Ataque, Alimentando, Perseguicao, Dead }
public enum AITipodoAlvo		{ Nenhum, Waypoint, TipoVisual_Player, TipoVisual_Luz, TipoVisual_Comida, Audio }
public enum AIEventoTipo	{ Entra, Fica, Sai }
public enum AIBoneAlignmentType { XAxis, YAxis, ZAxis, XAxisInverted, YAxisInverted, ZAxisInverted }