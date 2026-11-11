using System;
[Serializable]
public class LoadoutItem
{
    public int kind = -1;
    public int x = -1;
    public int y = -1;
    public int rotation;
}

[Serializable]
public class SaveData
{
    public int version = 1;
    public int completedWave;
    public int coins = 60;
    public int bunkerHealth;
    public int bunkerPower;
    public int foodUpgrade;
    public int[] unitHealth = new int[3];
    public int[] unitPower = new int[3];
    public long dailyTime;
    public int preparedWave;
    public bool prepReward;
    public int extensions;
    public int extensionRotation;
    public int slotRulesVersion;
    public bool[] cells = new bool[36];
    public LoadoutItem[] items = new LoadoutItem[16];
    public long revision;
}
