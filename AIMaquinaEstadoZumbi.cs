using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// Descricao		:	Maquina de estados usada pelos zumbis
public class AIZombieStateMachine : AIStateMachine{
	
	// Ragdoll 
	private AIBoneControlType			 _controleOssotipo  		= AIBoneControlType.Animado;
	private List<BodyPartSnapshot>		 _bodyPartSnapShots		= new List<BodyPartSnapshot>();
	private float					     _ragdollEndTime		= float.MinValue;
	private Vector3						 _ragdollPosicaoQuadril;
	private Vector3						 _ragdollPosicaoPe;
	private Vector3						 _ragdollPosicaoCabeca;
	private IEnumerator 				 _reanimaCorotina  = null;
	private float						 _mecanimTransitionTime	= 0.1f;

	public bool Grito(){
		if (estaGritando)
			return true;
		if (_animator == null || _cinematicEnabled || _prefabGrito == null)
			return false;

		_animator.SetTrigger (_gritoHash);
		Vector3 spawnPosico = _posicaoGrito == AIScreamPosition.Entity ? transform.position : AmeacaVisual.position;
		AIEmissorSom EmissorSom = Instantiate (_prefabGrito, spawnPosico, Quaternion.identity) as AIEmissorSom;

		if (EmissorSom != null)
			EmissorSom.SetaRaio (_raioGrito);
		return true;
	}

	public float ScreamChance{get{ return _chanceGrito;}}


	protected override void Update(){
		base.Update ();

		if (_animator!=null){
			_animator.SetFloat 	 (_velocidadeHash, _velocidade);
			_animator.SetBool	 (_alimentandoHash, _alimentando);
			_animator.SetInteger (_procurandoHash,	_procurando);
			_animator.SetInteger (_ataqueHash,	_tipoataque);
			_animator.SetInteger (_estadoHash, (int)_tipoEstadoAtual);
		
			//Esta gritando ou nao
			_taGritando = _cinematicEnabled?0.0f:_animator.GetFloat(_gritandoHash);
		}

		_satisfeito = Mathf.Max ( 0, _satisfeito - ((_depletionRate * Time.deltaTime)/100.0f) * Mathf.Pow( _velocidade, 3.0f));
	}

	protected void UpdateDanoNoAnimator(){
		if (_animator!=null){
			//Ativa o peso do layer Lower Body ou Upper Body dependendo do dano tomado
			if (_lowerBodyLayer != -1) {
				_animator.SetLayerWeight (_lowerBodyLayer, (_danoPartedeBaixo > _limpLimite && _danoPartedeBaixo < _rastejarLimite) ? 1.0f : 0.0f);
			}
			if (_upperBodyLayer != -1) {
				_animator.SetLayerWeight (_upperBodyLayer, (_danoPardeCima > _limiteDanoEmCima && _danoPartedeBaixo < _rastejarLimite) ? 1.0f : 0.0f);
			}

			_animator.SetBool( _rastejoHash, estaRastejando );
			_animator.SetInteger( _lowerBodyDamageHash , _danoPartedeBaixo );
			_animator.SetInteger( _upperBodyDamageHash, _danoPardeCima );
		}
	}

	//Descricao		: Processa a reação do zumbi ao levar dano
	public override void TomaDano( Vector3 position, Vector3 force, int damage, Rigidbody parteCorpo, CharacterManager characterManager, int hitDirection=0 ){
		if (GameSceneManager.instance!=null && GameSceneManager.instance.particulaSangue!=null){
			ParticleSystem sys  = GameSceneManager.instance.particulaSangue;
			sys.transform.position = position;
			var settings = sys.main;
			settings.simulationSpace = ParticleSystemSimulationSpace.World;
			sys.Emit(60);
		}

		float forcaDano = force.magnitude;

		if (_controleOssotipo==AIBoneControlType.Ragdoll){
			if (parteCorpo!=null){
				if (forcaDano>1.0f)
					parteCorpo.AddForce( force, ForceMode.Impulse );


				if (parteCorpo.CompareTag("Cabeca")){
					_vida = Mathf.Max( _vida-damage, 0);
				}else
					if (parteCorpo.CompareTag("Corpo Superior")){
						_danoPardeCima+=damage;
					}else
						if (parteCorpo.CompareTag("Corpo Inferior")){
							_danoPartedeBaixo+=damage;
						}

				UpdateDanoNoAnimator();

				if (_vida>0){
					if (_reanimaCorotina!=null)
						StopCoroutine (_reanimaCorotina);

					_reanimaCorotina = Reanima();
					IniciaCorotina( _reanimaCorotina );
				}
			}
			return;
		}

		//Pega a posição no espaço local do attacker
		Vector3 atacantePos  = transform.InverseTransformPoint( characterManager.transform.position );
		//Pega a posição no espaço local do Hit
		Vector3 hitPos		= transform.InverseTransformPoint( position );

		bool ragdoll = (forcaDano>1.0f);

		if (parteCorpo!=null){
			if (parteCorpo.CompareTag("Cabeca")){
				_vida = Mathf.Max( _vida-damage, 0);
				if (health==0) ragdoll = true;
			}else if (parteCorpo.CompareTag("Corpo Superior")){
					_danoPardeCima+=damage;
					UpdateDanoNoAnimator();
			}else if (parteCorpo.CompareTag("Corpo Inferior")){
						_danoPartedeBaixo+=damage;
						UpdateDanoNoAnimator();
						ragdoll = true;
			}
		}

		if (_controleOssotipo!=AIBoneControlType.Animado || estaRastejando || cinematicEnabled || atacantePos.z<0) ragdoll=true;

		if (!ragdoll){
			float angle = 0.0f;
			if (hitDirection==0){
				Vector3 vecToHit = (position - transform.position).normalized;
				angle = AIEstado.EncontraAngulo( vecToHit, transform.forward );
			}

			int tipoHit = 0;
			if (parteCorpo.gameObject.CompareTag ("Cabeca")){
				if (angle<-10 || hitDirection==-1) 	tipoHit=1;
				else
					if (angle>10 || hitDirection==1) 	tipoHit=3;
					else
						tipoHit=2;
			}else if (parteCorpo.gameObject.CompareTag("Corpo Superior" +
				"")){
				if (angle<-20 || hitDirection==-1)  tipoHit=4;
				else if (angle>20 || hitDirection==1) 	tipoHit=6;
				else
					tipoHit=5;
			}

			if (_animator){
				_animator.SetInteger( _tipoHitHash, tipoHit ); 
				_animator.SetTrigger( _hitTriggerHash );
			}
			return;
		}else{
			if (_tipoEstadoAtual){
				_tipoEstadoAtual.SaiEstado();
				_tipoEstadoAtual = null;
				_tipoEstadoAtual = AITipoEstado.Nenhum;
			}

			if (_navAgent) 
				_navAgent.enabled = false;
			if (_animator) 
				_animator.enabled = false;
			if (_collider) 
				_collider.enabled = false;

			estaDentroDaRange = false;

			foreach( Rigidbody body in _bodyParts ){
				if (body){
					body.isKinematic = false;
				}
			} 

			if (forcaDano>1.0f){
				if (parteCorpo!=null)
					parteCorpo.AddForce( force, ForceMode.Impulse );
			}

			_controleOssotipo = AIBoneControlType.Ragdoll;

			if (_vida>0){
				if (_reanimaCorotina!=null)
					StopCoroutine (_reanimaCorotina);

				_reanimaCorotina = Reanima();
				IniciaCorotina( _reanimaCorotina );
			}
		}

	}
	protected override void Start (){
		base.Start();

		if (_animator != null) {
			_lowerBodyLayer = _animator.GetLayerIndex ("Dano em baixo");
			_upperBodyLayer = _animator.GetLayerIndex ("Dano em cima");
		}

		// Cria uma Lista BodyPartSnapshot
		if (_osso!=null){
			Transform[] transforms = _osso.GetComponentsInChildren<Transform>();
			foreach( Transform trans in transforms ){
				BodyPartSnapshot snapShot = new BodyPartSnapshot();
				snapShot.transform = trans;
				_bodyPartSnapShots.Add( snapShot );
			}
		}

		UpdateDanoNoAnimator();
	}

	// Descricao	: Inicia o procedimento de reanimação
	protected IEnumerator Reanima (){
		//Reanima apenas se esta no estado de ragdoll
		if (_controleOssotipo!=AIBoneControlType.Ragdoll || _animator==null) yield break;
		//Aguarda pelos segundos desejados antes de iniciar o processo de reanimação 
		yield return new WaitForSeconds ( _reanimacaoTempoEspera );
		//Registra o tempo no inicio do processo de reanimação
		_ragdollEndTime = Time.time;
		//Seta o RigidBody de volta para kinematic
		foreach ( Rigidbody body in _bodyParts ){
			body.isKinematic = true;
		}

		//Entre no modo de reanimação
		_controleOssotipo = AIBoneControlType.RagdollPraAnimado;

		//Armazena as posições e rotações de todos os ossos para a reanimação
		foreach( BodyPartSnapshot snapShot in _bodyPartSnapShots ){
			snapShot.position 		= snapShot.transform.position;
			snapShot.rotation 		= snapShot.transform.rotation;
		}

		//Armazena a posição dos pés e da cabeça do ragdoll
		_ragdollPosicaoCabeca = _animator.GetBoneTransform( HumanBodyBones.Head ).position;
		_ragdollPosicaoPe = (_animator.GetBoneTransform(HumanBodyBones.LeftFoot).position + _animator.GetBoneTransform(HumanBodyBones.RightFoot).position) * 0.5f;
		_ragdollPosicaoQuadril  = _osso.position;

		//Habilita o animator
		_animator.enabled = true;

		if (_osso!=null){
			float forwardTest;

			switch (_alinhamentoDosOssos){
			case AIBoneAlignmentType.ZAxis:
				forwardTest = _osso.forward.y; break;
			case AIBoneAlignmentType.ZAxisInverted:
				forwardTest = -_osso.forward.y; break;
			case AIBoneAlignmentType.YAxis:
				forwardTest = _osso.up.y; break;
			case AIBoneAlignmentType.YAxisInverted:
				forwardTest = -_osso.up.y; break;
			case AIBoneAlignmentType.XAxis:
				forwardTest = _osso.right.y; break;
			case AIBoneAlignmentType.XAxisInverted:
				forwardTest = -_osso.right.y; break;
			default:
				forwardTest = _osso.forward.y; break;
			}

			//Seta o trigger no animator
			if (forwardTest>=0)
				_animator.SetTrigger( _reanimateFromBackHash ) ;
			else
				_animator.SetTrigger( _reanimateFromFrontHash );
		}
	}

	//Descricao    : Chamado pela Unity no fim de cada frame do update. Usado aqui para fazer a reanimação
	protected virtual void LateUpdate(){
		if ( _controleOssotipo==AIBoneControlType.RagdollPraAnimado  ){
			if (Time.time <= _ragdollEndTime + _mecanimTransitionTime ){
				Vector3 animatedToRagdoll = _ragdollPosicaoQuadril - _osso.position;
				Vector3 newRootPosition   = transform.position + animatedToRagdoll;

				RaycastHit[] hits = Physics.RaycastAll( newRootPosition + (Vector3.up * 0.25f) , Vector3.down, float.MaxValue, _geometryLayers);
				newRootPosition.y = float.MinValue;
				foreach( RaycastHit hit in hits){
					if (!hit.transform.IsChildOf(transform)){
						newRootPosition.y = Mathf.Max( hit.point.y, newRootPosition.y );
					}
				}

				NavMeshHit navMeshHit;
				Vector3 baseOffSet = Vector3.zero;
				if (_navAgent)
					baseOffSet.y = _navAgent.baseOffset;
				if (NavMesh.SamplePosition( newRootPosition, out navMeshHit, 25.0f, NavMesh.AllAreas )){
					transform.position = navMeshHit.position + baseOffSet;
				}else{
					transform.position = newRootPosition + baseOffSet;
				}

				Vector3 direcaoragdoll = _ragdollPosicaoCabeca - _ragdollPosicaoPe;
				direcaoragdoll.y = 0.0f;

				Vector3 posicaoPe=0.5f*(_animator.GetBoneTransform(HumanBodyBones.LeftFoot).position + _animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
				Vector3 direcaoAnimacao= _animator.GetBoneTransform(HumanBodyBones.Head).position - posicaoPe;
				direcaoAnimacao.y=0.0f;

				//Tenta dar Match nas rotações. Podemos rotacionar apenas no eixo Y
				transform.rotation*=Quaternion.FromToRotation(direcaoAnimacao.normalized,direcaoragdoll.normalized);
			}
			//Calcula o valor de interpolação
			float qtd = Mathf.Clamp01 ((Time.time - _ragdollEndTime - _mecanimTransitionTime) / _reanimacaoTempo);

			foreach( BodyPartSnapshot snapshot in _bodyPartSnapShots ){
				if (snapshot.transform == _osso ){
					snapshot.transform.position = Vector3.Lerp( snapshot.position, snapshot.transform.position, qtd);
				}

				snapshot.transform.rotation = Quaternion.Slerp( snapshot.rotation, snapshot.transform.rotation, qtd );					
			}

			//Condiçao para sair do modo de reanimação
			if (qtd==1.0f){
				_controleOssotipo = AIBoneControlType.Animado;
				if (_navAgent) _navAgent.enabled = true;
				if (_collider) _collider.enabled = true;

				AIEstado newState = null;
				if (_estados.TryGetValue( AITipoEstado.Alerta, out newState )){
					if (_tipoEstadoAtual!=null) _tipoEstadoAtual.SaiEstado();
					newState.EntraEstado();
					_tipoEstadoAtual = newState;
					_tipoEstadoAtual = AITipoEstado.Alerta;
				}
			}
		}
	}
	public enum AIBoneControlType { Animado, Ragdoll, RagdollPraAnimado }
	public enum AIScreamPosition {Entity, Player}

	// Classe 			: BodyPartSnapshot
	// Descricao 		: Usado para guardar informações sobre a posição de cada parte do corpo quando estiver transitando
	//					  do ragdoll
	public class BodyPartSnapshot{
		public Transform 	transform;
		public Vector3   	position;
		public Quaternion	rotation;
	}
	// Inspector
	[SerializeField]	[Range(10.0f, 360.0f)]	float _fov 			= 50.0f;
	[SerializeField]	[Range(0.0f, 1.0f)]		float _sentido 		= 0.5f;
	[SerializeField]	[Range(0.0f, 1.0f)]		float _ouvindo		= 1.0f;
	[SerializeField]	[Range(0.0f, 1.0f)]		float _agressao 	= 0.5f;
	[SerializeField]	[Range(0, 100)]			int   _vida		= 100;
	[SerializeField]	[Range(0, 100)]			int   _danoPartedeBaixo 		= 0;
	[SerializeField]	[Range(0, 100)]			int   _danoPardeCima 		= 0;
	[SerializeField]	[Range(0, 100)]			int	  _limiteDanoEmCima 	= 30;
	[SerializeField]	[Range(0, 100)]			int	  _limpLimite		= 30;
	[SerializeField]	[Range(0, 100)]			int   _rastejarLimite		= 90;
	[SerializeField]	[Range(0.0f, 1.0f)]		float _inteligencia			= 0.5f;
	[SerializeField]	[Range(0.0f, 1.0f)]		float _satisfeito			= 1.0f;
	[SerializeField]							float 		_replenishRate		= 0.5f;
	[SerializeField]							float 		_depletionRate		= 0.1f;
	[SerializeField] 							float 		_reanimacaoTempo = 1.5f;
	[SerializeField]							float 		_reanimacaoTempoEspera	= 3.0f;
	[SerializeField]							LayerMask	_geometryLayers			= 0;

	[SerializeField] [Range(0.0f, 1.0f)] float _chanceGrito = 1.0f;
	[SerializeField] [Range(0.0f, 50.0f)] float _raioGrito = 20.0f;
	[SerializeField] AIScreamPosition _posicaoGrito = AIScreamPosition.Entity;
	[SerializeField] AISoundEmitter _prefabGrito = null;

	// Private
	private	int		_procurando 	= 0;
	private bool	_alimentando 	= false;
	private bool	_rastejando	= false;
	private int		_tipoataque	= 0;
	private float	_velocidade		= 0.0f;
	private float   _taGritando = 0.0f;

	// Hashes
	private int		_velocidadeHash		=	Animator.StringToHash("Velocidade");
	private int 	_procurandoHash 	= 	Animator.StringToHash("Procurando");
	private int 	_alimentandoHash	=	Animator.StringToHash("Alimentando");
	private int		_ataqueHash		=	Animator.StringToHash("Ataque");
	private int 	_rastejoHash	=	Animator.StringToHash("Rastejando");
	private int		_hitTriggerHash 		=   Animator.StringToHash("Hit");
	private int		_tipoHitHash			=	Animator.StringToHash("TipoHit");
	private int		_lowerBodyDamageHash	=   Animator.StringToHash("Dano em baixo");
	private int		_upperBodyDamageHash	=	Animator.StringToHash("Dano em cima");
	private int		_reanimateFromBackHash	=	Animator.StringToHash("Reanima por tras");
	private int		_reanimateFromFrontHash =   Animator.StringToHash("Reanima pela frente");	
	private int 	_estadoHash 	   = Animator.StringToHash("Estado");
	private int     _upperBodyLayer = -1;
	private int     _lowerBodyLayer = -1;
	private int 	_gritandoHash = Animator.StringToHash("Gritando");
	private int     _gritoHash = Animator.StringToHash("Grito");

	// Propriedades Publicas
	public float			replenishRate{ get{ return _replenishRate;}}
	public float			fov		 	{ get{ return _fov;		 }}
	public float			ouvindo	 	{ get{ return _ouvindo;	 }}
	public float            sentido		{ get{ return _sentido;	 }}
	public bool 			rastejando	{ get{ return _rastejando; }}
	public float			inteligencia{ get{ return _inteligencia;}}
	public float			satisfeito{ get{ return _satisfeito; }	set{ _satisfeito = value;}}
	public float			aggression	{ get{ return _agressao; }	set{ _agressao = value;}	}
	public int				health		{ get{ return _vida; }		set{ _vida = value;}	}
	public int				tipoAtaque	{ get{ return _tipoataque; }	set{ _tipoataque = value;}}
	public bool				alimentando  	{ get{ return _alimentando; }		set{ _alimentando = value;}	}
	public int				procura		{ get{ return _procurando; }		set{ _procurando = value;}	}
	public float			velocidade    	{ get{ return _velocidade;	}		set{ _velocidade = value;} }
	public bool	estaRastejando{get{ return ( _danoPartedeBaixo>= _rastejarLimite ); }}
	public bool estaGritando{ get{ return _taGritando > 0.1f; }}


}