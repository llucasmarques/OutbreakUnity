using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum DisplayCaminho { None, Conexoes, Caminhos }

// Descricao		:	Contem uma lista de Waypoint. Cada waypoint e uma referencia ao transfom. 
public class AIWaypoint : MonoBehaviour {		
	[HideInInspector] public DisplayCaminho DisplayModo = DisplayCaminho.Conexoes;
	[HideInInspector] public int UIinicio 	= 0;											
	[HideInInspector] public int UIfim	= 0;											

	public List<Transform> Waypoints   = new List<Transform>();
}