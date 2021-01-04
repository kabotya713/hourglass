﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace KabotyaWorks
{
    public class PushButton : UdonSharpBehaviour
    {
        // メンバ変数 ------------------------------------------------------------
        public string OnEvent;                             // イベント呼び出し名
        public UdonBehaviour Target_UdonBehaviour;         // イベント送信先

        private const bool OUT_LEVEL_DEBUG = true;         // デバッグレベルのログ出力

        // 基本処理 ------------------------------------------------------------
        // 開始処理
        void Start()
        {
        }

        // 内部処理 ------------------------------------------------------------
        // デバッグ表示
        private void DebugLog(string level, string format)
        {
            if(level == "INFO")
            {
                Debug.Log(string.Format("[<color=cyan>U#</color>][<color=white>Info</color>]: {0}", format ));
            }
            else if(level == "WARNING")
            {
                Debug.LogWarning(string.Format("[<color=cyan>U#</color>]: {0}", format ));
            }
            else if(level == "ERROR")
            {
                Debug.LogError(string.Format("[<color=cyan>U#</color>]: {0}", format ));
            }
            else if(level == "CRITICAL")
            {
                Debug.LogError(string.Format("[<color=cyan>U#</color>][<color=red><b>Critical</b></color>]: {0}", format ));
            }
            else if((level == "DEBUG") && OUT_LEVEL_DEBUG == true)
            {
                Debug.Log(string.Format("[<color=cyan>U#</color>][<color=yellow>Debug</color>]: {0}", format ));
            }
            else if(level == "NOTICE")
            {
                Debug.Log(string.Format("[<color=cyan>U#</color>][<color=magenta>Notice</color>]: {0}", format ));
            }
            else if(level == "SUCCESS")
            {
                Debug.Log(string.Format("[<color=cyan>U#</color>][<color=#00ff00ff>Success</color>]: {0}", format ));
            }
            else
            {
                Debug.Log(string.Format("[<color=cyan>U#</color>][<color=magenta>NoTag</color>]: {0}", format ));
            }
        }

        // 外部イベント ------------------------------------------------------------
        // Event：ボタンがインタラクトされた
        public override void Interact()
        {
            DebugLog("INFO", string.Format("Interact:{0}", OnEvent));
            Target_UdonBehaviour.SendCustomEvent(OnEvent);
        }
    }
}