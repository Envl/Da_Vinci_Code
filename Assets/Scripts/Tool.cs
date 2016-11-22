using UnityEngine;
using System.Collections;

/// <summary>
/// Tool.类 包含一些自己要用的工具
/// a)		CirculateIndex  :: 循环访问 数组的工具
/// 		 输入一个索引和数组长度,自动提供循环功能,  
/// 		比如长度为7的数组 ,索引是7 就自动
/// 		将其改成1   如果是-1 就自动改成6 这样实现循环
/// </summary>
public static class Tool {
	public static int CirculateIndex(int index,int len){
		return (index+len)%len;
	}

	public static Vector3 DeepCloneVector3(Vector3 a){
		return new Vector3(a.x,a.y,a.z);
	}

	public static float Interpolate(float start,float end,float t){
		return t*end+(1-t)*start;
	}
	public static int Interpolate(int start,int end,float t){
		return (int)(t*end+(1-t)*start);
	}
	public static Vector3 Interpolate(Vector3 startPos,Vector3 endPos,float t){
		return new Vector3(t*endPos.x+(1-t)*startPos.x,
			t*endPos.y+(1-t)*startPos.y,
			t*endPos.z+(1-t)*startPos.z);
	}

	public static Color IntColor(int grey){
		return new Color((float)grey/255,(float)grey/255,(float)grey/255);
	}
	public static Color IntColor(int r,int g,int b){
		return new Color((float)r/255,(float)g/255,(float)b/255);
	}
	public static Color IntColor(int r,int g,int b,int alpha){
		return new Color((float)r/255,(float)g/255,(float)b/255,(float)alpha/255);
	}
}
	