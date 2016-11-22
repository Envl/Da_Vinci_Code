using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using ExitGames.Client.Photon.LoadBalancing;

//------------------------
//  不设置 玩家 这个类
//  不分 房主 和 成员 的原因是  
// 房主随时会变.. 当房主掉线. 房主就换成另一个人了.
//------------------------
public class GameControl : MonoBehaviour {
	PhotonClient _client;//管理与服务器的连接
	bool _IWin=false;
	float _lastQuitTime=-3;
	float _brickWidth=4.5f;
	byte _playerNum=4;
	// 编号actorID玩家是否ReadyToGo
	Dictionary<int,bool> _playerIsReadyToGo=new Dictionary<int, bool>();
	//已经完蛋了的玩家的id
	public List<int> DonePlayer=new List<int>();
	//------------------------------------------------
	//自己的deck是decks中的第0个 统统固定为0
	//然后再根据公式 ((id-myId)+人数)%人数 
	//来换算出其他玩家在我这边的deckindex
	//------------------------------------------------
	public const int _myDeckIndex=0;
	string _roomName;
	int _myID=0;
	int[] indexMap;//用来将AllBricks映射到StartPosition去
	//------UI元素相关------------------
	public Notification Notify{get;private set;}

	Transform _loading,_winBanner;
	List<Transform> _playerIcons;// 玩家头像  排列顺序和ID顺序一样
	GameObject _panel,_readyPanel,_overPanel,_canvas;
	public Transform DoneBtn{get;private set;} //整理牌完毕的按钮
	public Transform CancelBtn{get;private set;}//取消猜牌的按钮
	public Transform ContinueBtn{get;private set;}// 继续猜牌
	Vector3[] readyPos={new Vector3(-120,0),new Vector3(-40,0)
		,new Vector3(40,0),new Vector3(120,0)};
	public Transform PickPop{get;private set;}//确认选择的气泡按钮
	public Vector3[] FixedPos4Guess{get;private set;}//用来标明可以猜的牌的按钮的位置
	public RectTransform[] AllGuessBrick{get;private set;}// 用来猜牌的按钮
	public Transform StartBtn{get;private set;}//开始按钮
	public Transform Panel{get;private set;}//进入游戏房间的panel
	public Vector3[] FixedPos4PlayerIcon{get;private set;}//游戏进行时,玩家头像显示位置
	//------------------------
	public int CurrentID{get;private set;}//当前轮到的玩家的ID
	public int MyID{
		get{
			return _myID;
		}
	}
	public Transform MyDock{get;private set;}//我的dock
	//候选棋子 按下pickpop气泡就成功选择了
	public Transform CandidateBrick;
	//在猜的牌
	public Transform GuessingBrick;
	Transform _jokerBlack,_jokerWhite;// joker因为特殊 .. 需要他的引用
	GameObject _planet,_table,_unOccupied;
	Transform _go_GuessBricks;

	List<GameObject> PossibleBricks=new List<GameObject>();
	List<List<Transform>> _decks;
	List<Transform> _unOccupiedBricks;
	List<Transform> _allBricks,_unOpenedBrick;
	Transform[] _docks;//每个玩家牌应该放的位置
	Vector3[] _brickStartPositions;
	Quaternion[] _brickStartRotations;
	//List<Vector3> _fixedBrickPos;//牌可以放置的位置的坐标

	static GameControl _instance=null;//游戏控制器的单例
	public static GameControl Instance{
		get{
			return _instance;
		}
	}
	public List<Transform> UnOccupiedBricks{
		get{
			return _unOccupiedBricks;
		}
	}
	public List<Transform> UnOpenedBricks{
		get{
			return _unOpenedBrick;
		}
	}
	public List<List<Transform>> Decks{
		get{
			return _decks;
		}
	}
	public Transform Planet{
		get{
			return _planet.transform;
		}
	}
	public List<Transform> AllBricks{
		get{
			return _allBricks;
		}
	}
	public PhotonClient Client{
		get{
			return _client;
		}
	}

	void Awake(){
		Application.runInBackground=true;//防止后台掉线
		CustomTypes.Register();
		_client=new PhotonClient();
		_client.AppId="49395063-2a0e-4e1f-a35d-3d4600790bc2";//Turnbased 的 ID
		//_client.AppId="bc31b098-957d-4231-9ad5-b75adc3c6fb7";// RealTime 的ID
		bool connectInProcess=_client.ConnectToRegionMaster("asia");

		_go_GuessBricks=GameObject.Find("GuessBricks").transform;
		MyDock=GameObject.Find("DockD").transform;
		PickPop=GameObject.Find("PickPop").transform;
		_planet=GameObject.Find("Planet");
		_table=_planet.transform.GetChild(0).gameObject;
		_unOccupied=_table.transform.GetChild(5).gameObject;
		//--------------找到UI元素------------
		Notify=GetComponent<Notification>();
		Panel=GameObject.Find("Panel").transform;
		StartBtn=GameObject.Find("Start").transform;
		_canvas=GameObject.Find("Canvas");
		_winBanner=GameObject.Find("Win").transform;
		_panel=GameObject.Find("Panel");
		_readyPanel=GameObject.Find("ReadyPanel");
		_overPanel=GameObject.Find("OverPanel");
		DoneBtn=GameObject.Find("AdjustDone").transform;
		CancelBtn=GameObject.Find("Cancel").transform;
		ContinueBtn=GameObject.Find("ContinueGuess").transform;
		_loading=GameObject.Find("Loading").transform;
		//-----------------------

	

		if(GameControl._instance==null){
			GameControl._instance=this;
		}
		DontDestroyOnLoad(gameObject);

		#region 注册UI事件
		GameObject.Find("Create").GetComponent<Button>().onClick.AddListener(
			()=>{
				if(_roomName!=""){
					_client.OpCreateRoom(_roomName,new RoomOptions(){MaxPlayers=_playerNum},TypedLobby.Default);
				}

			});
		GameObject.Find("Join").GetComponent<Button>().onClick.AddListener(
			()=>{
				if(_roomName!=""){
					_client.OpJoinRoom(_roomName);
				

				}
			});
		GameObject.Find("PlayerNum").GetComponent<Dropdown>().onValueChanged
			.AddListener(x=>{
				_playerNum=(byte)(x+2);
			});
		GameObject.Find("RoomName").GetComponent<InputField>().onValueChanged
			.AddListener(x=>{
				_roomName=x;
			});
		GameObject.Find("Leave").GetComponent<Button>().onClick.AddListener(
			()=>{
				//销毁玩家头像
				foreach(var icon in _playerIcons){
					Destroy(icon.gameObject);
				}
				_playerIcons.Clear();

				//通知其他人  我离开了   
				_client.OpRaiseEvent(MyEventCode.OneLeave,_myID,true,RaiseEventOptions.Default);

				_client.OpLeaveRoom(false);
				_overPanel.SetActive(false);
				_panel.SetActive(true);
			
			});
		GameObject.Find("Again").GetComponent<Button>().onClick.AddListener(
			()=>{
				//currentID 为自己
				CurrentID=_myID;
				//重新设置玩家头像的parent
				for(int i=0;i<_playerNum;i++){
					var icon=_playerIcons[i];
					icon.SetParent(_readyPanel.transform);
				}
				//自己的头像放到readypos  并且通知别人
				//playerIcons readyPos的顺序和id一样 注意ID从1 开始
				var myIcon=_playerIcons[_myID-1];
				AnimElement.Prepare()
					.InitUGUI(myIcon.localPosition,
						readyPos[_myID-1],
						myIcon,
						0,
						40)
					.AddScaleAnimation(myIcon.localScale,Vector3.one*0.6f)
					.Play();
				_client.OpRaiseEvent(MyEventCode.OnePlayAgain,_myID,true,RaiseEventOptions.Default);
			

				//载入等待面板
				_overPanel.SetActive(false);
				_readyPanel.SetActive(true);

				EnterRoom(_playerNum);
			});
		//房主开始游戏
		GameObject.Find("Start").GetComponent<Button>().onClick.AddListener(
			()=>{
				//洗牌
				ShuffleDeck();
				//-----------------------------
				//把所有的牌发送给其他玩家
				//-----------------------
				_client.OpRaiseEvent(MyEventCode.SyncAllBricks,indexMap,true
				,RaiseEventOptions.Default);
				//放置牌到桌子上
				PlaceAllBricksOnTable(indexMap);
				StartGame();
			});
		// 完成 按钮的点击逻辑
		DoneBtn.GetComponent<Button>().onClick.AddListener(()=>{
			// 玩家确认完成Adjust过程中的joker放置后 点击 
			if(GameState.Instance.CurrentState==GameState.State.Adjust){
				//通知其他玩家在逻辑层更新我的Joker位置
				int msgType=0;
				int[] msg=new int[4];
				msg[0]=_myID;
				if(_decks[_myDeckIndex].Contains(_jokerWhite)){
					msgType=-1;
					msg[2]=_decks[_myDeckIndex].IndexOf(_jokerWhite);
				}
				if(_decks[_myDeckIndex].Contains(_jokerBlack)){
					msgType=-msgType;
					msg[3]=_decks[_myDeckIndex].IndexOf(_jokerBlack);
				}
				msg[1]=msgType;
				_client.OpRaiseEvent(MyEventCode.SyncJoker,msg,true,RaiseEventOptions.Default);

				//
				LocalReadyToGo();
				//隐藏所有arrows for joker
				_jokerBlack.GetComponent<Brick>().DestroyArrows();
				_jokerWhite.GetComponent<Brick>().DestroyArrows();
			}
			//  每一round中抽到joker的玩家确认完成joker的放置后点击
			else if(GameState.Instance.CurrentState==GameState.State.PlacingJoker){
				//进入Guess状态
				GameState.Instance.CurrentState=GameState.State.GuessABrick;
				//视觉提示 让我猜牌 
				Notify.autoNtf("请猜牌",Vector2.zero,new Vector2(1,1),1);

				//隐藏所有arrows for joker
				_jokerBlack.GetComponent<Brick>().DestroyArrows();
				_jokerWhite.GetComponent<Brick>().DestroyArrows();
			}
			else if(GameState.Instance.CurrentState==GameState.State.GuessingABrick){
				//不猜了   轮到下一个玩家
				ToNextOne();
				//隐藏按钮
				ContinueBtn.gameObject.SetActive(false);
				DoneBtn.gameObject.SetActive(false);
				//切换状态
				GameState.Instance.CurrentState=GameState.State.WaitForOthers;
			}
		
			DoneBtn.gameObject.SetActive(false);
		});
		//取消猜牌按钮的点击逻辑
		CancelBtn.GetComponent<Button>().onClick.AddListener(()=>{
			CancelBtn.gameObject.SetActive(false);
			//隐藏所有PossibleBrick
			HidePossibleBricks();
			//状态回到GuessABrick
			GameState.Instance.CurrentState=GameState.State.GuessABrick;
		});
		//继续猜
		ContinueBtn.GetComponent<Button>().onClick.AddListener(()=>{
			GameState.Instance.CurrentState=GameState.State.GuessABrick	;
			ContinueBtn.gameObject.SetActive(false);
			DoneBtn.gameObject.SetActive(false);
		});
		#endregion

		//----初始化UI状态----------
		_readyPanel.SetActive(false);
		DoneBtn.gameObject.SetActive(false);
		CancelBtn.gameObject.SetActive(false);
		ContinueBtn.gameObject.SetActive(false);
		Panel.gameObject.SetActive(false);
		StartBtn.gameObject.SetActive(false);
		_overPanel.SetActive(false);

		//--------------------------------------
	}

	// Use this for initialization
	void Start () {

		//---------------------------------------
		#region 对client中的事件进行函数注册
		//----------------------------------------
		//成功连接到服务器
		_client.ConnectedToServer+=()=>{
				Panel.gameObject.SetActive(true);
			//隐藏loading
			_loading.gameObject.SetActive(false);
		};
		_client.DebugAnyEventDataReceived+=e=>
		{
			print("收到事件代码: "+e.Code);//打印出code

		};
		_client.OtherJoin+=e=>{
			//显示房间中新加入的玩家
			Transform a=Instantiate(Resources.Load("prefabs/Player"+_client.CurrentRoom.PlayerCount)
				as GameObject).transform;
			a.SetParent(_readyPanel.transform);
			a.GetComponent<RectTransform>().anchoredPosition3D=readyPos[_client.CurrentRoom.PlayerCount-1];
			//因为Player使用的Dictionary  值从1开始 
			a.GetChild(0).GetComponent<Text>().text=_client.CurrentRoom
				.Players[_client.CurrentRoom.PlayerCount].ID.ToString();
			a.GetComponent<PlayerIcon>().playerID=_client.CurrentRoom.PlayerCount;
			_playerIcons.Add(a);
		};
		_client.OtherLeave+=e=>{
			#region
			//这里还需要完善逻辑.  需要对服务器进行修改
			#endregion
			/*StartCoroutine("FetchingInfoAfterEnter");
			print("m y   new  id "+_myID);*/
		};
		//某玩家在游戏结束后  点击了  离开按钮
		_client.OneLeave+=(id)=>{
			
		};
		//------------------
		//更新牌的位置为房主洗牌后的位置
		//-------------------
		_client.SyncAllBricks+=newIndex=>{
			//_readyPanel.SetActive(false);
			for(int i=0;i<newIndex.Length;i++){
				indexMap[i]=newIndex[i];
			}

			PlaceAllBricksOnTable(indexMap);
			StartGame();
		};
		//----------------------------
		//  别人Open了一张牌
		//------------------------------
		_client.OtherOpenABrick+=(indexInAllBricks)=>{
			_allBricks[indexInAllBricks].GetComponent<Brick>().OpenMe();
			//如果open的是我的牌
			if(_decks[_myDeckIndex].Contains(_allBricks[indexInAllBricks])){
				//我已经没有牌剩余了
				if(CheckMyRemanent()==0){
					IamDone();
					GameState.Instance.CurrentState=GameState.State.Watching;
				}
			}
		};
		//-----------------------------------------
		//每一轮别人捡起了一张牌 更新相关位置和从属关系
		//--------------------------------
		_client.OtherPickABrick+=(id,indexInAllBricks,destIndexInDeck)=>{
			//根据公式进行换算 因为每个人在自己那里都是 DockD为自己的Dock..
			int userIndexOfDockAndDeck=GetPlayerDeckIndex(id);
			Transform brick=_allBricks[indexInAllBricks];
			//设置Brick所有者的ID
			brick.GetComponent<Brick>().OwnerID=id;
			print("user:"+id+" picked "+indexInAllBricks);
			//从 UnOccupied中剔除
			_unOccupiedBricks.Remove(brick);
			//加入该玩家的deck  如果不含有这张牌  才加入 ,因为有时候同一张牌
			// 已经存在了,但是会被加入两次(应该只有 joker这种牌)
			if(!Decks[userIndexOfDockAndDeck].Contains(brick)){
				Decks[userIndexOfDockAndDeck].Insert(destIndexInDeck,brick);

			}
			//先移动到别处改变好rotation
			brick.localPosition=new Vector3(0,-100,0);
			//挂载到该玩家Dock下
			brick.SetParent(_docks[userIndexOfDockAndDeck]);
			brick.localRotation=Quaternion.Euler(new Vector3(0,0,0));

			//再根据在数组中的逻辑顺序放置到应该的位置
			PlaceBricks(userIndexOfDockAndDeck);

		};
		//成功进入房间
		_client.JoinedRoom+=()=>{
			//获取到自己的ID
			_myID= _client.LocalPlayer.ID;
			//用yield进行循环检查 直到有信息为止
			StartCoroutine("FetchingInfoAfterEnter");
			print("my id is:"+_myID);
			//获取完ID后  关掉UI
			_panel.SetActive(false);
			_readyPanel.SetActive(true);
			StartBtn.gameObject.SetActive(false);
			EnterRoom(_playerNum);
			//CurrentID设置为自己  让自己在房间里晃动
			CurrentID=_myID;

		};
		//成功创建房间
		_client.CreatedRoom+=()=>{
			//获取到自己的ID
			_myID= _client.LocalPlayer.ID;
			//房主为一号玩家 首先活动
			CurrentID=_myID;

			//用yield进行循环检查 直到有信息为止
			StartCoroutine("FetchingInfoAfterEnter");
			print("my id is:"+_myID);
			//获取完ID后  关掉UI
			_panel.SetActive(false);
			_readyPanel.SetActive(true);
			StartBtn.gameObject.SetActive(true);
			EnterRoom(_playerNum);
		};
		//--------------------------------------------
		//  房主处理别人的InitialPickRequest-
		//--------------------------------------------
		_client.RequestInitialPick+=(id,index)=>{
			//如果我是房主 
			if(_client.CurrentRoom.MasterClientId==_myID){
				HandleInitialPickRequest(id,index);
			}
		};
		//------------------------------------------
		//收到房主 InitialPick的Confirmation
		//		房主不响应这条消息,因为
		// 他发出消息时就进行了相关处理了
		//------------------------------------------
		_client.ConfirmInitialPick+=(id,indexer)=>{
			//如果我不是房主
			if(_client.CurrentRoom.MasterClientId!=_myID){
				OnInitialPickConfirmation(id,indexer);
			}
		};
		//-------------------------------
		//  房主收到其他人ReadyToGo
		//  保存当前ReadyToGo的人的ID
		//------------------------------
		_client.OtherReadyToGo+=(id)=>{
			//如果我是房主才响应这条消息
			if(_myID==_client.CurrentRoom.MasterClientId){
				ReadyToGo(id);
			}
		};
		//-----------------
		//  游戏开始
		//----------------
		_client.GO+=GO;
		//--------------
		//  轮到下一个玩家
		//----------------
		_client.NextOne+=(id)=>{
			//更新当前活动的那个玩家的ID
			CurrentID=id;
			//下一个玩家是我
			print("轮到id  "+id+"  我的id是 "+_myID);
			if(id==_myID){
				//视觉提示 请取一张牌  以后改成状态机了 用状态机做这个提示
				Notify.autoNtf("请取一张牌",Vector2.zero,new Vector2(1,1),1);

				//如果牌都取完了,  那么进入GuessABrick状态
				if(UnOccupiedBricks.Count==0){
					GameState.Instance.CurrentState=GameState.State.GuessABrick;
				}
				//切换到取牌的状态
				else{
					GameState.Instance.CurrentState=GameState.State.PickABrick;
				}
			}	
		};
		//--------------
		//某玩家 选择继续玩
		//---------------
		_client.OnePlayAgain+=(id)=>{
			var icon=_playerIcons[id-1];
			AnimElement.Prepare()
				.InitUGUI(icon.localPosition,
					readyPos[id-1],
					icon,
					0,
					40)
				.AddScaleAnimation(icon.localScale,Vector3.one*0.6f)
				.Play();

		};

		//----------------
		//  某玩家adjust完成后
		//  其他玩家同步他的joker位置
		//------------------------
		_client.SyncJoker+=  (id, msgType, whiteDestIndex, blackDestIndex) => {
			//  只有jokerWhite
			if(msgType==-1){
				_decks[GetPlayerDeckIndex(id)].Remove(_jokerWhite);
				_decks[GetPlayerDeckIndex(id)].Insert(whiteDestIndex,_jokerWhite);

			}
			//只有 jokerBlack
			else if(msgType==0){
				_decks[GetPlayerDeckIndex(id)].Remove(_jokerBlack);
				_decks[GetPlayerDeckIndex(id)].Insert(blackDestIndex,_jokerBlack);
			}
			// 两者都有
			else if(msgType==1){
				//先放入index小的  
				if(blackDestIndex<whiteDestIndex){
					_decks[GetPlayerDeckIndex(id)].Remove(_jokerBlack);
					_decks[GetPlayerDeckIndex(id)].Remove(_jokerWhite);

					_decks[GetPlayerDeckIndex(id)].Insert(blackDestIndex,_jokerBlack);
					_decks[GetPlayerDeckIndex(id)].Insert(whiteDestIndex,_jokerWhite);
				}
				else{
					_decks[GetPlayerDeckIndex(id)].Remove(_jokerBlack);
					_decks[GetPlayerDeckIndex(id)].Remove(_jokerWhite);

					_decks[GetPlayerDeckIndex(id)].Insert(whiteDestIndex,_jokerWhite);
					_decks[GetPlayerDeckIndex(id)].Insert(blackDestIndex,_jokerBlack);
				}
			}
		};
		//-----------------------------
		//  对某个玩家的牌进行逻辑层排序
		//---------------------------------
		_client.OrderOnesBrick+=(id)=>{
			OrderBricks(GetPlayerDeckIndex(id))	;
		};

		_client.DebugResponse+=(resp)=>{
			print("Code is "+resp.OperationCode);
			print("result is "+resp.ReturnCode);
		};
		// 玩家(id)的牌都翻开了  (不包括自己
		_client.OneDone+=(id)=>{
			DonePlayer.Add(id);

			//检查没有done的玩家只剩自己 ,那么  我赢了 游戏结束
			if(DonePlayer.Count+1==_playerNum){
				_IWin=true;
				//显示我赢了  
				AnimElement.Prepare()
					.InitUGUI(new Vector3(0,300,0),
						new Vector3(0,110,0),
						_winBanner,
						0,
						25)
					.Play();
				//赢家头像放大到中间   id从1开始
				var icon=_playerIcons[_myID-1];
				AnimElement.Prepare()
					.InitUGUI(icon.localPosition,
						Vector3.up*40,
						icon,
						0.2f,
						30)
					.AddScaleAnimation(icon.localScale,Vector3.one*0.5f)
					.Play();

				//通知其他玩家
				_client.OpRaiseEvent(MyEventCode.GameOver,_myID,true,RaiseEventOptions.Default);
				//显示重新游戏面板
				_overPanel.SetActive(true);
				//隐藏结束按钮  继续猜按钮
				DoneBtn.gameObject.SetActive(false);
				ContinueBtn.gameObject.SetActive(false);
				//gameover
				GameState.Instance.CurrentState=GameState.State.GameOver;
			}
			//显示通知 id  玩家已经完蛋了
			else{
				
			}
		};
		//游戏结束 (赢家发出这条消息)
		_client.GameOver+=(id)=>{
			//显示通知  你输了

			//gameover
			GameState.Instance.CurrentState=GameState.State.GameOver;
			//显示重新游戏面板
			_overPanel.SetActive(true);
			//隐藏结束按钮  继续猜按钮
			DoneBtn.gameObject.SetActive(false);
			ContinueBtn.gameObject.SetActive(false);
		};
		#endregion

		//-.-.-.-.-.-..-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-..-.-.-.-.-.-.-.-.-.-.--.-..---.-.-.-
		//-.-.-.-.-.-..-.-.-.-.-.-.-.-.-以上是各种事件注册-.-.-.-.-.--.-..---.-.-.-
		//-.-.-.-.-.-..-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-.-..-.-.-.-.-.-.-.-.-.-.--.-..---.-.-.-


		InitGuessStuffs();
		GetStartPositions();
		LoadResources();
		PlaceAllBricksOnTable(indexMap);

	}

	//猜牌相关 资源 变量 的初始化
	void InitGuessStuffs(){
		//计算GuessBrick可以放置的固定坐标
		FixedPos4Guess=new Vector3[14];// 最多有13张牌可以猜测
		AllGuessBrick=new RectTransform[26];
		for(int i=0;i<2;i++)
			for(int j=0;j<7;j++){
				FixedPos4Guess[i*7+j]=new Vector3(j*68-100,-i*115,0);
			}
		//拿到所有GuessBrick的引用 并且隐藏之
		for(int i=0;i<26;i++){
			AllGuessBrick[i]=_go_GuessBricks.GetChild(i).GetComponent<RectTransform>();
			AllGuessBrick[i].gameObject.SetActive(false);
		}
	}
		

	//显示 可以猜的 牌 
	public void ShowPossibleBricks(int brickType){
		HidePossibleBricks();
		for(int i=0;i<_unOpenedBrick.Count;i++){
			// 在猜的那张牌与候选牌颜色一致
			if(brickType==_unOpenedBrick[i].GetComponent<Brick>().BrickType
				//并且我没有这张牌
				&&!_decks[_myDeckIndex].Contains(_unOpenedBrick[i])
			){
					PossibleBricks.Add(AllGuessBrick[brickType*13+
						(_unOpenedBrick[i].GetComponent<Brick>().BrickNumber+13)%13].gameObject);
			}
		}
		for(int i=0;i<PossibleBricks.Count;i++){
			PossibleBricks[i].transform.localPosition=FixedPos4Guess[i];
			PossibleBricks[i].SetActive(true);
		}
	}

	public void HidePossibleBricks(){
		//隐藏cancel按钮
		CancelBtn.gameObject.SetActive(false);
		foreach(var guessBrick in PossibleBricks){
			guessBrick.SetActive(false);
		}
		PossibleBricks.Clear();
	}

	public void ToNextOne(){
		
		int nextID=_myID%_playerNum+1;
		//通知下一个还没"死"的玩家 轮到他了
		for(int i=0;i<_playerNum;i++){
			if(DonePlayer.Contains(nextID)){
				nextID=nextID%_playerNum+1;
			}	
			else{
				break;
			}
		}
		CurrentID=nextID;
		_client.OpRaiseEvent(MyEventCode.NextOne,nextID,true,RaiseEventOptions.Default);
		print("传递给了玩家  "+nextID);
	}

	public void InformOtherOpenABrick(Transform brick){
		_client.OpRaiseEvent(MyEventCode.OtherOpenABrick
			,_allBricks.IndexOf(brick)
			,true,RaiseEventOptions.Default);
	}
	//猜对了
	public void GotYa(Transform brick){
		//通知所有玩家 open那张牌
		InformOtherOpenABrick(brick);
		//隐藏PossibleBrick
		HidePossibleBricks();
		//如果游戏结束了 也就是我赢了

		//显示结束按钮  继续猜按钮
		DoneBtn.gameObject.SetActive(true);
		ContinueBtn.gameObject.SetActive(true);

	}
	//猜错了
	public void Oops(){
		//隐藏PossibleBricks
		HidePossibleBricks();
		//视觉提示 请公布一张牌
		Notify.autoNtf("请公开一张牌",Vector2.zero,new Vector2(1,1),1);


		//进入公布一张牌的状态
		GameState.Instance.CurrentState=GameState.State.OpenABrick;
	}
	//查找自己还剩几张牌d
	public int CheckMyRemanent(){
		int count=0;
		foreach(var brick in _decks[_myDeckIndex]){
			if(!brick.GetComponent<Brick>().IsOpen){
				count++;
			}			
		}
		return count;
	}
	public void IamDone(){
		DonePlayer.Add(_myID);
		//通知其他玩家  我完蛋了
		_client.OpRaiseEvent(MyEventCode.OneDone,_myID,true,RaiseEventOptions.Default);
	}
	IEnumerator FetchingInfoAfterEnter(){
		//显示正在获取信息的动画


		//一直在这边进行循环  直到获取到了其他玩家信息
		while(_client.CurrentRoom==null || _client.CurrentRoom.Players==null){
			yield return null;
		}
		//关闭正在获取信息的动画


		//显示在我之前加入房间的玩家  不用显示自己..自己的显示会在OtherJoin事件中处理
		for(int i=0;i<_client.CurrentRoom.PlayerCount-1;i++){
			Transform a=Instantiate(Resources.Load("prefabs/Player"+(i+1))
				as GameObject).transform;
			a.SetParent(_readyPanel.transform);
			a.GetComponent<RectTransform>().anchoredPosition3D=readyPos[i];
			//Players使用字典 从1开始
			a.GetChild(0).GetComponent<Text>().text=_client.CurrentRoom
				.Players[i+1].ID.ToString();
			a.GetComponent<PlayerIcon>().playerID=i+1;
			_playerIcons.Add(a);

						
		}	
		yield return null;
	}

	// Update is called once per frame
	void Update () {

		//退出事件
		if(Input.GetKeyDown(KeyCode.Escape)){
			if(Time.time-_lastQuitTime<2)
				Application.Quit();
			_lastQuitTime=Time.time;
		}

		_client.Service();
	}

	void OnApplicationQuit(){
		_client.Disconnect();
	}

	/// <summary>
	/// 加载所有的牌
	/// </summary>
	void LoadResources(){
		Transform[] tmpAllBricks=new Transform[26];
		for(int i=0;i<12;i++){
			tmpAllBricks[i]=Instantiate(Resources.Load("Prefabs/Bricks/"+i+"_Black") as GameObject).transform;
			tmpAllBricks[i].SetParent(_unOccupied.transform);

			tmpAllBricks[i+13]= Instantiate(Resources.Load("Prefabs/Bricks/"+i+"_White")as GameObject).transform;
			tmpAllBricks[i+13].SetParent(_unOccupied.transform);
		}
		tmpAllBricks[12]=Instantiate(Resources.Load("Prefabs/Bricks/Dash_Black")as GameObject).transform;
		tmpAllBricks[12].SetParent(_unOccupied.transform);
	
		tmpAllBricks[25]=Instantiate(Resources.Load("Prefabs/Bricks/Dash_White")as GameObject).transform;
		tmpAllBricks[25].SetParent(_unOccupied.transform);
		//头像固定显示位置  顺序为 下 左 上 右
		FixedPos4PlayerIcon=new Vector3[4]{new Vector3(-138,-123),
			new Vector3(-220,97),new Vector3(138,123),new Vector3(220,-97)};
		_unOccupiedBricks=new List<Transform>();
		_allBricks=new List<Transform>();
		_unOpenedBrick=new List<Transform>();
		_playerIcons=new List<Transform>();
		//所有牌加入unOccupied  unOpenedBrick  allBricks 中
		_unOccupiedBricks.AddRange(tmpAllBricks);
		_allBricks.AddRange(tmpAllBricks);
		_unOpenedBrick.AddRange(tmpAllBricks);

		//获取到joker的引用
		_jokerBlack=_allBricks[12];
		_jokerWhite=_allBricks[25];
	}

	//初始化所有牌的位置
	void PlaceAllBricksOnTable(int[] map){
		for(int i=0;i<map.Length;i++){
			_allBricks[i].SetParent(_unOccupied.transform);
			_allBricks[i].localScale=new Vector3(0.09f,0.09f,0.09f);
			_allBricks[i].localRotation=new Quaternion(_brickStartRotations[map[i]].x
																					,_brickStartRotations[map[i]].y
																					,_brickStartRotations[map[i]].z
																					,_brickStartRotations[map[i]].w);
			_allBricks[i].localPosition=new Vector3(_brickStartPositions[map[i]].x,
																					_brickStartPositions[map[i]].y,
																					_brickStartPositions[map[i]].z);
		}
	}

	//房主和成员进入房间都会执行的相同操作
	void EnterRoom(byte playerNum){
		if(_decks!=null){
			foreach(var deck in _decks){
				deck.Clear();
			}
			_decks.Clear();
		}
		//decks保存了每个玩家的牌
		_decks=new List<List<Transform>>();
		for(int i=0;i<_playerNum;i++){
			_decks.Add(new List<Transform>());
		}
		//根据玩家人数获取不同的布局方案
		_docks=new Transform[playerNum];
		switch(playerNum){
		case 2:
			_docks[0]=GameObject.Find("DockD").transform;
			_docks[1]=GameObject.Find("DockU").transform;
			break;
		case 3:
			_docks[0]=GameObject.Find("DockD").transform;
			_docks[1]=GameObject.Find("DockL").transform;
			_docks[2]=GameObject.Find("DockR").transform;
			break;
		case 4:
			_docks[0]=GameObject.Find("DockD").transform;
			_docks[1]=GameObject.Find("DockL").transform;
			_docks[2]=GameObject.Find("DockU").transform;
			_docks[3]=GameObject.Find("DockR").transform;
			break;
		}

		//清空unOccupied unOpeneed
		UnOccupiedBricks.Clear();
		UnOpenedBricks.Clear();
		//所有牌重新加入unOccupied  unOpenedBrick   中
		_unOccupiedBricks.AddRange(AllBricks);
		_unOpenedBrick.AddRange(AllBricks);
		foreach(var brick in _allBricks){
			brick.GetComponent<Brick>().IsOpen=false;
		}
		//清空挂掉的玩家
		DonePlayer.Clear();

	}
		

	//所有成员开始游戏都会执行的相同操作
	void StartGame(){
		//如果上局为赢家 ,隐藏 winbanner
		if(_IWin){
			_IWin=false;
			AnimElement.Prepare()
				.InitUGUI(new Vector3(0,300,0),
					new Vector3(0,110,0),
					_winBanner,
					0,
					25)
				.SetReverseAnimation(true)
				.Play();
		}
		//玩家头像移动到目标位置
		for(int i=0;i<_playerIcons.Count;i++){
			Transform icon=_playerIcons[i];
				icon.SetParent(_canvas.transform);
			AnimElement.Prepare()
				.InitLocal(icon.localPosition,
					//  id从1开始  
					FixedPos4PlayerIcon[GetPlayerDeckIndex(i+1)],
					icon,
					0,
					40)
				.AddScaleAnimation(icon.localScale,new Vector3(0.25f,0.25f))
				.Play();
			
		}
		//隐藏掉等待界面Panel
		_readyPanel.SetActive(false);

		print("游戏开始  人数: "+_playerNum);
		//将世界放平
		_planet.transform.localRotation=Quaternion.Euler(new Vector3());//有空了用插值动画实现
		//关掉kinematic
		foreach(var brick in _allBricks){
			brick.GetComponent<Rigidbody>().isKinematic=false;
		}
		//开启重力
		Physics.gravity=new Vector3(0,-30,0);
		//定时启动kinematic
		Invoke("SetAllKinematic",3);
		//游戏状态----InitialSelectBricks
		GameState.Instance.CurrentState=GameState.State.InitialSelectBricks;
	}

	//开启kinematic
	void SetAllKinematic(){
		foreach(var brick in _allBricks){
			brick.GetComponent<Rigidbody>().isKinematic=true;
		}
	}

	public void PlaceBricks(int indexOfDeckAndDock){
		int brickNum=_decks[indexOfDeckAndDock].Count;
		for(int i=0;i<brickNum;i++){
			_decks[indexOfDeckAndDock][i].localPosition=new Vector3(Random.value+0.2f,0.03f,(-brickNum/2+i)*_brickWidth);
			//_decks[indexOfDeckAndDock][i].localRotation=Quaternion.Euler(0,0,0);
		}
	}

	public void PickTheCandidate(){
		AUserPickABrick(_myID,CandidateBrick);
		//是Joker  进入PlaceJoker状态
		if(CandidateBrick.GetComponent<Brick>().BrickNumber==-1){
			GameState.Instance.CurrentState=GameState.State.PlacingJoker;
			//直接显示Joker可以放置的位置
			CandidateBrick.GetComponent<Brick>().SwitchArrows();
			//显示 确认完成 按钮
			DoneBtn.gameObject.SetActive(true);
		}
		//否则进入Guess状态
		else{
			//不过先通知其他玩家 我pick了哪张牌
			_client.OpRaiseEvent(MyEventCode.OtherPickABrick,
											new int[]{_myID,
															_allBricks.IndexOf(CandidateBrick),
															_decks[_myDeckIndex].IndexOf(CandidateBrick)}
											,true,RaiseEventOptions.Default);

			//最后进入Guess状态
			GameState.Instance.CurrentState=GameState.State.GuessABrick;

			Notify.autoNtf("请猜牌",Vector2.zero,new Vector2(1,1),1);

		}
	}

	//----------------------------
	//InitialPick的时候捡一张牌过来   
	//有空了用动画实现移动
	void AUserPickABrick(int id,Transform brick){
		//根据公式进行换算 因为每个人在自己那里都是 DockD为自己的Dock..
		int userIndexOfDockAndDeck=GetPlayerDeckIndex(id);
		print("user:"+id+" picked "+_allBricks.IndexOf(brick)+"added to deck"+userIndexOfDockAndDeck);
		//隐藏pickpop按钮
		GameControl.Instance.PickPop.position=new Vector3(0,-100,0);
		//从 UnOccupied中剔除
		_unOccupiedBricks.Remove(brick);
		//加入该玩家的deck
		if(GameState.Instance.CurrentState==GameState.State.InitialSelectBricks){
			Decks[userIndexOfDockAndDeck].Add(brick);
		}
		else{
			//获取到brick应该放到的index
			int index=0;
			for(int i=0;i<_decks[userIndexOfDockAndDeck].Count;i++){
				//同数字 黑色在前
				if((brick.GetComponent<Brick>().BrickNumber==_decks[userIndexOfDockAndDeck][i]
					.GetComponent<Brick>().BrickNumber
					&& brick.GetComponent<Brick>().BrickType>_decks[userIndexOfDockAndDeck][i]
					.GetComponent<Brick>().BrickType)
				//数字大的在前
				||(brick.GetComponent<Brick>().BrickNumber>_decks[userIndexOfDockAndDeck][i]
						.GetComponent<Brick>().BrickNumber)
				){
					index=i+1;
				}
			}
			//直接把该牌放到对应的index去  这样就避免了排序
			_decks[userIndexOfDockAndDeck].Insert(index,brick);
		}
		//挂载到该玩家Dock下
		brick.SetParent(_docks[userIndexOfDockAndDeck]);
		//先移动到别处改变好rotation
		brick.localPosition=new Vector3(0,-100,0);
		brick.localRotation=Quaternion.Euler(new Vector3(0,0,0));

		//如果是自己pick  并且是InitialPick那么就进行排序
		if(id==_myID
		&&GameState.Instance.CurrentState==GameState.State.InitialSelectBricks){
			OrderBricks(0);
		}
		//再放置到应该的位置
		PlaceBricks(userIndexOfDockAndDeck);
	}
#region    InitialPick相关的代码

	//------------------------------
	//向房主请求pick一张牌.. 如果同意才pick那张牌
	//-----------------------------
	public void RequestInitialPick(Transform brick){
		print("RequestInitialPick masterID is "+_client.CurrentRoom.MasterClientId);
		print("And my ID is "+_myID);
		//如果我不是房主 向房主请求
		if(_client.CurrentRoom.MasterClientId!=_myID){
			int[] msg={_myID,AllBricks.IndexOf(brick)};
			_client.OpRaiseEvent(MyEventCode.RequestInitialPick,msg
				,true,RaiseEventOptions.Default);
		}
		//如果我是房主..就自己做判断,然后将结果Confirm给其他玩家
		else{
			//该棋子unOccupied confirm 给所有其他玩家
			if(UnOccupiedBricks.Contains(brick)){
				//可以移动这张牌 于是移动之
				AUserPickABrick(_myID,brick);
				//通知其他玩家
				int[] msg={_myID,AllBricks.IndexOf(brick)};
				_client.OpRaiseEvent(MyEventCode.ConfirmInitialPick,msg
					,true,RaiseEventOptions.Default);
				//拿齐了四个
				if(Decks[_myDeckIndex].Count>=4){
					//状态进入整理自己的牌
					GameState.Instance.CurrentState=GameState.State.Adjust;
					PostInitialPick();
				}
			}
		}

	}

	//----------------------------------------
	//房主处理别人的InitialPickRequest/
	//-----------------------------------------
	void HandleInitialPickRequest(int id,int index){
		//该棋子unOccupied  同意pick
		if(UnOccupiedBricks.Contains(AllBricks[index])){
			//可以pick这张牌  pick之
			OnInitialPickConfirmation(id,index); 
			//通知其他玩家
			int[] msg={id,index};
			_client.OpRaiseEvent(MyEventCode.ConfirmInitialPick,msg,true,RaiseEventOptions.Default);
		}
		//该棋子已经occupied   不回复

	}

	//------------------------------------------
	//收到房主的InitialPickConfirmation
	//           进行InitialPick
	//只有房主自己不处理这条消息
	//-----------------------------------------
	void OnInitialPickConfirmation(int id,int index){
		AUserPickABrick(id,_allBricks[index]);
		//如果我就是是发出请求的那个人
		if(id==_myID){
			//拿齐了四张
			if(Decks[_myDeckIndex].Count>=4){
				GameState.Instance.CurrentState=GameState.State.Adjust;
				PostInitialPick();
			}
		}
	
	}



	//取完头四个牌后 让取到了dash的同学重新排列自己的牌
	public void PostInitialPick(){
		//对自己的牌进行逻辑层的排序更新.
		OrderBricks(_myDeckIndex);
		//对自己的牌进行视觉层更新
		PlaceBricks(_myDeckIndex);
		//通知其他玩家对我的牌进行逻辑层排序
		_client.OpRaiseEvent(MyEventCode.OrderOnesBrick,_myID,true,RaiseEventOptions.Default);

		bool iGotAJoker=false;//默认我没有joker
		//检查自己有没有Joker牌 (dash)
		foreach(var brick in Decks[_myDeckIndex]){
			//如果是一张joker
			if(brick.GetComponent<Brick>().BrickNumber==-1){
				//标记我有joker
				iGotAJoker=true;
				//做一些视觉提示  让用户来操作这张牌 
				Notify.autoNtf("请放置Joker牌",Vector2.zero,new Vector2(1,1),1);


			}
		}
		//我没有joker  直接进入等待状态 .并且通知主机
		if(!iGotAJoker){
			print("local ready to go");
			LocalReadyToGo();
		}
		//我有Joker  显示整理完毕的按钮
		else{
			DoneBtn.gameObject.SetActive(true);
		}
	}

	//ReadyToGo函数 
	void ReadyToGo(int id){
		//如果我是房主
		if(_client.CurrentRoom.MasterClientId==_myID){
			_playerIsReadyToGo[id]=true;
			//如果全员ReadyToGo 马上开始游戏
			if(_playerIsReadyToGo.Count==_playerNum){
				//开始本地游戏
				GO();
				//通知其他玩家开始游戏
				_client.OpRaiseEvent(MyEventCode.GO,null,true,RaiseEventOptions.Default);
				//清空字典   因为已经用不着了 以防干扰下一局游戏
				_playerIsReadyToGo.Clear();
			}
		}
		//我是客机
		else{
			//通知主机
			_client.OpRaiseEvent(MyEventCode.ReadyToGo,_myID,true,RaiseEventOptions.Default);
		}
	}


	//我准备完毕
	void LocalReadyToGo(){
		//进入等待状态
		GameState.Instance.CurrentState=GameState.State.ReadyToGo;
		ReadyToGo(_myID);
	}

	//所有玩家readytogo以后, 
	//收到房主发出事件 GO 后   所有人
	// 并且如果我是1号玩家直接进入pickBrick状态
	void GO(){
		//动画提示游戏正式开始

		//CurrentID设置为1号
		CurrentID=1;
		//在视觉层更新其他玩家的牌  
		for(int id=0;id<_playerNum;id++){
			//其他玩家
			if(id!=_myID){
				PlaceBricks(GetPlayerDeckIndex(id));
			}
		}
		//如果我是1号 开始pick牌
		if(_myID==1){
			Notify.autoNtf("请取一张牌",Vector2.zero,Vector2.one,1);
			GameState.Instance.CurrentState=GameState.State.PickABrick;
		}

	}
	//---------------------------------------
	//对 某玩家  的牌进行自动排序//
	// 只改变其在Deck List里面的顺序  
	//  而不更改其世界坐标.  这个等待其
	// 整理完自己的牌后再进行视觉更新 ,
	//  并且通知其他玩家
	//--------------------------------------
	void OrderBricks(int indexOfDeckAndDock){
		Decks[indexOfDeckAndDock].Sort((a,b)=>{
			if(a.GetComponent<Brick>().BrickNumber
				==b.GetComponent<Brick>().BrickNumber){
				return a.GetComponent<Brick>().BrickType
					.CompareTo(b.GetComponent<Brick>().BrickType);
			}
			return a.GetComponent<Brick>()
				.BrickNumber.CompareTo(b.GetComponent<Brick>().BrickNumber);
		});
		print("------排序结果是-----------deck"+indexOfDeckAndDock+"中有"+_decks[indexOfDeckAndDock].Count+"张牌");
		foreach(var brick in _decks[indexOfDeckAndDock]){
			print(brick.name);

		}

	}
	#endregion
	//---------------------------



	//这个函数绝对没问题 不必再检查
	void ShuffleDeck(){
		int tmpIndex;
		for(int i=indexMap.Length;i>0;i--){
			int a=(int) (Random.value*i);
			int b=i-1;

			if(a==b){
				continue;
			}

			tmpIndex=indexMap[a];
			indexMap[a]=indexMap[b];
			indexMap[b]=tmpIndex;
		}
	
	}

	//牌落地后,在每个客户端上位置是一样的
	void GetStartPositions(){
		indexMap=new int[26];
		_brickStartPositions=new Vector3[26];
		_brickStartRotations=new Quaternion[26];
		//计算出所有牌的出场坐标
		Transform tmp= _planet.transform.GetChild(0).GetChild(4);
		for(int i=0;i<26;i++){
			indexMap[i]=i;//初始进行对应的映射
			Transform a=tmp.GetChild(i);
			/*a.localPosition=new Vector3(a.localPosition.x
				,Mathf.Abs(a.localPosition.x)*2+Mathf.Abs(a.localPosition.z)*3
				,a.localPosition.z);*/
			a.localPosition=new Vector3(a.localPosition.x
				,Random.Range(5,15)
				,a.localPosition.z);
			a.Rotate(180,0,180);
			_brickStartPositions[i]=a.localPosition;
			_brickStartRotations[i]=a.localRotation;
		}
		Destroy(tmp.gameObject);
	}

	int GetPlayerDeckIndex(int actorID){
		//根据公式进行换算 因为每个人在自己那里都是 DockD为自己的Dock..
		return ((actorID-_myID)+_playerNum)%_playerNum;
	}
}
