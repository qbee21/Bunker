using UnityEngine;

public class HideObject : MonoBehaviour
{
	public GameObject obj;
	public int timerActivate;
	public bool isBattle;
	
	public void Hide(bool isInPlan){
		if (obj == null) return;
		obj.SetActive(false);
		if(isInPlan) Invoke("Show", timerActivate);
		
	}
   
	public void Show(){
		if (obj == null) return;
		if(!isBattle)
		{
			obj.SetActive(true);
		}
		else 
		{
			Invoke("Show", 10);
		}
	}
	
	public void SetBool(bool B){
		
		isBattle = B;
		
	}
   
    
}
