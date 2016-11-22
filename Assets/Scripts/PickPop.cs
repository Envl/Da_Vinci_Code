using UnityEngine;
using System.Collections;

public class PickPop : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnMouseDown(){
		switch(GameState.Instance.CurrentState){
		case GameState.State.InitialSelectBricks:
			GameControl.Instance.RequestInitialPick(GameControl.Instance.CandidateBrick);
			break;
		// 这种时候pick无人竞争   所以本地操作  结果传给主机
		case GameState.State.PickABrick:
			GameControl.Instance.PickTheCandidate();
			break;

		}
	}
}
