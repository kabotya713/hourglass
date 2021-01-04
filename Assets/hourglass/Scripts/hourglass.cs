
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

// 砂時計制御クラス
public class hourglass : UdonSharpBehaviour
{
    // メンバ変数 ------------------------------------------------------------
    // ゲームオブジェクト
    // 表示版用
    public Text text_1;                 // 砂時計カウンター１
    public Text text_2;                 // 砂時計カウンター２
    public Text DebugConsole;           // デバッグ用コンソール

    public GameObject Timer3Button;     // 3分タイマー設定ボタン
    public GameObject Timer5Button;     // 5分タイマー設定ボタン
    public GameObject Timer10Button;    // 10分タイマー設定ボタン
    public GameObject ResetButton;      // リセットボタン
    public GameObject Sound;            // タイムアップ時の効果音
    public GameObject Sand;             // 砂パーティクル

    // 同期変数
    // 砂時計の設定時間
    [UdonSynced(UdonSyncMode.None)] private long Minute = 10;
    [UdonSynced(UdonSyncMode.None)] private long CountTicks;

    // インタラクトされたときの開始/終了時刻
    [UdonSynced(UdonSyncMode.None)] private long m_binTime_CheckPoint_Start = 0;
    [UdonSynced(UdonSyncMode.None)] private long m_binTime_CheckPoint_End = 0;


    // 反転時加算時間
    [UdonSynced(UdonSyncMode.None)] private long m_binCountTimer_1 = 0;
    [UdonSynced(UdonSyncMode.None)] private long m_binCountTimer_2 = 0;
    [UdonSynced(UdonSyncMode.None)] private long m_DispCount_1 = 0;
    [UdonSynced(UdonSyncMode.None)] private long m_DispCount_2 = 0;

    // 同期処理を行うユーザー
    [UdonSynced(UdonSyncMode.None)] private int m_SyncPlayerId;

    // フラグ関連
    // インタラクトしたか
    [UdonSynced(UdonSyncMode.None)] private bool m_flgIntaract = false;

    // 初回の動作フラグ
    [UdonSynced(UdonSyncMode.None)] private bool m_flgFirst = true;

    // タイマー１がカウント中か？
    [UdonSynced(UdonSyncMode.None)] private bool m_IsTimer_1 = false;

    // 反転中かどうか
    [UdonSynced(UdonSyncMode.None)] private bool m_IsReverse = false;

    // タイムアップしたか
    [UdonSynced(UdonSyncMode.None)] private bool m_IsTimeUp = false;
    //private bool m_IsTimeUp = false;

    // ローカル変数
    // リセット設定
    private bool m_IsReset = false;

    // デバッグ用コンソールの表示内容切り替えモード
    private bool m_DispMode = true; // true :フラグ表示モード、false:時刻表示モード

    // 反転時の回転度数(Updateの度に残角度を更新)
    private int m_RotateNum = 0;

    // 砂時計回転速度
    const int DEF_ROTATESPEED = 10; // １回の回転度数

    // 基本処理 ------------------------------------------------------------
    // 開始処理
    void Start()
    {
        m_flgFirst = true;
        m_IsTimer_1 = false;
        m_IsReverse = false;
        m_IsTimeUp = false;
        m_SyncPlayerId = -1;
        m_IsReset = false;
        OffSandParticle();
    }

    // 更新処理
    void Update()
    {
        if(m_DispMode)
        {
            // フラグを表示
            System.DateTime nowTime = System.DateTime.Now;
            DebugConsole.text = "";
            DebugConsole.text += string.Format("nowTime: {0:hh:mm:ss}.{1}\r\n", nowTime, nowTime.Millisecond);
            DebugConsole.text += string.Format("m_IsTimeUp: {0}\r\n", m_IsTimeUp);
            DebugConsole.text += string.Format("m_IsReset: {0}\r\n", m_IsReset);
            DebugConsole.text += string.Format("m_flgIntaract: {0}\r\n", m_flgIntaract);
            DebugConsole.text += string.Format("m_flgFirst: {0}\r\n", m_flgFirst);
            DebugConsole.text += string.Format("m_IsTimer_1: {0}\r\n", m_IsReset);

        }
        if((m_SyncPlayerId != -1) || (m_IsReset==false))
        {
            if(m_IsTimeUp)
            {
                // タイムアップしていたらタイムアップ用の表示をする
                if(m_IsTimer_1)
                {
                    text_1.text = "00:00.000";
                    text_1.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                    text_2.text = string.Format("{0}:00.000", Minute );
                }
                else
                {
                    text_1.text = string.Format("{0}:00.000", Minute );
                    text_2.text = "00:00.000";
                    text_2.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                }

                return;
            }

            if(Networking.LocalPlayer != null)
            {
                if(Networking.LocalPlayer.playerId == m_SyncPlayerId)
                {
                    // 同期した人が回転させる
                    RotateHourGlass();
                }
                else
                {
                }
            }
            if(!m_IsTimeUp)
            {
                // 同期した人がカウンタ表示させる
                DispCalcCount();
            }

        }
        else if(m_IsReset)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResetObject");
        }
    }

    // 砂時計の回転
    private void RotateHourGlass()
    {
       if(m_IsReverse)
        {
            m_RotateNum = 180;
            m_IsReverse = false;
            if(this.gameObject.transform.rotation.z == 360)
            {
                //TODO：０度に戻す
                // 戻さなくてもいいかも
            }
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "OffSandParticle");
        }

        if(m_RotateNum > 0){
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "RotatePerUpdate");
            m_RotateNum -= DEF_ROTATESPEED;
        }
        else if((m_RotateNum == 0) &&(!Sand.activeSelf))
        {
            // 回転してなくて、パーティクルがオフの時
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "OnSandParticle");
        }
    }

    // 1秒があたりの回転更新
    public void RotatePerUpdate()
    {
        this.gameObject.transform.Rotate(this.gameObject.transform.forward * DEF_ROTATESPEED);
    }

    // 時間計算処理
    private void DispCalcCount()
    {
        if(m_flgIntaract)
        {
            System.TimeSpan timer_1;
            System.TimeSpan timer_2;
            if(m_flgFirst)
            {
                System.DateTime nowTime = System.DateTime.Now;
                System.DateTime startTime = System.DateTime.FromBinary(m_binTime_CheckPoint_Start);
                System.DateTime endTime = System.DateTime.FromBinary(m_binTime_CheckPoint_End);
                System.TimeSpan  interval_1 = nowTime - startTime;　// 現在時刻-開始時刻
                //System.TimeSpan  interval_2 = nowTime - endTime;　　// 現在時刻-終了時刻

                long outTick_1 = CountTicks - interval_1.Ticks;
                long outTick_2 = interval_1.Ticks;

                timer_1 = new System.TimeSpan(outTick_1);
                timer_2 = interval_1;

                // デバッグ用出力
                if((DebugConsole.IsActive()) && (m_DispMode == false))
                {
                    DebugConsole.text = "";
                    DebugConsole.text += string.Format("Set Minute: {0}\r\n", Minute);
                    DebugConsole.text += string.Format("CountTicks: {0}\r\n", CountTicks);
                    DebugConsole.text += string.Format("nowTime: {0:hh:mm:ss}.{1}\r\n", nowTime, nowTime.Millisecond);
                    DebugConsole.text += string.Format("startTime: {0:hh:mm:ss}.{1}\r\n", startTime, startTime.Millisecond);
                    DebugConsole.text += string.Format("endtTime: {0:hh:mm:ss}.{1}\r\n", endTime, endTime.Millisecond);
                    //DebugConsole.text += string.Format("interval_1: {0}:{1}.{2}\r\n", interval_1.Minutes, interval_1.Seconds, interval_1.Milliseconds);
                    //DebugConsole.text += string.Format("interval_2: {0}:{1}.{2}\r\n", interval_2.Minutes, interval_2.Seconds, interval_2.Milliseconds);
                    DebugConsole.text += string.Format("timer_1: {0}:{1}.{2}\r\n", timer_1.Minutes, timer_1.Seconds, timer_1.Milliseconds);
                    DebugConsole.text += string.Format("timer_2: {0}:{1}.{2}\r\n", timer_2.Minutes, timer_2.Seconds, timer_2.Milliseconds);
                    DebugConsole.text += string.Format("timer_1Tick: {0}\r\n", timer_1.Ticks);
                    DebugConsole.text += string.Format("timer_2Tick: {0}\r\n", timer_2.Ticks);
                    System.TimeSpan Checktime = timer_1 + timer_2;
                    DebugConsole.text += string.Format("ズレ確認: {0}:{1}.{2}\r\n", Checktime.Minutes, Checktime.Seconds, Checktime.Milliseconds);
                }
            }
            else
            {
                System.DateTime nowTime = System.DateTime.Now;
                System.DateTime startTime = System.DateTime.FromBinary(m_binTime_CheckPoint_Start);
                System.DateTime endTime = System.DateTime.FromBinary(m_binTime_CheckPoint_End);
                System.TimeSpan  interval_1 = nowTime - startTime;　// 現在時刻-開始時刻
                //System.TimeSpan  interval_2 = nowTime - endTime;　　// 現在時刻-終了時刻
                long countTime = interval_1.Ticks;
                long outTick_1 = 0;
                long outTick_2 = 0;

                if(m_IsTimer_1)
                {
                    // 反転時の時刻からカウント計算
                    // タイマー1の場合
                    outTick_1 = m_DispCount_1 - countTime; // タイマー１は減算
                    outTick_2 = m_DispCount_2 + countTime; // タイマー２は加算
                    timer_1 = new System.TimeSpan(outTick_1);
                    timer_2 = new System.TimeSpan(outTick_2);

                    if(outTick_1 < 10000){
                        // memo:Updateの更新頻度がわからないので念のため
                        // 音鳴らすのにラグがある
                        // タイムアップ
                        TimeUp();
                    }
                }
                else
                {
                    // タイマー2の場合
                    outTick_1 = m_DispCount_1 + countTime; // タイマー１は加算
                    outTick_2 = m_DispCount_2 - countTime; // タイマー２は減算
                    timer_1 = new System.TimeSpan(outTick_1);
                    timer_2 = new System.TimeSpan(outTick_2);
                    if(outTick_2 < 10000){
                        // memo:Updateの更新頻度がわからないので念のため
                        // タイムアップ
                        TimeUp();
                    }
                }

                // デバッグ用出力
                if((DebugConsole.IsActive()) && (m_DispMode == false))
                {
                    if(m_IsTimer_1)
                    {
                        DebugConsole.text = "タイマ１に反転\r\n";
                    }
                    else
                    {
                        DebugConsole.text = "タイマ２に反転\r\n";
                    }

                    DebugConsole.text += string.Format("Set Minute {0}\r\n", Minute);
                    DebugConsole.text += string.Format("CountTicks {0}\r\n", CountTicks);
                    DebugConsole.text += string.Format("nowTime：{0:hh:mm:ss}.{1}\r\n", nowTime, nowTime.Millisecond);
                    DebugConsole.text += string.Format("startTime:{0:hh:mm:ss}.{1}\r\n", startTime, startTime.Millisecond);
                    DebugConsole.text += string.Format("endtTime:{0:hh:mm:ss}.{1}\r\n", endTime, endTime.Millisecond);
                    //DebugConsole.text += string.Format("interval_1: {0}:{1}.{2}\r\n", interval_1.Minutes, interval_1.Seconds, interval_1.Milliseconds);
                    //DebugConsole.text += string.Format("interval_2: {0}:{1}.{2}\r\n", interval_2.Minutes, interval_2.Seconds, interval_2.Milliseconds);
                    DebugConsole.text += string.Format("timer_1: {0}:{1}.{2}\r\n", timer_1.Minutes, timer_1.Seconds, timer_1.Milliseconds);
                    DebugConsole.text += string.Format("timer_2: {0}:{1}.{2}\r\n", timer_2.Minutes, timer_2.Seconds, timer_2.Milliseconds);
                    System.TimeSpan Checktime = timer_1 + timer_2;
                    DebugConsole.text += string.Format("outTick_1 {0}\r\n", outTick_1);
                    DebugConsole.text += string.Format("outTick_2 {0}\r\n", outTick_2);
                    DebugConsole.text += string.Format("ズレ確認: {0}:{1}.{2}\r\n", Checktime.Minutes, Checktime.Seconds, Checktime.Milliseconds);
                }


            }

            // 表示出力
            text_1.text = string.Format("{0:mm\\:ss\\.fff}", timer_1 );
            text_2.text = string.Format("{0:mm\\:ss\\.fff}", timer_2 );
            if(Networking.LocalPlayer != null)
            {
            if(Networking.LocalPlayer.playerId == m_SyncPlayerId)
                {
                    // 同期している人が残タイマーを取得する
                    m_binCountTimer_1 = timer_1.Ticks;
                    m_binCountTimer_2 = timer_2.Ticks;
                }
            }
        }
        else
        {
            // 初期表示
            text_1.text = string.Format("{0}:00.000", Minute );
            text_2.text = "00:00.000";

            if(DebugConsole.IsActive())
            {
                DebugConsole.text = "";
            }
        }
    }

    // タイマーの設定
    private void OnTimer()
    {
        if(m_IsTimeUp)
        {
            // タイムアップしてたら、先にリセットボタンを押すこと
            return;
        }
        // 反転ボタンをインタラクトした人が同期する
        SyncVariable();
        m_SyncPlayerId = Networking.LocalPlayer.playerId;

        // インタラクトしたらスタート
        m_flgIntaract = true;

        if((m_binCountTimer_1==0)&&(m_binCountTimer_2==0))
        {
            // 加算値がないから初期時計算から
            m_flgFirst = true;

            // カウント値を設定：100nSec単位Tickに変換
            CountTicks = (long)(Minute*60*10000000);

            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "StartObject");
        }
        else
        {
            // １回以上反転した（初期状態ではない）から加算時計算から
            m_flgFirst = false;

            // 反転時タイマー値(Tick)
            m_DispCount_1 = m_binCountTimer_1;
            m_DispCount_2 = m_binCountTimer_2;

        }
        // ２回め以降のインタラクトは反転する
        // 減算タイマを反転する
        if(m_IsTimer_1 == true)
        {
            m_IsTimer_1 = false;    // タイマー１ではない
        }
        else
        {
            m_IsTimer_1 = true;
        }

        // // 初回時間設定
        // m_binCountTimer_1 = CountTicks;
        // m_binCountTimer_2 = 0;

        // 同期時刻取得
        System.DateTime starttime = System.DateTime.Now;
        m_binTime_CheckPoint_Start = starttime.ToBinary();

        // long StartTick = starttime.Ticks;
        // long endTick = StartTick + CountTicks;
        long StartTick = 0;
        long endTick = 0;

        if(m_IsTimer_1)
        {
            StartTick = starttime.Ticks;
            endTick = StartTick + (CountTicks - m_binCountTimer_2);
        }
        else
        {
            StartTick = starttime.Ticks;
            endTick = StartTick + (CountTicks - m_binCountTimer_1);
        }


        System.DateTime endtime = new System.DateTime(endTick);
        m_binTime_CheckPoint_End = endtime.ToBinary();

        // DebugConsole.text = "";
        // DebugConsole.text += string.Format("startTime: {0:hh:mm:ss}.{1}\r\n", starttime, starttime.Millisecond);
        // DebugConsole.text += string.Format("endtTime: {0:hh:mm:ss}.{1}\r\n", endtime, endtime.Millisecond);
        // DebugConsole.text += string.Format("m_binTime_CheckPoint_Start: {0}\r\n", m_binTime_CheckPoint_Start);
        // DebugConsole.text += string.Format("m_binTime_CheckPoint_End: {0}\r\n", m_binTime_CheckPoint_End);
        // DebugConsole.text += string.Format("StartTick: {0}\r\n", StartTick);
        // DebugConsole.text += string.Format("endTick: {0}\r\n", endTick);
        // DebugConsole.text += string.Format("m_binCountTimer_1: {0}\r\n", m_binCountTimer_1);
        // DebugConsole.text += string.Format("m_binCountTimer_2: {0}\r\n", m_binCountTimer_2);
    }

    // タイムアップ処理
    private void TimeUp()
    {
        m_IsTimeUp = true;
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "OnSound");
    }

    // 効果音を鳴らす
    public void OnSound()
    {
        // サウンドオブジェクトを無効→有効にして鳴らす
        Sound.SetActive(false);
        Sound.SetActive(true);
    }

    // パーティクル表示切替
    public void OnSandParticle()
    {
        Sand.SetActive(true);
    }
    public void OffSandParticle()
    {
        Sand.SetActive(false);
    }


    // リセットボタン表示/非表示
    public void OnResetButton()
    {
        ResetButton.SetActive(true);
    }
    public void OffResetButton()
    {
        ResetButton.SetActive(false);
    }

    // 変数同期のためのオーナー切り替え
    private void SyncVariable()
    {
        if (!Networking.IsOwner(Networking.LocalPlayer, this.gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, this.gameObject);
        }
        m_SyncPlayerId = Networking.LocalPlayer.playerId;
    }

    // 開始時のオブジェクト設定(同期用)
    public void StartObject()
    {
        Timer3Button.SetActive(false);
        Timer5Button.SetActive(false);
        Timer10Button.SetActive(false);
        ResetButton.SetActive(true);
        OnSandParticle();

        text_1.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        text_2.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    }

    // Event：ワールドオブジェクトのリセット(同期用)
    public void ResetObject()
    {
        Timer3Button.SetActive(true);
        Timer5Button.SetActive(true);
        Timer10Button.SetActive(true);
        ResetButton.SetActive(false);
        OffSandParticle();

        text_1.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        text_2.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        if(DebugConsole.IsActive())
        {
            DebugConsole.text = "";
        }

        m_IsReset = false;
    }


    // 外部イベント ------------------------------------------------------------
    // Event：リセットボタン押下
    public void OnReset()
    {
        SyncVariable();
        // 同期変数
        m_binCountTimer_1 = 0;
        m_binCountTimer_2 = 0;
        m_DispCount_1 = 0;
        m_DispCount_2 = 0;
        m_flgIntaract = false;
        m_flgFirst = true;
        m_IsTimer_1 = false;
        m_IsTimeUp = false;
        m_SyncPlayerId = -1;

        // 非同期変数
        // リセットフラグを有効にして、各自で描画更新させる
        m_IsReset = true;
    }

    // Event：3分タイマー設定ボタン押下
    public void setTimer3()
    {
        SyncVariable();
        Minute = 3;
        OnReset();
    }

    // Event：5分タイマー設定ボタン押下
    public void setTimer5()
    {
        SyncVariable();
        Minute = 5;
        OnReset();
    }

    // Event：10分タイマー設定ボタン押下
    public void setTimer10()
    {
        SyncVariable();
        Minute = 10;
        OnReset();
    }

    // Event：砂時計の開始/回転ボタンを押下
    public void Reverse()
    {
        if(m_RotateNum == 0)
        {
            SyncVariable();
            m_IsReverse = true;

            OnTimer();
        }

        // タイマー１が上向きのときにリセットボタンを表示
        // MEMO：タイマー２が上向きのときにリセットすると、計算が逆向きになるため
        if(m_IsTimer_1)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "OffResetButton");
        }
        else
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "OnResetButton");
        }
    }
}
