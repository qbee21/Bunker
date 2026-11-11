using UnityEngine;

using PlayerPrefs = UnityEngine.PlayerPrefs;

public class HintShower : MonoBehaviour
{
	public GameObject hint;
    void Start()
    {
	    if(PlayerPrefs.GetInt("start",0)!=0)
	    {
	    	hint.SetActive(false);
	    }
    }

	public void Hide(){
		
		
		hint.SetActive(false);
		PlayerPrefs.SetInt("start",1);
		PlayerPrefs.Save();
	}
	
}
