using UnityEngine;
using UnityEngine.EventSystems;

public class CoinPickup : MonoBehaviour, IPointerClickHandler
{
    public int value;
    public float lifetime = 12;
    bool collected;

    void Update()
    {
        lifetime -= Time.deltaTime;
        if (lifetime <= 0) Collect();
    }

    public void OnPointerClick(PointerEventData data) { Collect(); }
    public void Collect()
    {
        if (collected) return;
        collected = true;
        GameCore.instance.economy.AddCoin(value);
        GameCore.instance.sound.PlayCoin();
        Destroy(gameObject);
    }
}
