using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Brick : MonoBehaviour {
	//bool _isOnland=false;//是否落地
	bool _needAddForce=false;
	[HideInInspector]
	public int OwnerID;//这张brick是属于谁的  id从 1开始取值
	float forceX,forceY,forceZ;

	public  int  BrickNumber;
	public int BrickType;
	public int FixedIndex;//在AllBricks中的固定index编号
	public bool IsOpen=false;//棋子是否倒地公开
	public List<Transform> Arrows4Joker;//装载本Brick生成的所有arrow的容器

	void Start(){
		Arrows4Joker=new List<Transform>();
	}

	/*void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.tag == "Floor")
		{
			this._isOnland = true;
		}
	}*/


	void OnMouseUpAsButton(){
		switch(GameState.Instance.CurrentState){
		case GameState.State.OpenABrick:
			if(//这个棋是我的 并且此时我应该公开一张牌
				GameControl.Instance.Decks[GameControl._myDeckIndex].Contains(transform)
			){
				OpenMe();
				//通知其他玩家
				GameControl.Instance.InformOtherOpenABrick(transform);
				//如果我没有牌了  那么我完蛋了
				if(GameControl.Instance.CheckMyRemanent()==0){
					GameControl.Instance.IamDone();
					//进入观战状态
					GameState.Instance.CurrentState=GameState.State.Watching;
				}
				else{
					//进入等待状态
					GameState.Instance.CurrentState=GameState.State.WaitForOthers;
				}
				//通知轮到下一个玩家
				GameControl.Instance.ToNextOne();

			}
			break;
		case GameState.State.PickABrick:
			if(//棋是桌面上还没被拿走的
				GameControl.Instance.UnOccupiedBricks.Contains(transform)){
				//_needAddForce=true;
				//弹出一个气泡按钮 确认选择这个棋
				GameControl.Instance.PickPop.position=transform.position+new Vector3(0,2.5f,0);
				//把本棋子设置为候选棋子
				GameControl.Instance.CandidateBrick=transform;

			}
			break; 
		case GameState.State.GuessABrick:
			//这张牌不是自己的
			if(!GameControl.Instance.Decks[0].Contains(transform)
			//也不是unOccupied的
			&&!GameControl.Instance.UnOccupiedBricks.Contains(transform)
			//并且这张牌是unOpened
			&&GameControl.Instance.UnOpenedBricks.Contains(transform)){
				GameControl.Instance.GuessingBrick=transform;
				//正在被猜的牌 用动画或者颜色或者标记 做视觉提示

				//显示可供猜的选项
				GameControl.Instance.ShowPossibleBricks(GetComponent<Brick>().BrickType);
				//显示取消按钮
				GameControl.Instance.CancelBtn.gameObject.SetActive(true);
				//切换状态
				GameState.Instance.CurrentState=GameState.State.GuessingABrick;
			}
			break;
		case GameState.State.InitialSelectBricks:
			//没拿够四张牌
			if(GameControl.Instance.Decks[GameControl._myDeckIndex].Count<4){
				if(//棋是桌面上还没被拿走的
					GameControl.Instance.UnOccupiedBricks.Contains(transform)){
					print("ready to pick this");
					//_needAddForce=true;
					//弹出一个气泡按钮 确认选择这个棋
					GameControl.Instance.PickPop.position=transform.position+new Vector3(0,2.5f,3);
					//把本棋子设置为候选棋子
					GameControl.Instance.CandidateBrick=transform;
				}
			}

			break;
			//这个阶段里只有Joker牌(dash) 才会响应用户操作
		case GameState.State.Adjust:
			//joker牌的 数字 在prefab被我设置成 -1了
			//我是Joker牌  并且属于本玩家 那么 我就要响应一些操作..  自己的deck编号就是0
			if(GetComponent<Brick>().BrickNumber==-1
				&& GameControl.Instance.Decks[0].Contains(transform)){
				SwitchArrows();
			}
			break;
		}

	}

	public void SwitchArrows(){
		//arrow已经显示  
		if(Arrows4Joker.Count>0){
			DestroyArrows();
		}
		//还没有显示
		else{
			//          如果有空可以考虑做成支持手指拖动过去.... 
			//生成一堆跳动的箭头,指向这张Joker可以去的位置(也就是任意两张牌之间)
			foreach(var brick in GameControl.Instance.Decks[0]){
				//在所有不是自己的Brick的右上方生成一个小箭头
				//如果不是自己
				if(!brick.Equals(transform)){
					//新建一个箭头并加入 arrow容器中管理
					Transform tf=Instantiate(Resources.Load("prefabs/Arrow")as GameObject).transform;
					//指定arrow的parent以及arrow指向的目标位置的左邻居
					tf.GetComponent<ArrowForJoker>().ParentBrick=transform;
					tf.GetComponent<ArrowForJoker>().LNeiborOfDest=brick;
					Arrows4Joker.Add(tf);
					//把箭头挂载到本Brick的同一个Dock上
					tf.SetParent(transform.parent);
					//目标在自己左边
					if(GameControl.Instance.Decks[0].IndexOf(transform)>
						GameControl.Instance.Decks[0].IndexOf(brick)){
						tf.GetComponent<ArrowForJoker>().DestAtLeft=true;
						//箭头放置在目标左上角
						tf.localPosition=new Vector3(0,9,brick.localPosition.z-2);
					}
					else{
						tf.GetComponent<ArrowForJoker>().DestAtLeft=false;
						//箭头放置到目标Brick的右上角
						tf.localPosition=new Vector3(0,9,brick.localPosition.z+2);
					}
				}
			}
		}
	}

	public void DestroyArrows(){
		//销毁这些arrow
		if(Arrows4Joker.Count>0){
			foreach(var arrow in Arrows4Joker){
				Destroy(arrow.gameObject);
			}
			Arrows4Joker.Clear();
		}
	}

	//猜测这张牌的数字
	public void GuessTheBrick(int number){
		//猜对了
		if(number==BrickNumber){
			GameControl.Instance.GotYa(transform);
			//open自己
			OpenMe();
		}
		//猜错了
		else{
			GameControl.Instance.Oops();
		}
	}

	public void OpenMe(){
		IsOpen=true;
		GameControl.Instance.UnOpenedBricks.Remove(transform);
		_needAddForce=true;
	}

	void FixedUpdate(){
		if(_needAddForce&&transform.GetComponent<Rigidbody>().velocity.sqrMagnitude==0){
			CancelKinematic();
			forceX= -400;
			forceY=Random.Range(0,20);
			forceZ=Random.Range(-10,10);
			//GetComponent<Rigidbody>().AddForceAtPosition(new Vector3(forceX,forceY,forceZ),Input.mousePosition);
			GetComponent<Rigidbody>().AddRelativeForce(forceX,forceY,forceZ);
			_needAddForce=false;
			Invoke("SetAsKinematic",2);
		}

	}

	void SetAsKinematic(){
		GetComponent<Rigidbody>().isKinematic=true;
	}
	void CancelKinematic(){
		GetComponent<Rigidbody>().isKinematic=false;
	}

}
