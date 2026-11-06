using UnityEngine;

public class MerchantStageSpawnState : MonoBehaviour
{
    private static MerchantStageSpawnState instance;


    public static MerchantStageSpawnState Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj =
                    new GameObject(
                        "MerchantStageSpawnState"
                    );


                instance =
                    obj.AddComponent<
                        MerchantStageSpawnState
                    >();


                DontDestroyOnLoad(
                    obj
                );
            }


            return instance;
        }
    }


    // =========================================================
    // Runtime
    // =========================================================

    private bool merchantInNextDungeon;


    public bool MerchantInNextDungeon =>
        merchantInNextDungeon;


    // =========================================================
    // 다음 Dungeon 설정
    // =========================================================

    public void SetNextDungeonMerchant(
        bool value
    )
    {
        merchantInNextDungeon =
            value;


        Debug.Log(
            $"[MerchantStage] 다음 Dungeon 상인 : {value}"
        );
    }


    // =========================================================
    // DungeonGenerator가 한 번 읽고 초기화
    // =========================================================

    public bool ConsumeNextDungeonMerchant()
    {
        bool result =
            merchantInNextDungeon;


        merchantInNextDungeon =
            false;


        Debug.Log(
            $"[MerchantStage] 상인 Spawn 값 소비 : {result}"
        );


        return result;
    }


    public void Clear()
    {
        merchantInNextDungeon =
            false;
    }


    private void OnDestroy()
    {
        if (instance == this)
        {
            instance =
                null;
        }
    }
}