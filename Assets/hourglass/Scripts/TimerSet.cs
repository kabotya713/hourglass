
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// (N)分タイマー設定クラス
public class TimerSet : UdonSharpBehaviour
{
    // メンバ変数 ------------------------------------------------------------
    // ゲームオブジェクト
    [SerializeField] public GameObject hourglass;    // 砂時計

    UdonBehaviour Target_hourglass;                 // イベント送信先

    [SerializeField] public string OnEvent;         // イベント呼び出し名


    // 基本処理 ------------------------------------------------------------
    // 開始処理
    void Start()
    {
        Target_hourglass = (UdonBehaviour) hourglass.GetComponent(typeof(UdonBehaviour));
    }

    // 外部イベント ------------------------------------------------------------
    // Event：ボタンがインタラクトされた
    public override void Interact()
    {
        Target_hourglass.SendCustomEvent(OnEvent);
    }
}