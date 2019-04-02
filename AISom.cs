using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AISom : MonoBehaviour {

	void Awake () {
		//Referencia do Collider
		_collider = GetComponent<SphereCollider> ();
		if (!_collider)
			return;

		//Seta o raio
		_raioOrigem = _raioTgt = _collider.radius;

		//Setup interpolator
		_interpolador = 0.0f;
		if (_declinioRate > 0.02f)
			_velocidadeInterpolador = 1.0f / _declinioRate;
		else
			_velocidadeInterpolador = 0.0f;
	}

	void FixedUpdate(){
		if (!_collider)
			return;
		_interpolador = Mathf.Clamp01 (_interpolador + Time.deltaTime * _velocidadeInterpolador);
		_collider.radius = Mathf.Lerp (_raioOrigem, _raioTgt, _interpolador);

		if (_collider.radius < Mathf.Epsilon)
			_collider.enabled = false;
		else
			_collider.enabled = true;
	}

	public void SetRadius (float newRadius, bool instantResize = false){
		if (!_collider || newRadius == _raioTgt)
			return;
		_raioOrigem = (instantResize || newRadius>_collider.radius)? newRadius: _collider.radius;
		_raioTgt = newRadius;
		_interpolador = 0.0f;

	}
	// Inspector
	[SerializeField] private float _declinioRate = 1.0f;

	// Interno
	private SphereCollider _collider = null;
	private float _raioOrigem = 0.0f;
	private float _raioTgt = 0.0f;
	private float _interpolador = 0.0f;
	private float _velocidadeInterpolador = 0.0f;
}