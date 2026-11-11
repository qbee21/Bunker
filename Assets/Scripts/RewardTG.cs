using UnityEngine;
// Retained only so older scenes deserialize cleanly. Telegram reward functionality was removed.
public class RewardTG : MonoBehaviour
{
	public GameObject hint;
    void Start() { if (hint != null) hint.SetActive(false); }
    public void GetCoins() { }
}
