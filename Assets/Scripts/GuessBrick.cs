using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class GuessBrick : MonoBehaviour,IPointerClickHandler {
	public int type,number;

	public void OnPointerClick(PointerEventData EData){
		if(GameState.Instance.CurrentState==GameState.State.GuessingABrick){
			GameControl.Instance.GuessingBrick.GetComponent<Brick>().GuessTheBrick(number);
		}
	}
}
