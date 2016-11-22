

public class GameState{
	static GameState _instance;

	public enum State:int{
		EnterPage,
		Lobby,//大厅
		Settings,//设置界面
		RoomWaiting,//房间中等待游戏开始
		Playing,//正在玩
		InitialSelectBricks,//开头选四张牌
		PickABrick,//拿一张牌
		GuessABrick,//猜一张牌
		OpenABrick,//摊开一张牌
		Adjust,//整理自己的牌
		ReadyToGo,//自己整理完成 在等待其他玩家整理完牌
		WaitForOthers,//等待其他玩家
		PlacingJoker,//放置Joker
		GuessingABrick,//正在猜牌界面中  正显示一堆可以猜的候选牌
		Watching,//观战模式
		GameOver,
	}

	public static GameState Instance{
		get{
			if(_instance==null){
				_instance=new GameState();
			}
			return _instance;
		}
	}

	public State CurrentState=State.EnterPage;
}