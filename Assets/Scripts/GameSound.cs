using UnityEngine;
using PlayerPrefs = UnityEngine.PlayerPrefs;

public class GameSound : MonoBehaviour
{
    public AudioSource source;
	public GameObject music;
    public AudioClip attack;
    public AudioClip coin;
    public AudioClip click;
	public AudioClip victory;
    
    float nextAttack;
	public bool muted;
	public GameObject cross;

    public void PlayAttack()
    {
        if (muted || Time.unscaledTime < nextAttack) return;
        nextAttack = Time.unscaledTime + 0.12f;
        source.PlayOneShot(attack, 0.2f);
    }
    public void PlayCoin() { if (!muted) source.PlayOneShot(coin, 0.3f); }
    public void PlayClick() { if (!muted) source.PlayOneShot(click, 0.3f); }
    public void PlayWin() { if (!muted) source.PlayOneShot(victory, 0.4f); }
    public void Toggle()
    {
	    muted = !muted;
	    cross.SetActive(muted);
	    music.SetActive(!muted);
	    
        PlayerPrefs.SetInt("BunkerMuted", muted ? 1 : 0);
        PlayerPrefs.Save();
    }
}
