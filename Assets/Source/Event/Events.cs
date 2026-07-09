using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CardEvents
{
    public const string PREVIEW_CARD_TARGET = "Preview_Card_Target";
    public const string PLAY_CARD = "Play_Card";
    public const string PLAY_CARD_REQUEST = "Play_Card_Request";
    public const string SHOW_CARD_RANGE = "Show_Card_Range";
    public const string CLEAR_CARD_RANGE = "Clear_Card_Range";
}

public static class UIEvents
{
    public const string DRAW_CARD = "Draw_Card";
    public const string STAMINA_CHANGE = "Stamina_Change";
}

public static class TurnEvents
{
    public const string END_TURN = "End_Turn";
    
}

public static class BattleEvents
{
    public const string END_BATTLE = "End_Battle";
}

public static class SceneEvents
{
    public const string NEXT_SCENE = "Next_Scene";
    public const string TRY_AGAIN = "Try_Again";
}
